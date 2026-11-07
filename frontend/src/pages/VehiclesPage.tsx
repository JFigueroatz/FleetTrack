import { useEffect, useState, type FormEvent } from "react";
import { apiFetch, ApiError } from "../api/client";
import { Empty, ListSkeleton, matches, Notice, SearchBox, useNotice } from "../components/ui";

interface Vehicle {
  id: string;
  plate: string;
  model: string;
  capacityKg: number;
}

export function VehiclesPage() {
  const [vehicles, setVehicles] = useState<Vehicle[] | null>(null);
  const [plate, setPlate] = useState("");
  const [model, setModel] = useState("");
  const [capacityKg, setCapacityKg] = useState(0);
  const [query, setQuery] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useNotice();

  async function load() {
    setVehicles(await apiFetch<Vehicle[]>("/api/vehicles"));
  }

  useEffect(() => {
    load().catch(() => {
      setVehicles([]);
      setError("No se pudieron cargar los vehiculos.");
    });
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
      setNotice("Vehiculo agregado.");
      await load();
    } catch (err) {
      setError(err instanceof ApiError && err.status === 409
        ? "Ya existe un vehiculo con esa placa."
        : "No se pudo crear el vehiculo.");
    }
  }

  const visible = (vehicles ?? []).filter((v) => matches(query, v.plate, v.model)).reverse();

  return (
    <>
      <div className="page-head">
        <div>
          <h2>Vehiculos</h2>
          <p>La flota disponible para asignar a una ruta.</p>
        </div>
      </div>

      <div className="split">
        <form className="card form" onSubmit={handleSubmit}>
          <h3>Nuevo vehiculo</h3>

          <div className="field">
            <label htmlFor="vehicle-plate">Placa</label>
            <input id="vehicle-plate" required value={plate} onChange={(e) => setPlate(e.target.value)} />
          </div>

          <div className="field">
            <label htmlFor="vehicle-model">Modelo</label>
            <input id="vehicle-model" required value={model} onChange={(e) => setModel(e.target.value)} />
          </div>

          <div className="field">
            <label htmlFor="vehicle-capacity">Capacidad (kg)</label>
            <input id="vehicle-capacity" type="number" min={0} required value={capacityKg} onChange={(e) => setCapacityKg(Number(e.target.value))} />
          </div>

          {error && <Notice tone="bad" role="alert">{error}</Notice>}
          {notice && <Notice tone="ok" role="status" fade>{notice}</Notice>}

          <button type="submit">Agregar vehiculo</button>
        </form>

        <section className="card" aria-label="Lista de vehiculos">
          <div className="card-head">
            <h3>Vehiculos registrados</h3>
            <span className="chip brand num">{vehicles?.length ?? 0}</span>
          </div>
          <SearchBox label="Buscar por placa o modelo" value={query} onChange={setQuery} />

          {vehicles === null ? (
            <ListSkeleton />
          ) : visible.length === 0 ? (
            <Empty icon="vehicles" title={query ? "Sin resultados." : "Aun no hay vehiculos."}
              hint={query ? "Prueba con otra placa o modelo." : "Agrega el primero con el formulario."} />
          ) : (
            <ul className="rows">
              {visible.map((v) => (
                <li key={v.id}>
                  <span className="plate">{v.plate}</span>
                  <div className="main">
                    <strong>{v.model}</strong>
                  </div>
                  <span className="end num">{v.capacityKg.toLocaleString()} kg</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </>
  );
}
