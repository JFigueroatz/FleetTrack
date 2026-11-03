import { useEffect, useState, type FormEvent } from "react";
import { apiFetch, ApiError } from "../api/client";

interface Vehicle {
  id: string;
  plate: string;
  model: string;
  capacityKg: number;
}

export function VehiclesPage() {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [plate, setPlate] = useState("");
  const [model, setModel] = useState("");
  const [capacityKg, setCapacityKg] = useState(0);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setVehicles(await apiFetch<Vehicle[]>("/api/vehicles"));
  }

  useEffect(() => {
    load().catch(() => setError("No se pudieron cargar los vehiculos."));
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await apiFetch<Vehicle>("/api/vehicles", {
        method: "POST",
        body: JSON.stringify({ plate, model, capacityKg }),
      });
      setPlate("");
      setModel("");
      setCapacityKg(0);
      await load();
    } catch (err) {
      setError(err instanceof ApiError && err.status === 409
        ? "Ya existe un vehiculo con esa placa."
        : "No se pudo crear el vehiculo.");
    }
  }

  return (
    <section>
      <h2>Vehiculos</h2>

      <form onSubmit={handleSubmit}>
        <label htmlFor="vehicle-plate">Placa</label>
        <input id="vehicle-plate" required value={plate} onChange={(e) => setPlate(e.target.value)} />

        <label htmlFor="vehicle-model">Modelo</label>
        <input id="vehicle-model" required value={model} onChange={(e) => setModel(e.target.value)} />

        <label htmlFor="vehicle-capacity">Capacidad (kg)</label>
        <input id="vehicle-capacity" type="number" min={0} required value={capacityKg} onChange={(e) => setCapacityKg(Number(e.target.value))} />

        {error && <p role="alert">{error}</p>}

        <button type="submit">Agregar vehiculo</button>
      </form>

      <ul>
        {vehicles.map((v) => (
          <li key={v.id}>
            {v.plate} - {v.model} - {v.capacityKg} kg
          </li>
        ))}
      </ul>
    </section>
  );
}
