import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../services/api";
import { negocioError } from "../services/negocioErrors";
import "./Negocios.css";

export default function Admin() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  useEffect(() => {
    let active = true;
    api.get("/api/Negocio/solicitudes").then(({ data }) => { if (active) setItems(data); })
      .catch(e => { if (active) setError(negocioError(e)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);
  async function revisar(id, action) {
    setBusy(true); setError(""); setMessage("");
    try {
      await api.post(`/api/Negocio/solicitudes/${id}/${action}`);
      setItems(current => current.filter(item => item.id !== id));
      setMessage(action === "aprobar" ? "Negocio publicado y permiso de propietario asignado." : "Solicitud rechazada.");
    } catch (e) { setError(negocioError(e)); }
    finally { setBusy(false); }
  }
  return <main className="negocios">
    <Link to="/">← Volver al mapa</Link><h1>Administración</h1><h2>Solicitudes de negocios</h2>
    {error && <p role="alert">{error}</p>}{message && <p role="status">{message}</p>}
    {loading ? <p role="status">Cargando…</p> : !error && items.length === 0 && <p>No hay solicitudes pendientes.</p>}
    {items.map(item => <article key={item.id}>
      <h3>{item.nombre}</h3><p>{item.descripcion}</p>
      <p>Ubicación: {item.latitud}, {item.longitud}</p>
      <button disabled={busy} onClick={() => revisar(item.id, "aprobar")}>Aprobar y publicar</button>
      <button disabled={busy} onClick={() => revisar(item.id, "rechazar")}>Rechazar</button>
    </article>)}
  </main>;
}
