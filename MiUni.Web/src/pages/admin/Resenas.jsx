import { useState } from "react";
import { Link } from "react-router-dom";
import { Search, Trash2 } from "lucide-react";
import { adminApi, limpiarParams } from "../../services/admin";
import { Aviso, Cargando, Confirmacion, Encabezado, Estrellas, Insignia, Paginacion, Vacio, fecha, useAccion, useCarga } from "./ui";

const TAMANO = 20;

export default function Resenas({ onCambio }) {
  const [filtro, setFiltro] = useState({ q: "", calificacion: "", conReportes: false });
  const [busqueda, setBusqueda] = useState("");
  const [pagina, setPagina] = useState(1);
  const params = limpiarParams({ ...filtro, conReportes: filtro.conReportes || undefined, pagina, tamanoPagina: TAMANO });
  const { datos, cargando, error, recargar } = useCarga(() => adminApi.resenas(params), JSON.stringify(params));
  const { ocupado, aviso, setAviso, ejecutar } = useAccion();
  const [confirmar, setConfirmar] = useState(null);

  const cambiar = cambio => { setFiltro(f => ({ ...f, ...cambio })); setPagina(1); };
  const eliminar = r => setConfirmar({
    titulo: "Eliminar reseña", boton: "Eliminar reseña",
    texto: `La reseña de ${r.nombreUsuario} sobre "${r.nombreLugar}" se eliminará permanentemente${r.reportesTotales ? ` junto con sus ${r.reportesTotales} reporte(s)` : ""}.`,
    accion: async () => { if (await ejecutar(r.id, () => adminApi.eliminarResena(r.id), "Reseña eliminada.")) { recargar(); onCambio(); } },
  });

  return <section className="adm-card">
    <Encabezado titulo="Reseñas" descripcion="Modera las opiniones publicadas. Las reseñas con reportes pendientes aparecen primero." />
    <Aviso aviso={aviso || (error && { tipo: "error", texto: error })} onCerrar={() => setAviso(null)} />
    <form className="adm-filters" onSubmit={e => { e.preventDefault(); cambiar({ q: busqueda }); }}>
      <label className="adm-search"><Search size={15} /><input type="search" placeholder="Comentario, lugar o autor" value={busqueda}
        onChange={e => { setBusqueda(e.target.value); if (!e.target.value) cambiar({ q: "" }); }} /></label>
      <select aria-label="Calificación" value={filtro.calificacion} onChange={e => cambiar({ calificacion: e.target.value })}>
        <option value="">Todas las calificaciones</option>
        {[5, 4, 3, 2, 1].map(n => <option key={n} value={n}>{n} estrella{n > 1 ? "s" : ""}</option>)}
      </select>
      <label className="adm-check"><input type="checkbox" checked={filtro.conReportes} onChange={e => cambiar({ conReportes: e.target.checked })} /> Solo reportadas</label>
    </form>

    {cargando && !datos ? <Cargando texto="Cargando reseñas…" /> : datos?.items.length === 0 ?
      <Vacio titulo="Sin reseñas" texto="No hay reseñas que coincidan con los filtros." /> :
      <div className={`adm-list${cargando ? " recargando" : ""}`}>
        {datos?.items.map(r => <article className="adm-item" key={r.id}>
          <div className="adm-item-top">
            <Estrellas valor={r.calificacion} />
            {r.reportesPendientes > 0 && <Insignia tono="alerta">{r.reportesPendientes} reporte(s) pendiente(s)</Insignia>}
            <time>{fecha(r.fechaCreacion)}</time>
          </div>
          <h3><Link to={`/lugares/${r.lugarId}`}>{r.nombreLugar}</Link></h3>
          <p className="adm-reason">{r.comentario || <em>Sin comentario</em>}</p>
          <p className="adm-meta">Por <strong>{r.nombreUsuario}</strong>{r.emailUsuario ? ` · ${r.emailUsuario}` : ""}</p>
          <div className="adm-item-actions">
            <button type="button" className="adm-btn peligro-suave" disabled={ocupado === r.id} onClick={() => eliminar(r)}><Trash2 size={15} /> Eliminar</button>
          </div>
        </article>)}
      </div>}
    {datos && <Paginacion pagina={pagina} total={datos.total} tamano={TAMANO} onCambio={setPagina} />}
    <Confirmacion confirmar={confirmar} onCerrar={() => setConfirmar(null)} ocupado={!!ocupado} />
  </section>;
}
