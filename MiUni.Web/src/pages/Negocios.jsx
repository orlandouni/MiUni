import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api, { getMe } from "../services/api";
import { canManageBusiness } from "../services/roles";
import "./Negocios.css";
import { negocioError } from "../services/negocioErrors";

export default function Negocios() {
  const [items, setItems] = useState([]);
  const [categories, setCategories] = useState([]);
  const [roles, setRoles] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [editing, setEditing] = useState(null);
  useEffect(() => {
    let active = true;
    Promise.all([api.get("/api/Negocio/mios"), api.get("/api/Categoria"), getMe()])
      .then(([businesses, categories, user]) => {
        if (active) { setItems(businesses.data); setCategories(categories.data); setRoles(user.roles || []); }
      }).catch(e => { if (active) setError(negocioError(e)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);

  async function submit(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const values = Object.fromEntries(new FormData(form));
    setBusy(true); setError(""); setMessage("");
    try {
      if (editing) {
        const { data } = await api.put(`/api/Negocio/${editing.id}`, values);
        setItems(current => current.map(item => item.id === data.id ? data : item));
        setEditing(null); setMessage("Cambios guardados.");
      } else {
        const { data } = await api.post("/api/Negocio/solicitudes", {
          ...values, latitud: Number(values.latitud), longitud: Number(values.longitud),
        });
        setItems(current => [...current, data]); form.reset();
        setMessage("Solicitud enviada. Un administrador revisará tu negocio antes de publicarlo.");
      }
    } catch (e) { setError(negocioError(e)); }
    finally { setBusy(false); }
  }

  return <main className="negocios">
    <Link to="/">← Volver al mapa</Link>
    <h1>Mis negocios</h1>
    <p>Solicita el registro de tu negocio. Cuando se apruebe, podrás administrar su información.</p>
    {error && <p role="alert">{error}</p>}
    {message && <p role="status">{message}</p>}
    {loading ? <p role="status">Cargando…</p> : <>
      <section aria-label="Mis solicitudes">
        {items.length === 0 && <p>Aún no has registrado negocios.</p>}
        {items.map(item => <article key={item.id}>
          <h2>{item.nombre}</h2><p>{item.descripcion}</p><p>Estado: {item.estadoSolicitud}</p>
          {item.estadoSolicitud === "Aprobada" && canManageBusiness(roles) &&
            <button disabled={busy} onClick={() => setEditing(item)}>Editar negocio</button>}
        </article>)}
      </section>
      <h2>{editing ? "Editar negocio" : "Registrar mi negocio"}</h2>
      <form key={editing?.id || "nuevo"} onSubmit={submit}>
        <fieldset disabled={busy}>
          <label>Nombre<input name="nombre" required maxLength={150} defaultValue={editing?.nombre || ""} /></label>
          <label>Descripción<textarea name="descripcion" maxLength={2000} defaultValue={editing?.descripcion || ""} /></label>
          {!editing && <>
            <label>Categoría<select name="categoriaId" required defaultValue="">
              <option value="" disabled>Selecciona una categoría</option>
              {categories.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
            </select></label>
            <label>Latitud<input name="latitud" type="number" step="any" min="-90" max="90" required /></label>
            <label>Longitud<input name="longitud" type="number" step="any" min="-180" max="180" required /></label>
          </>}
          <button type="submit">{busy ? "Guardando…" : editing ? "Guardar cambios" : "Enviar solicitud"}</button>
          {editing && <button type="button" onClick={() => setEditing(null)}>Cancelar</button>}
        </fieldset>
      </form>
    </>}
  </main>;
}
