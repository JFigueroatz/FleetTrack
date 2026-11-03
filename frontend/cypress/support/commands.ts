/// <reference types="cypress" />

declare global {
  // eslint-disable-next-line @typescript-eslint/no-namespace
  namespace Cypress {
    interface Chainable {
      /** Inicia sesion por la UI (no por API) con el supervisor de prueba del seed. */
      loginAsSupervisor(): Chainable<void>;
    }
  }
}

Cypress.Commands.add("loginAsSupervisor", () => {
  cy.visit("/login");
  cy.get("#email").type("supervisor@fleettrack.test");
  cy.get("#password").type("Supervisor123!");
  cy.contains("button", "Ingresar").click();
  cy.url().should("include", "/dashboard");
});

export {};
