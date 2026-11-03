// Accesibilidad: axe-core contra las pantallas principales. Reglas wcag2a y wcag2aa, sin hallazgos criticos/serios.
function checkPage(path: string, heading: string) {
  cy.visit(path);
  if (heading) cy.contains(heading).should("be.visible");
  cy.injectAxe();
  cy.checkA11y();
}

describe("Accesibilidad (axe-core)", () => {
  it("login sin hallazgos criticos/serios", () => {
    checkPage("/login", "FleetTrack");
  });

  it("dashboard sin hallazgos criticos/serios", () => {
    cy.loginAsSupervisor();
    cy.injectAxe();
    cy.checkA11y();
  });

  it("conductores sin hallazgos criticos/serios", () => {
    cy.loginAsSupervisor();
    cy.contains("nav a", "Conductores").click();
    cy.injectAxe();
    cy.checkA11y();
  });

  it("asignar ruta sin hallazgos criticos/serios", () => {
    cy.loginAsSupervisor();
    cy.contains("nav a", "Rutas").click();
    cy.injectAxe();
    cy.checkA11y();
  });
});
