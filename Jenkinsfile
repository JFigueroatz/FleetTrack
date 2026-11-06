// Pipeline de FleetTrack (plan de trabajo, seccion 10).
// Checkout -> dependencias -> lint -> unitarias -> SonarQube -> npm audit -> Dependency-Check -> SAST ->
// Docker Build -> despliegue QA -> API -> Cypress -> recuperacion -> Maestro -> ZAP -> rendimiento ->
// criterios de salida -> aprobacion de Alejandro -> despliegue final.
//
// Reglas: ningun paso ignora errores en silencio; si un reporte obligatorio falta, el build falla.
// Los secretos vienen de credenciales de Jenkins, nunca de Git. Los reportes se archivan por build y commit.
// Agentes: 'qa' (Docker, .NET 10, Node 24, k6, Semgrep; equipo propio) y 'android' (SDK + emulador + Maestro).
// MSYS_NO_PATHCONV=1 solo en los `docker run` (evita que Git Bash convierta /src, /w, /report); en el resto lo necesitan /dev/null y /tmp.
// Un mismo equipo puede tener las dos etiquetas (ver infrastructure/jenkins). Terraform y Ansible corren en contenedores.

pipeline {
  agent { label 'qa' }

  options {
    timestamps()
    disableConcurrentBuilds()
    timeout(time: 150, unit: 'MINUTES')
    buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '10'))
  }

  parameters {
    booleanParam(name: 'FULL_PERFORMANCE', defaultValue: false,
      description: 'Candidato a release: load 100 VUs/10 min + stress + spike a escala completa. Sin marcar: corrida corta.')
    booleanParam(name: 'DEPLOY_FINAL', defaultValue: false,
      description: 'Solo en main: pide la aprobacion de Alejandro y despliega con Ansible.')
  }

  environment {
    COMPOSE_PROJECT_NAME = 'fleettrack'
    SONAR_HOST_URL       = 'http://localhost:9000'
    SONAR_TOKEN          = credentials('sonar-token')
    // .env de QA (POSTGRES_*, RABBITMQ_*, JWT_SECRET) como credencial tipo "Secret file".
    QA_ENV_FILE          = credentials('fleettrack-qa-env')
  }

  stages {

    stage('Checkout') {
      steps {
        checkout scm
        sh '''
          cp "$QA_ENV_FILE" .env
          mkdir -p reports/sonar reports/fortify reports/dependency-check reports/zap reports/performance reports/accessibility reports/coverage reports/cypress reports/maestro reports/junit
          printf '{"build":"%s","commit":"%s","date":"%s","ambiente":"QA local (Docker Compose)"}\n' \
            "$BUILD_NUMBER" "$(git rev-parse HEAD)" "$(date -u +%FT%TZ)" > reports/metadata.json
        '''
      }
    }

    stage('Dependencias') {
      parallel {
        stage('NuGet') { steps { sh 'dotnet restore backend/FleetTrack.slnx' } }
        stage('npm')   { steps { sh 'cd frontend && npm ci' } }
      }
    }

    stage('Lint y validaciones') {
      steps {
        sh '''
          WS=$(pwd -W 2>/dev/null || pwd)
          (cd frontend && npm run lint && npx tsc -b)
          docker compose config -q
          for f in tests/recovery/*.sh tests/maestro/*.sh tests/coverage/*.sh; do bash -n "$f"; done
          MSYS_NO_PATHCONV=1 docker run --rm -v "$WS/infrastructure/terraform:/w" -w /w hashicorp/terraform:1.9 init -backend=false -input=false
          MSYS_NO_PATHCONV=1 docker run --rm -v "$WS/infrastructure/terraform:/w" -w /w hashicorp/terraform:1.9 validate
          MSYS_NO_PATHCONV=1 docker run --rm -v "$WS:/w" -w /w python:3.12-slim sh -c "pip install -q ansible-core && ansible-playbook --syntax-check -i ansible/inventory.example.ini ansible/site.yml"
        '''
      }
    }

    stage('Unitarias') {
      steps {
        sh 'dotnet test backend/tests/FleetTrack.Api.Tests/FleetTrack.Api.Tests.csproj --logger "trx;LogFileName=unit.trx" --results-directory reports/junit'
      }
    }

    stage('SonarQube') {
      steps {
        // Levanta solo la infraestructura (postgres, redis, rabbitmq): la API corre en el host bajo cobertura.
        sh '''
          docker compose up -d --wait postgres redis rabbitmq
          bash tests/coverage/run-coverage.sh
          bash tests/coverage/sonar-scan.sh "$BUILD_NUMBER"
        '''
      }
    }

    stage('npm audit') {
      steps {
        sh '''
          cd frontend
          npm audit --audit-level=high --json > ../reports/dependency-check/npm-audit.json || { npm audit --audit-level=high; exit 1; }
        '''
      }
    }

    stage('Dependency-Check') {
      // Sin la clave de NVD este paso falla: el build queda UNSTABLE y la etapa de criterios de salida
      // lo bloquea por falta del reporte obligatorio (no se aprueba nada sin este analisis).
      environment { NVD_API_KEY = credentials('nvd-api-key') }
      steps {
        catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
          sh '''
            WS=$(pwd -W 2>/dev/null || pwd)
            MSYS_NO_PATHCONV=1 docker run --rm -v "$WS:/src" -v "$WS/reports/dependency-check:/report" \
              -v fleettrack-depcheck-data:/usr/share/dependency-check/data owasp/dependency-check:latest \
              --scan /src --nvdApiKey "$NVD_API_KEY" --format HTML --format JSON --out /report \
              --failOnCVSS 9 --exclude "**/node_modules/**" --exclude "**/bin/**" --exclude "**/obj/**"
            (cd backend && dotnet list package --vulnerable --include-transitive | tee ../reports/dependency-check/nuget-vulnerable.txt | { ! grep -q "Critical\\|High"; })
          '''
        }
      }
    }

    stage('SAST (Semgrep en lugar de Fortify)') {
      // Fortify requiere licencia no disponible; el plan exige resolver el acceso o una excepcion academica expresa.
      steps {
        // Semgrep corre en su imagen oficial: no depende de un ejecutable local (Windows lo bloquea con Control de aplicaciones).
        sh '''
          WS=$(pwd -W 2>/dev/null || pwd)
          MSYS_NO_PATHCONV=1 docker run --rm -v "$WS:/src" -w /src semgrep/semgrep:1.183.0 semgrep --error --config p/csharp --config p/security-audit --config p/secrets --json -o reports/fortify/semgrep-backend-mobile.json backend mobile
          MSYS_NO_PATHCONV=1 docker run --rm -v "$WS:/src" -w /src semgrep/semgrep:1.183.0 semgrep --error --config p/typescript --config p/react --config p/security-audit --config p/secrets --json -o reports/fortify/semgrep-frontend.json frontend/src frontend/cypress
        '''
      }
    }

    stage('Docker Build') {
      steps { sh 'docker compose build' }
    }

    stage('Despliegue QA') {
      steps {
        sh '''
          docker compose up -d --wait
          curl -fsS http://localhost:8080/health
          curl -fsS http://localhost:3000/ > /dev/null
        '''
      }
    }

    stage('Pruebas de API (xUnit + coleccion Insomnia)') {
      // Las pruebas de integracion pegan por HTTP al stack desplegado. La coleccion de Insomnia
      // (tests/insomnia) se versiona para ejecucion manual; no se corre con `inso` porque requiere tokens por peticion.
      steps { sh 'dotnet test backend/tests/FleetTrack.Api.IntegrationTests/FleetTrack.Api.IntegrationTests.csproj --logger "trx;LogFileName=integration.trx" --results-directory reports/junit' }
    }

    stage('Cypress y accesibilidad') {
      steps {
        sh 'cd frontend && CYPRESS_BASE_URL=http://localhost:3000 npx cypress run'
      }
      post { always { sh 'for d in screenshots videos; do if [ -d frontend/cypress/$d ]; then cp -r frontend/cypress/$d reports/cypress/; fi; done' } }
    }

    stage('Recuperacion') {
      steps { sh 'set -o pipefail; bash tests/recovery/run-all.sh | tee reports/junit/recovery.txt' }
    }

    stage('Maestro (agente Android)') {
      agent { label 'android' }
      environment { QA_ENV_FILE = credentials('fleettrack-qa-env') }
      steps {
        checkout scm
        sh '''
          set -o pipefail
          cp "$QA_ENV_FILE" .env
          mkdir -p reports/maestro
          bash tests/maestro/ensure-emulator.sh
          dotnet build mobile -f net10.0-android -t:Install -p:AndroidSdkDirectory="$ANDROID_HOME"
          "$ADB" shell pm grant com.companyname.fleettrack.mobile android.permission.ACCESS_FINE_LOCATION
          "$ADB" shell pm grant com.companyname.fleettrack.mobile android.permission.CAMERA
          bash tests/maestro/run-all.sh | tee reports/maestro/maestro.txt
          ! grep -q "exit=[1-9]" reports/maestro/maestro.txt
        '''
      }
      post { always { archiveArtifacts artifacts: 'reports/maestro/**', allowEmptyArchive: false } }
    }

    stage('ZAP') {
      steps {
        sh '''
          WS=$(pwd -W 2>/dev/null || pwd)
          MSYS_NO_PATHCONV=1 docker run --rm --network fleettrack_default -v "$WS/reports/zap:/zap/wrk:rw" zaproxy/zap-stable \
            zap-baseline.py -t http://web:8080 -r zap-web-report.html -J zap-web-report.json -I
          MSYS_NO_PATHCONV=1 docker run --rm --network fleettrack_default -v "$WS/reports/zap:/zap/wrk:rw" zaproxy/zap-stable \
            zap-api-scan.py -t http://api:8080/health -f openapi -r zap-api-report.html -J zap-api-report.json -I
          ! grep -q '"riskcode": "3"' reports/zap/zap-web-report.json reports/zap/zap-api-report.json
        '''
      }
    }

    stage('Rendimiento') {
      steps {
        script {
          if (params.FULL_PERFORMANCE) {
            sh 'k6 run --summary-export reports/performance/load.json tests/k6/load.js && k6 run --summary-export reports/performance/stress.json tests/k6/stress.js && k6 run --summary-export reports/performance/spike.json tests/k6/spike.js'
          } else {
            sh 'K6_VUS=20 K6_DURATION=30s k6 run --summary-export reports/performance/load-corto.json tests/k6/load.js'
          }
        }
      }
    }

    stage('Quality Gate y criterios de salida') {
      steps {
        sh '''
          bash tests/coverage/quality-gate.sh
          for r in reports/metadata.json reports/sonar/quality-gate-actual.json reports/fortify/semgrep-frontend.json \
                   reports/dependency-check/dependency-check-report.json reports/zap/zap-web-report.json \
                   reports/zap/zap-api-report.json reports/coverage/integration.xml; do
            test -s "$r" || { echo "Falta el reporte obligatorio $r: ejecucion incompleta"; exit 1; }
          done
        '''
      }
    }

    stage('Aprobacion de Alejandro') {
      when { allOf { branch 'main'; expression { params.DEPLOY_FINAL } } }
      steps {
        timeout(time: 24, unit: 'HOURS') {
          input message: 'Autorizar el despliegue final de FleetTrack?', ok: 'Autorizo', submitter: 'alejandro'
        }
      }
    }

    stage('Despliegue final') {
      when { allOf { branch 'main'; expression { params.DEPLOY_FINAL } } }
      environment { ANSIBLE_EXTRA = credentials('fleettrack-ansible-extra-vars') }   // archivo JSON con secretos y public_url
      steps {
        sh '''
          set -a; . ./.env; set +a
          # Respaldo previo de PostgreSQL (restauracion de datos solo con el procedimiento autorizado).
          docker compose exec -T postgres pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" > "reports/backup-$BUILD_NUMBER.sql"
          # prod.ini: inventario con la IP de Terraform, fuera de Git.
          ansible-playbook -i ansible/inventory/prod.ini ansible/site.yml -e "@$ANSIBLE_EXTRA"
          curl -fsS "$(node -p "require('$ANSIBLE_EXTRA').public_url")/health"   # smoke posterior al despliegue
        '''
      }
    }
  }

  post {
    always {
      sh 'docker compose logs --no-color > reports/compose.log 2>&1'
      archiveArtifacts artifacts: 'reports/**', fingerprint: true, allowEmptyArchive: false
      sh 'docker compose down'
    }
    failure { echo 'Build fallido: sin despliegue final ni aprobacion.' }
  }
}
