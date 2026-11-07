import { useEffect, useState, type FormEvent } from "react";
import { apiFetch } from "../api/client";
import { Empty, Notice } from "../components/ui";

interface Driver { id: string; fullName: string; email: string }
interface Vehicle { id: string; plate: string; model: string; capacityKg: number }
interface Customer { id: string; name: string; address: string; phone: string | null }

export function AssignRoutePage() {
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);

  const [driverId, setDriverId] = useState("");
  const [vehicleId, setVehicleId] = useState("");
  const [customerToAdd, setCustomerToAdd] = useState("");
  const [orderedCustomerIds, setOrderedCustomerIds] = useState<string[]>([]);

  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      apiFetch<Driver[]>("/api/drivers"),
      apiFetch<Vehicle[]>("/api/vehicles"),
      apiFetch<Customer[]>("/api/customers"),
    ])
      .then(([d, v, c]) => {
        setDrivers(d);
        setVehicles(v);
        setCustomers(c);
      })
      .catch(() => setError("No se pudieron cargar conductores, vehiculos o clientes."));
  }, []);

  useEffect(() => {
    if (!message) return;
    const t = setTimeout(() => setMessage(null), 5000);
    return () => clearTimeout(t);
  }, [message]);

  function addCustomer() {
    if (customerToAdd && !orderedCustomerIds.includes(customerToAdd)) {
      setOrderedCustomerIds([...orderedCustomerIds, customerToAdd]);
      setCustomerToAdd("");
    }
  }

  function removeCustomer(id: string) {
    setOrderedCustomerIds(orderedCustomerIds.filter((c) => c !== id));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setMessage(null);

    if (!driverId || !vehicleId || orderedCustomerIds.length === 0) {
      setError("Selecciona conductor, vehiculo y al menos un cliente.");
      return;
    }

    try {
      await apiFetch("/api/routes", {
        method: "POST",
        body: JSON.stringify({
          driverId,
          vehicleId,
          deliveries: orderedCustomerIds.map((customerId, index) => ({ customerId, sequence: index + 1 })),
        }),
      });
      setMessage("Ruta creada y asignada.");
      setOrderedCustomerIds([]);
    } catch {
      setError("No se pudo crear la ruta.");
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h2>Asignar ruta</h2>
          <p>Elige conductor y vehiculo, y agrega los clientes en el orden en que se visitaran.</p>
        </div>
      </div>

      <form className="split" onSubmit={handleSubmit}>
        <div className="card form">
          <h3>Datos de la ruta</h3>

          <div className="field">
            <label htmlFor="driver-select">Conductor</label>
            <select id="driver-select" required value={driverId} onChange={(e) => setDriverId(e.target.value)}>
              <option value="">Selecciona...</option>
              {drivers.map((d) => <option key={d.id} value={d.id}>{d.fullName}</option>)}
            </select>
          </div>

          <div className="field">
            <label htmlFor="vehicle-select">Vehiculo</label>
            <select id="vehicle-select" required value={vehicleId} onChange={(e) => setVehicleId(e.target.value)}>
              <option value="">Selecciona...</option>
              {vehicles.map((v) => <option key={v.id} value={v.id}>{v.plate} - {v.model}</option>)}
            </select>
          </div>

          <div className="form-row">
            <div className="field">
              <label htmlFor="customer-select">Agregar cliente a la ruta</label>
              <select id="customer-select" value={customerToAdd} onChange={(e) => setCustomerToAdd(e.target.value)}>
                <option value="">Selecciona...</option>
                {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <button type="button" className="btn-quiet" onClick={addCustomer}>Agregar</button>
          </div>
        </div>

        <section className="card form" aria-label="Orden de entregas">
          <div className="card-head">
            <h3>Orden de entregas</h3>
            <span className="chip brand num">{orderedCustomerIds.length}</span>
          </div>

          {orderedCustomerIds.length === 0 ? (
            <Empty icon="route" title="Aun no hay paradas." hint="Agrega clientes en el orden de entrega: la primera parada es la primera que visitara el conductor." />
          ) : (
            <ol className="stops">
              {orderedCustomerIds.map((id) => {
                const customer = customers.find((c) => c.id === id);
                return (
                  <li key={id}>
                    <span>{customer?.name ?? id}</span>
                    <button type="button" className="btn-quiet btn-small" onClick={() => removeCustomer(id)}>Quitar</button>
                  </li>
                );
              })}
            </ol>
          )}

          {error && <Notice tone="bad" role="alert">{error}</Notice>}
          {message && <Notice tone="ok" role="status" fade>{message}</Notice>}

          <button type="submit">Crear ruta</button>
        </section>
      </form>
    </>
  );
}
