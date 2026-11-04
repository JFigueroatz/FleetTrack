// Accesibilidad: axe-core contra las pantallas principales. Reglas wcag2a y wcag2aa, sin hallazgos criticos/serios.
// Cada prueba confirma primero que la pantalla cargo (aserciones .should explicitas) y luego pasa axe.
describe("Accesibilidad (axe-core)", () => {
  it("login sin hallazgos criticos/serios", () => {
    cy.visit("/login");
    cy.contains("h1", "FleetTrack").should("be.visible");
    cy.injectAxe();
    cy.checkA11y();
  });

  it("dashboard sin hallazgos criticos/serios", () => {
    cy.loginAsSupervisor();
    cy.contains("h2", "Dashboard de seguimiento").should("be.visible");
    cy.injectAxe();
    cy.checkA11y();
  });

  it("conductores sin hallazgos criticos/serios", () => {
    cy.loginAsSupervisor();
    cy.contains("nav a", "Conductores").click();
    cy.contains("h2", "Conductores").should("be.visible");
    cy.injectAxe();
    cy.checkA11y();
  });

  it("asignar ruta sin hallazgos criticos/serios", () => {
    cy.loginAsSupervisor();
    cy.contains("nav a", "Rutas").click();
    cy.contains("h2", "Asignar ruta").should("be.visible");
    cy.injectAxe();
    cy.checkA11y();
  });
});
