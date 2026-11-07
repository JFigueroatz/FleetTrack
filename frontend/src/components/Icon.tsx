// Iconos propios en SVG (trazo de 1.75, cuadricula de 24): un solo estilo para toda la interfaz. Siempre decorativos
// (aria-hidden): el texto de cada control sigue siendo lo que lee un lector de pantalla.
const PATHS = {
  dashboard: "M4 4h7v7H4zM13 4h7v4h-7zM13 11h7v9h-7zM4 14h7v6H4z",
  route: "M6 19a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM18 9a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM8 17h6a3 3 0 0 0 0-6h-4a3 3 0 0 1 0-6h6",
  customers: "M9 11a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7zM2.5 20a6.5 6.5 0 0 1 13 0M16 4.3a3.5 3.5 0 0 1 0 6.4M18 14.2a6.5 6.5 0 0 1 3.5 5.8",
  vehicles: "M3 6h11v10H3zM14 9h4l3 3v4h-7zM7 19a1.8 1.8 0 1 0 0-3.6A1.8 1.8 0 0 0 7 19zM17 19a1.8 1.8 0 1 0 0-3.6 1.8 1.8 0 0 0 0 3.6z",
  drivers: "M4 5h16v14H4zM8.5 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM5.5 16.5a3 3 0 0 1 6 0M14 9h3.5M14 12.5h3.5",
  logout: "M9 4H5v16h4M16 8l4 4-4 4M20 12H9",
  search: "M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14zM20 20l-4-4",
  check: "M5 12.5l4.5 4.5L19 7.5",
  alert: "M12 4l9 16H3zM12 10v4M12 17.2v.1",
  signal: "M12 20a1.4 1.4 0 1 0 0-2.8 1.4 1.4 0 0 0 0 2.8zM7.8 14.6a6 6 0 0 1 8.4 0M4.5 11.3a10.6 10.6 0 0 1 15 0",
  inbox: "M4 13l2.5-7h11L20 13M4 13v6h16v-6M4 13h5l1 2h4l1-2h5",
  plus: "M12 5v14M5 12h14",
} as const;

export type IconName = keyof typeof PATHS;

export function Icon({ name, size = 20 }: { name: IconName; size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75"
      strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">
      <path d={PATHS[name]} />
    </svg>
  );
}

/** Marca de FleetTrack: un pin de ruta con su trayecto. */
export function BrandMark() {
  return (
    <svg className="brand-mark" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <rect width="32" height="32" rx="9" fill="#512bd4" />
      <path d="M9 22.5c3 0 4-2 7-2s4 2 7 2" fill="none" stroke="#fff" strokeOpacity="0.55" strokeWidth="2" strokeLinecap="round" strokeDasharray="1 4.2" />
      <path d="M16 7.5c-3 0-5.2 2.2-5.2 5 0 3.4 5.2 8 5.2 8s5.2-4.6 5.2-8c0-2.8-2.2-5-5.2-5z" fill="#fff" />
      <circle cx="16" cy="12.5" r="1.9" fill="#512bd4" />
    </svg>
  );
}

export function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  return ((parts[0]?.[0] ?? "?") + (parts.length > 1 ? parts[parts.length - 1][0] : "")).toUpperCase();
}
