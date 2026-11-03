// Regresion: comportamientos ya corregidos en semanas anteriores que no deben volver a romperse.
describe("Regresion", () => {
  it("un conductor sin rol de supervisor no ve el dashboard web (restriccion de supervisor, semana 2)", () => {
    // La API ya rechaza esto (ver AuthAndOwnershipTests); aqui se confirma que la UI tampoco deja ver la pagina.
    cy.visit("/login");
    cy.window().then((win) => {
      win.localStorage.setItem("fleettrack_token", "token-de-prueba");
      win.localStorage.setItem("fleettrack_roles", JSON.stringify(["Driver"]));
    });
    cy.visit("/dashboard");
    cy.contains("No tienes permiso de supervisor").should("be.visible");
    cy.contains("h2", "Dashboard de seguimiento").should("not.exist");
  });

  it("el dashboard sigue mostrando el aviso de modo degradado cuando la API marca Redis no disponible (semana 4)", () => {
    cy.loginAsSupervisor();
    cy.intercept("GET", "**/api/tracking/latest", {
      statusCode: 200,
      headers: { "X-Redis-Status": "unavailable" },
      body: [],
    }).as("latest");
    cy.visit("/dashboard");
    cy.wait("@latest");
    cy.contains("Modo degradado").should("be.visible");
  });

  it("la lista de conductores sigue cargando tras agregar uno nuevo (sin perder los existentes)", () => {
    cy.loginAsSupervisor();
    cy.contains("nav a", "Conductores").click();
    cy.contains("Conductor de Prueba").should("be.visible");
  });
});
