import { useState } from "react";
import { Link } from "react-router-dom";
import { Check, EyeOff, RotateCcw, Trash2, X } from "lucide-react";
import { adminApi } from "../../services/admin";
import { Aviso, Cargando, Confirmacion, Encabezado, Insignia, Paginacion, Vacio, fecha, useAccion, useCarga } from "./ui";

const ESTADOS = ["Pendiente", "Revisado", "Descartado"];
const TONO = { Pendiente: "alerta", Revisado: "ok", Descartado: "neutro" };
const TAMANO = 20;

export default function Reportes({ onCambio }) {
  const [estado, setEstado] = useState("Pendiente");
  const [pagina, setPagina] = useState(1);
  const params = { estado: estado || undefined, pagina, tamanoPagina: TAMANO };
  const { datos, cargando, error, recargar } = useCarga(() => adminApi.reportes(params), JSON.stringify(params));
  const { ocupado, aviso, setAviso, ejecutar } = useAccion();
  const [confirmar, setConfirmar] = useState(null);

  const tras = ok => { if (ok) { recargar(); onCambio(); } };

  const cambiarEstado = async (r, nuevo) =>
    tras(await ejecutar(r.id, () => adminApi.estadoReporte(r.id, nuevo), `Reporte marcado como ${nuevo.toLowerCase()}.`));

  // Retirar el contenido denunciado y cerrar el reporte en un solo paso.
  const retirarContenido = r => setConfirmar(r.resenaId ? {
    titulo: "Eliminar reseña reportada",
    texto: "La reseña se eliminará de forma permanente junto con todos sus reportes.",
    boton: "Eliminar reseña",
    accion: async () => tras(await ejecutar(r.id, () => adminApi.eliminarResena(r.resenaId), "Reseña eliminada.")),
  } : {
    titulo: "Ocultar lugar reportado",
    texto: `"${r.nombreLugar}" dejará de aparecer en el mapa. Podrás reactivarlo desde Lugares.`,
    boton: "Ocultar lugar",
    accion: async () => tras(await ejecutar(r.id, async () => {
      await adminApi.desactivarLugar(r.lugarId);
      await adminApi.estadoReporte(r.id, "Revisado");
    }, "Lugar ocultado y reporte marcado como revisado.")),
  });

  const eliminar = r => setConfirmar({
    titulo: "Eliminar reporte", texto: "Se borrará el registro del reporte. El contenido reportado no cambia.", boton: "Eliminar",
    accion: async () => tras(await ejecutar(r.id, () => adminApi.eliminarReporte(r.id), "Reporte eliminado.")),
  });

  return <section className="adm-card">
    <Encabezado titulo="Reportes de la comunidad" descripcion="Evalúa reportes sobre lugares y reseñas.">
      <div className="adm-segmented" role="group" aria-label="Filtrar por estado">
        {[...ESTADOS, ""].map(e => <button type="button" key={e || "todos"} className={estado === e ? "activo" : ""}
          onClick={() => { setEstado(e); setPagina(1); }}>{e || "Todos"}</button>)}
      </div>
    </Encabezado>
    <Aviso aviso={aviso || (error && { tipo: "error", texto: error })} onCerrar={() => setAviso(null)} />

    {cargando && !datos ? <Cargando texto="Cargando reportes…" /> : datos?.items.length === 0 ?
      <Vacio titulo="Todo al día" texto="No hay reportes con este estado." /> :
      <div className={`adm-list${cargando ? " recargando" : ""}`}>
        {datos?.items.map(r => <article className="adm-item" key={r.id}>
          <div className="adm-item-top">
            <span className="adm-kind">{r.resenaId ? "Reseña" : "Lugar"}</span>
            <Insignia tono={TONO[r.estado]}>{r.estado}</Insignia>
            <time>{fecha(r.fechaCreacion)}</time>
          </div>
          <h3>{r.lugarReportadoId ? <Link to={`/lugares/${r.lugarReportadoId}`}>{r.nombreLugar}</Link> : "Lugar reportado"}</h3>
          {r.comentarioResena && <blockquote>“{r.comentarioResena}”</blockquote>}
          <p className="adm-reason">{r.motivo}</p>
          <p className="adm-meta">Reportado por <strong>{r.nombreUsuario}</strong>{r.emailUsuario ? ` · ${r.emailUsuario}` : ""}</p>
          <div className="adm-item-actions">
            <button type="button" className="adm-btn ghost" disabled={ocupado === r.id} onClick={() => eliminar(r)}><Trash2 size={15} /> Eliminar</button>
            {r.estado === "Pendiente" ? <>
              <button type="button" className="adm-btn" disabled={ocupado === r.id} onClick={() => cambiarEstado(r, "Descartado")}><X size={15} /> Descartar</button>
              <button type="button" className="adm-btn peligro-suave" disabled={ocupado === r.id} onClick={() => retirarContenido(r)}><EyeOff size={15} /> {r.resenaId ? "Eliminar reseña" : "Ocultar lugar"}</button>
              <button type="button" className="adm-btn primario" disabled={ocupado === r.id} onClick={() => cambiarEstado(r, "Revisado")}><Check size={15} /> Revisado</button>
            </> : <button type="button" className="adm-btn" disabled={ocupado === r.id} onClick={() => cambiarEstado(r, "Pendiente")}><RotateCcw size={15} /> Reabrir</button>}
          </div>
        </article>)}
      </div>}
    {datos && <Paginacion pagina={pagina} total={datos.total} tamano={TAMANO} onCambio={setPagina} />}
    <Confirmacion confirmar={confirmar} onCerrar={() => setConfirmar(null)} ocupado={!!ocupado} />
  </section>;
}
