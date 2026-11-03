// Negativo: entradas invalidas y errores de la API deben mostrarse al usuario, nunca romper la pagina silenciosamente.
describe("Casos negativos", () => {
  it("login con credenciales incorrectas muestra el error y no navega", () => {
    cy.visit("/login");
    cy.get("#email").type("supervisor@fleettrack.test");
    cy.get("#password").type("password-incorrecta");
    cy.contains("button", "Ingresar").click();

    cy.contains("Correo o contrasena incorrectos.").should("be.visible");
    cy.url().should("include", "/login");
  });

  it("crear un vehiculo con placa duplicada muestra el error de conflicto", () => {
    cy.loginAsSupervisor();
    cy.contains("nav a", "Vehiculos").click();

    cy.get("#vehicle-plate").type("P-000TEST"); // placa del seed, ya existe
    cy.get("#vehicle-model").type("Duplicado");
    cy.get("#vehicle-capacity").clear().type("500");
    cy.contains("button", "Agregar vehiculo").click();

    cy.contains("Ya existe un vehiculo con esa placa.").should("be.visible");
  });

  it("crear una ruta sin elegir conductor, vehiculo o cliente no la envia", () => {
    cy.intercept("POST", "**/api/routes").as("createRoute");
    cy.loginAsSupervisor();
    cy.contains("nav a", "Rutas").click();
    cy.contains("button", "Crear ruta").click();

    // El select de conductor es "required": el navegador bloquea el submit antes de que corra el JS de la pagina.
    cy.get("#driver-select:invalid").should("exist");
    cy.get("@createRoute.all").should("have.length", 0);
  });

  it("el formulario de login no envia con campos vacios (validacion HTML required)", () => {
    cy.visit("/login");
    cy.contains("button", "Ingresar").click();
    cy.get("#email:invalid").should("exist");
    cy.url().should("include", "/login");
  });
});
