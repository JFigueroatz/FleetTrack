import { useEffect, useState, type FormEvent } from "react";
import { apiFetch } from "../api/client";
import { initials } from "../components/Icon";
import { Empty, ListSkeleton, matches, Notice, SearchBox, useNotice } from "../components/ui";

interface Customer {
  id: string;
  name: string;
  address: string;
  phone: string | null;
}

export function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[] | null>(null);
  const [name, setName] = useState("");
  const [address, setAddress] = useState("");
  const [phone, setPhone] = useState("");
  const [query, setQuery] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useNotice();

  async function load() {
    setCustomers(await apiFetch<Customer[]>("/api/customers"));
  }

  useEffect(() => {
    load().catch(() => {
      setCustomers([]);
      setError("No se pudieron cargar los clientes.");
    });
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await apiFetch<Customer>("/api/customers", {
        method: "POST",
        body: JSON.stringify({ name, address, phone: phone || null }),
      });
      setName("");
      setAddress("");
      setPhone("");
      setNotice("Cliente agregado.");
      await load();
    } catch {
      setError("No se pudo crear el cliente.");
    }
  }

  const visible = (customers ?? []).filter((c) => matches(query, c.name, c.address, c.phone)).reverse();

  return (
    <>
      <div className="page-head">
        <div>
          <h2>Clientes</h2>
          <p>Destinos de entrega. Despues los ordenas en una ruta.</p>
        </div>
      </div>

      <div className="split">
        <form className="card form" onSubmit={handleSubmit}>
          <h3>Nuevo cliente</h3>

          <div className="field">
            <label htmlFor="customer-name">Nombre</label>
            <input id="customer-name" required value={name} onChange={(e) => setName(e.target.value)} />
          </div>

          <div className="field">
            <label htmlFor="customer-address">Direccion</label>
            <input id="customer-address" required value={address} onChange={(e) => setAddress(e.target.value)} />
          </div>

          <div className="field">
            <label htmlFor="customer-phone">Telefono</label>
            <input id="customer-phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
          </div>

          {error && <Notice tone="bad" role="alert">{error}</Notice>}
          {notice && <Notice tone="ok" role="status" fade>{notice}</Notice>}

          <button type="submit">Agregar cliente</button>
        </form>

        <section className="card" aria-label="Lista de clientes">
          <div className="card-head">
            <h3>Clientes registrados</h3>
            <span className="chip brand num">{customers?.length ?? 0}</span>
          </div>
          <SearchBox label="Buscar cliente" value={query} onChange={setQuery} />

          {customers === null ? (
            <ListSkeleton />
          ) : visible.length === 0 ? (
            <Empty icon="customers" title={query ? "Sin resultados." : "Aun no hay clientes."}
              hint={query ? "Prueba con otro nombre o direccion." : "Agrega el primero con el formulario."} />
          ) : (
            <ul className="rows">
              {visible.map((c) => (
                <li key={c.id}>
                  <span className="avatar" aria-hidden="true">{initials(c.name)}</span>
                  <div className="main">
                    <strong>{c.name}</strong>
                    <span>{c.address}{c.phone ? ` · ${c.phone}` : ""}</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </>
  );
}
