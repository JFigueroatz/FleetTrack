import { useEffect, useState, type FormEvent } from "react";
import { apiFetch } from "../api/client";

interface Customer {
  id: string;
  name: string;
  address: string;
  phone: string | null;
}

export function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [name, setName] = useState("");
  const [address, setAddress] = useState("");
  const [phone, setPhone] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setCustomers(await apiFetch<Customer[]>("/api/customers"));
  }

  useEffect(() => {
    load().catch(() => setError("No se pudieron cargar los clientes."));
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
      await load();
    } catch {
      setError("No se pudo crear el cliente.");
    }
  }

  return (
    <section>
      <h2>Clientes</h2>

      <form onSubmit={handleSubmit}>
        <label htmlFor="customer-name">Nombre</label>
        <input id="customer-name" required value={name} onChange={(e) => setName(e.target.value)} />

        <label htmlFor="customer-address">Direccion</label>
        <input id="customer-address" required value={address} onChange={(e) => setAddress(e.target.value)} />

        <label htmlFor="customer-phone">Telefono</label>
        <input id="customer-phone" value={phone} onChange={(e) => setPhone(e.target.value)} />

        {error && <p role="alert">{error}</p>}

        <button type="submit">Agregar cliente</button>
      </form>

      <ul>
        {customers.map((c) => (
          <li key={c.id}>
            {c.name} - {c.address} {c.phone ? `- ${c.phone}` : ""}
          </li>
        ))}
      </ul>
    </section>
  );
}
