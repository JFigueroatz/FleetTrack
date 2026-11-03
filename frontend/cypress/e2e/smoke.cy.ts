// Smoke: lo minimo para confirmar que el frontend sirve, llama a la API real y las rutas principales cargan.
describe("Smoke", () => {
  it("la pantalla de login carga", () => {
    cy.visit("/login");
    cy.contains("h1", "FleetTrack").should("be.visible");
    cy.get("#email").should("be.visible");
    cy.get("#password").should("be.visible");
  });

  it("el supervisor inicia sesion y ve el dashboard", () => {
    cy.loginAsSupervisor();
    cy.contains("h2", "Dashboard de seguimiento").should("be.visible");
  });

  it("la navegacion a las secciones principales no rompe", () => {
    cy.loginAsSupervisor();
    for (const [link, heading] of [
      ["Rutas", "Asignar ruta"],
      ["Clientes", "Clientes"],
      ["Vehiculos", "Vehiculos"],
      ["Conductores", "Conductores"],
    ] as const) {
      cy.contains("nav a", link).click();
      cy.contains("h2", heading).should("be.visible");
    }
  });
});
