import { useEffect, useState, type FormEvent } from "react";
import { apiFetch } from "../api/client";

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
    <section>
      <h2>Asignar ruta</h2>

      <form onSubmit={handleSubmit}>
        <label htmlFor="driver-select">Conductor</label>
        <select id="driver-select" required value={driverId} onChange={(e) => setDriverId(e.target.value)}>
          <option value="">Selecciona...</option>
          {drivers.map((d) => <option key={d.id} value={d.id}>{d.fullName}</option>)}
        </select>

        <label htmlFor="vehicle-select">Vehiculo</label>
        <select id="vehicle-select" required value={vehicleId} onChange={(e) => setVehicleId(e.target.value)}>
          <option value="">Selecciona...</option>
          {vehicles.map((v) => <option key={v.id} value={v.id}>{v.plate} - {v.model}</option>)}
        </select>

        <label htmlFor="customer-select">Agregar cliente a la ruta</label>
        <select id="customer-select" value={customerToAdd} onChange={(e) => setCustomerToAdd(e.target.value)}>
          <option value="">Selecciona...</option>
          {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <button type="button" onClick={addCustomer}>Agregar</button>

        <ol>
          {orderedCustomerIds.map((id) => {
            const customer = customers.find((c) => c.id === id);
            return (
              <li key={id}>
                {customer?.name ?? id}
                <button type="button" onClick={() => removeCustomer(id)}>Quitar</button>
              </li>
            );
          })}
        </ol>

        {error && <p role="alert">{error}</p>}
        {message && <p>{message}</p>}

        <button type="submit">Crear ruta</button>
      </form>
    </section>
  );
}
