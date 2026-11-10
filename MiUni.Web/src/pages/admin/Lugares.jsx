import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { MapContainer, TileLayer, CircleMarker, useMapEvents } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import { EyeOff, Pencil, Plus, RotateCcw, Search } from "lucide-react";
import { adminApi } from "../../services/admin";
import { negocioError } from "../../services/negocioErrors";
import { LIMITES_MAPA, TESELAS } from "../../components/mapaBase";
import LimitarAlCampus from "../../components/LimitarAlCampus";
import { Aviso, Cargando, Confirmacion, Encabezado, Insignia, Modal, Vacio, useAccion, useCarga } from "./ui";

const CENTRO = [29.0833, -110.9627];

const cuerpo = l => ({
  nombre: l.nombre, descripcion: l.descripcion || null, categoriaId: l.categoriaId,
  latitud: Number(l.latitud), longitud: Number(l.longitud),
  piso: l.piso === "" || l.piso === null || l.piso === undefined ? null : Number(l.piso), activo: !!l.activo,
});

export default function Lugares({ onCambio }) {
  const lugares = useCarga(adminApi.lugares);
  const categorias = useCarga(adminApi.categorias);
  const { ocupado, aviso, setAviso, ejecutar } = useAccion();
  const [filtro, setFiltro] = useState({ q: "", categoria: "", activo: "" });
  const [editando, setEditando] = useState(null);
  const [confirmar, setConfirmar] = useState(null);

  const visibles = useMemo(() => {
    const q = filtro.q.trim().toLowerCase();
    return (lugares.datos || []).filter(l =>
      (!q || l.nombre.toLowerCase().includes(q) || l.descripcion?.toLowerCase().includes(q)) &&
      (!filtro.categoria || l.categoriaId === filtro.categoria) &&
      (filtro.activo === "" || String(l.activo) === filtro.activo));
  }, [lugares.datos, filtro]);

  const tras = ok => { if (ok) { lugares.recargar(); onCambio(); } return ok; };

  const cambiarActivo = l => l.activo
    ? setConfirmar({
      titulo: "Ocultar lugar", boton: "Ocultar",
      texto: `"${l.nombre}" dejará de mostrarse en el mapa. Sus reseñas y favoritos se conservan.`,
      accion: async () => tras(await ejecutar(l.id, () => adminApi.desactivarLugar(l.id), "Lugar ocultado.")),
    })
    : ejecutar(l.id, () => adminApi.editarLugar(l.id, cuerpo({ ...l, activo: true })), "Lugar publicado de nuevo.").then(tras);

  return <section className="adm-card">
    <Encabezado titulo="Lugares" descripcion="Edificios, servicios y negocios que aparecen en el mapa.">
      <button type="button" className="adm-btn primario" onClick={() => setEditando({ nombre: "", descripcion: "", categoriaId: categorias.datos?.[0]?.id || "", latitud: CENTRO[0], longitud: CENTRO[1], piso: "", activo: true })}>
        <Plus size={15} /> Nuevo lugar
      </button>
    </Encabezado>
    <Aviso aviso={aviso || ((lugares.error || categorias.error) && { tipo: "error", texto: lugares.error || categorias.error })} onCerrar={() => setAviso(null)} />

    <div className="adm-filters">
      <label className="adm-search"><Search size={15} /><input type="search" placeholder="Buscar por nombre o descripción" value={filtro.q} onChange={e => setFiltro(f => ({ ...f, q: e.target.value }))} /></label>
      <select aria-label="Categoría" value={filtro.categoria} onChange={e => setFiltro(f => ({ ...f, categoria: e.target.value }))}>
        <option value="">Todas las categorías</option>
        {categorias.datos?.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
      </select>
      <select aria-label="Estado" value={filtro.activo} onChange={e => setFiltro(f => ({ ...f, activo: e.target.value }))}>
        <option value="">Activos e inactivos</option><option value="true">Solo activos</option><option value="false">Solo inactivos</option>
      </select>
    </div>

    {lugares.cargando && !lugares.datos ? <Cargando texto="Cargando lugares…" /> : visibles.length === 0 ?
      <Vacio titulo="Sin resultados" texto="Ajusta los filtros o crea un lugar nuevo." /> :
      <div className="adm-table-wrap"><table className="adm-table">
        <thead><tr><th>Nombre</th><th>Categoría</th><th>Piso</th><th>Tipo</th><th>Estado</th><th aria-label="Acciones" /></tr></thead>
        <tbody>{visibles.map(l => <tr key={l.id} className={l.activo ? "" : "inactivo"}>
          <td><strong>{l.activo ? <Link to={`/lugares/${l.id}`}>{l.nombre}</Link> : l.nombre}</strong>{l.descripcion && <small>{l.descripcion}</small>}</td>
          <td>{l.nombreCategoria}</td>
          <td>{l.piso ?? "—"}</td>
          <td>{l.propietarioId ? <Insignia>Negocio · {l.estadoSolicitud}</Insignia> : "Campus"}</td>
          <td><Insignia tono={l.activo ? "ok" : "neutro"}>{l.activo ? "Publicado" : "Oculto"}</Insignia></td>
          <td className="adm-row-actions">
            <button type="button" className="adm-icon-btn" title="Editar" aria-label={`Editar ${l.nombre}`} onClick={() => setEditando({ ...l, piso: l.piso ?? "", descripcion: l.descripcion ?? "" })}><Pencil size={15} /></button>
            <button type="button" className="adm-icon-btn" disabled={ocupado === l.id || (!l.activo && l.propietarioId && l.estadoSolicitud !== "Aprobada")}
              title={l.activo ? "Ocultar" : "Publicar"} aria-label={`${l.activo ? "Ocultar" : "Publicar"} ${l.nombre}`} onClick={() => cambiarActivo(l)}>
              {l.activo ? <EyeOff size={15} /> : <RotateCcw size={15} />}
            </button>
          </td>
        </tr>)}</tbody>
      </table></div>}
    <p className="adm-pager-info">{visibles.length} de {lugares.datos?.length ?? 0} lugares</p>

    {editando && <FormLugar lugar={editando} categorias={categorias.datos || []} onCerrar={() => setEditando(null)}
      onGuardado={texto => { setEditando(null); setAviso({ tipo: "ok", texto }); tras(true); }} />}
    <Confirmacion confirmar={confirmar} onCerrar={() => setConfirmar(null)} ocupado={!!ocupado} />
  </section>;
}

function SelectorMapa({ posicion, onCambio }) {
  useMapEvents({ click: e => onCambio(e.latlng.lat, e.latlng.lng) });
  return <CircleMarker center={posicion} radius={10} pathOptions={{ color: "#102e50", fillColor: "#f4ca64", fillOpacity: 0.95, weight: 3 }} />;
}

function FormLugar({ lugar, categorias, onCerrar, onGuardado }) {
  const [valores, setValores] = useState(lugar);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");
  const nuevo = !lugar.id;
  const set = (campo, valor) => setValores(v => ({ ...v, [campo]: valor }));
  const posicion = [Number(valores.latitud) || CENTRO[0], Number(valores.longitud) || CENTRO[1]];

  async function enviar(e) {
    e.preventDefault();
    setGuardando(true); setError("");
    try {
      if (nuevo) await adminApi.crearLugar(cuerpo(valores));
      else await adminApi.editarLugar(lugar.id, cuerpo(valores));
      onGuardado(nuevo ? `"${valores.nombre}" creado.` : "Cambios guardados.");
    } catch (err) { setError(negocioError(err)); }
    finally { setGuardando(false); }
  }

  return <Modal titulo={nuevo ? "Nuevo lugar" : `Editar ${lugar.nombre}`} onCerrar={onCerrar} ancho={760}>
    <form className="adm-form" onSubmit={enviar}>
      {error && <p className="adm-error" role="alert">{error}</p>}
      <div className="adm-form-grid">
        <label className="ancho">Nombre<input required maxLength={150} value={valores.nombre} onChange={e => set("nombre", e.target.value)} /></label>
        <label className="ancho">Descripción<textarea maxLength={2000} rows={3} value={valores.descripcion} onChange={e => set("descripcion", e.target.value)} /></label>
        <label>Categoría<select required value={valores.categoriaId} onChange={e => set("categoriaId", e.target.value)}>
          <option value="" disabled>Selecciona…</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
        </select></label>
        <label>Piso<input type="number" min={-5} max={20} value={valores.piso} onChange={e => set("piso", e.target.value)} placeholder="Opcional" /></label>
        <label>Latitud<input required type="number" step="any" min={-90} max={90} value={valores.latitud} onChange={e => set("latitud", e.target.value)} /></label>
        <label>Longitud<input required type="number" step="any" min={-180} max={180} value={valores.longitud} onChange={e => set("longitud", e.target.value)} /></label>
      </div>
      <div className="adm-map-picker">
        <MapContainer center={posicion} zoom={17} scrollWheelZoom {...LIMITES_MAPA}>
          <TileLayer {...TESELAS} />
          <LimitarAlCampus />
          <SelectorMapa posicion={posicion} onCambio={(lat, lng) => setValores(v => ({ ...v, latitud: lat.toFixed(6), longitud: lng.toFixed(6) }))} />
        </MapContainer>
        <small>Haz clic en el mapa para colocar el lugar.</small>
      </div>
      <label className="adm-check"><input type="checkbox" checked={valores.activo} onChange={e => set("activo", e.target.checked)} /> Publicado en el mapa</label>
      <div className="adm-modal-actions">
        <button type="button" className="adm-btn" onClick={onCerrar}>Cancelar</button>
        <button type="submit" className="adm-btn primario" disabled={guardando}>{guardando ? "Guardando…" : nuevo ? "Crear lugar" : "Guardar cambios"}</button>
      </div>
    </form>
  </Modal>;
}
