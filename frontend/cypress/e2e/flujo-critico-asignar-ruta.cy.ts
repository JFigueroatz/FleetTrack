// Flujo critico: el supervisor crea cliente, vehiculo y conductor, arma una ruta y la ve reflejada.
// Usa datos unicos por corrida (timestamp) para poder repetirse contra la misma base sin chocar con restricciones unicas.
describe("Flujo critico — supervisor asigna una ruta completa", () => {
  const stamp = Date.now();

  it("crea cliente, vehiculo, conductor y los usa para armar una ruta", () => {
    cy.loginAsSupervisor();

    // Cliente
    cy.contains("nav a", "Clientes").click();
    cy.get("#customer-name").type(`Cliente Cypress ${stamp}`);
    cy.get("#customer-address").type("Zona 10, Ciudad de Guatemala");
    cy.contains("button", "Agregar cliente").click();
    cy.contains(`Cliente Cypress ${stamp}`).should("be.visible");

    // Vehiculo
    cy.contains("nav a", "Vehiculos").click();
    cy.get("#vehicle-plate").type(`CYP-${stamp}`.slice(0, 10));
    cy.get("#vehicle-model").type("Furgon de prueba");
    cy.get("#vehicle-capacity").clear().type("1200");
    cy.contains("button", "Agregar vehiculo").click();
    cy.contains(`CYP-${stamp}`.slice(0, 10)).should("be.visible");

    // Conductor
    cy.contains("nav a", "Conductores").click();
    cy.get("#driver-name").type(`Conductor Cypress ${stamp}`);
    cy.get("#driver-email").type(`cypress.${stamp}@fleettrack.test`);
    cy.get("#driver-password").type("Cypress123!");
    cy.contains("button", "Agregar conductor").click();
    cy.contains(`Conductor Cypress ${stamp}`).should("be.visible");

    // Asignar ruta con lo recien creado
    cy.contains("nav a", "Rutas").click();
    const plate = `CYP-${stamp}`.slice(0, 10);
    cy.get("#driver-select").select(`Conductor Cypress ${stamp}`);
    cy.get("#vehicle-select").contains("option", plate).then((option) => {
      cy.get("#vehicle-select").select(option.val() as string);
    });
    cy.get("#customer-select").select(`Cliente Cypress ${stamp}`);
    cy.contains("button", "Agregar").click();
    cy.contains("li", `Cliente Cypress ${stamp}`).should("be.visible");

    cy.contains("button", "Crear ruta").click();
    cy.contains("Ruta creada y asignada.").should("be.visible");
  });
});
