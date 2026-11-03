/// <reference types="cypress" />
import "axe-core";

declare global {
  // eslint-disable-next-line @typescript-eslint/no-namespace
  namespace Cypress {
    interface Chainable {
      /** Inicia sesion por la UI (no por API) con el supervisor de prueba del seed. */
      loginAsSupervisor(): Chainable<void>;
      /** Inyecta axe-core en la pagina actual (cypress-axe no soporta Cypress 16 todavia, se usa axe-core directo). */
      injectAxe(): Chainable<void>;
      /** Corre axe-core y falla la prueba si hay hallazgos criticos o serios de WCAG 2 A/AA. */
      checkA11y(): Chainable<void>;
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

Cypress.Commands.add("injectAxe", () => {
  cy.readFile("node_modules/axe-core/axe.min.js").then((script) => {
    cy.window({ log: false }).then((win) => win.eval(script as string));
  });
});

Cypress.Commands.add("checkA11y", () => {
  cy.window({ log: false }).then((win) => {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const axe = (win as any).axe;
    return cy.wrap(
      axe.run(win.document, { runOnly: { type: "tag", values: ["wcag2a", "wcag2aa"] } }) as Promise<{
        violations: Array<{ id: string; impact: string; help: string; nodes: unknown[] }>;
      }>,
      { log: false },
    );
  }).then((results) => {
    const serious = results.violations.filter((v) => v.impact === "critical" || v.impact === "serious");
    if (serious.length > 0) {
      cy.task("log", serious.map((v) => `${v.impact}: ${v.id} — ${v.help} (${v.nodes.length} nodo(s))`).join("\n"), { log: false });
    }
    expect(serious, `hallazgos criticos/serios de axe: ${JSON.stringify(serious.map((v) => v.id))}`).to.have.length(0);
  });
});

export {};
