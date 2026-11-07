import { useEffect, useState, type ReactNode } from "react";
import { Icon, type IconName } from "./Icon";

/** Mensaje de estado/alerta. Los textos los define cada pagina; aqui solo vive la forma. */
export function Notice({ tone, role, children, fade }: { tone: "ok" | "warn" | "bad"; role?: "alert" | "status"; children: ReactNode; fade?: boolean }) {
  return (
    <div className={`notice ${tone}${fade ? " fade" : ""}`} role={role}>
      <Icon name={tone === "ok" ? "check" : "alert"} size={18} />
      <span>{children}</span>
    </div>
  );
}

/** Aviso de exito que se retira solo a los 4 s. */
export function useNotice(): [string | null, (message: string) => void] {
  const [message, setMessage] = useState<string | null>(null);
  useEffect(() => {
    if (!message) return;
    const t = setTimeout(() => setMessage(null), 4000);
    return () => clearTimeout(t);
  }, [message]);
  return [message, setMessage];
}

export function SearchBox({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) {
  return (
    <div className="search">
      <Icon name="search" size={18} />
      <input type="search" aria-label={label} placeholder={label} value={value} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

export function Empty({ icon, title, hint }: { icon: IconName; title: string; hint: string }) {
  return (
    <div className="empty">
      <Icon name={icon} size={40} />
      <strong>{title}</strong>
      <p>{hint}</p>
    </div>
  );
}

export function ListSkeleton() {
  return (
    <div aria-hidden="true">
      <div className="skeleton" />
      <div className="skeleton" />
      <div className="skeleton" />
    </div>
  );
}

/** Coincidencia sin distinguir mayusculas ni acentos, para el buscador de las listas. */
export function matches(query: string, ...fields: Array<string | null | undefined>) {
  const norm = (s: string) => s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();
  const q = norm(query.trim());
  return q === "" || fields.some((f) => f && norm(f).includes(q));
}
