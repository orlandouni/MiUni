import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { AlertTriangle, Building2, Check, Clock3, RefreshCw, ShieldCheck, X } from "lucide-react";
import api from "../services/api";
import { negocioError } from "../services/negocioErrors";
import "./Admin.css";

const estados = ["Pendiente", "Revisado", "Descartado"];
const etiquetas = { Pendiente: "Pendiente", Revisado: "Revisado", Descartado: "Descartado" };

export default function Admin() {
  const [seccion, setSeccion] = useState("reportes");
  const [reportes, setReportes] = useState([]);
  const [resumen, setResumen] = useState([]);
  const [solicitudes, setSolicitudes] = useState([]);
  const [filtro, setFiltro] = useState("Pendiente");
  const [cargando, setCargando] = useState(true);
  const [ocupado, setOcupado] = useState("");
  const [error, setError] = useState("");
  const [aviso, setAviso] = useState("");

  const cargar = useCallback(async () => {
    setCargando(true); setError("");
    try {
      const [lista, conteo, negocios] = await Promise.all([
        api.get("/api/ReporteUsuario", { params: { estado: filtro, pagina: 1, tamanoPagina: 100 } }),
        api.get("/api/ReporteUsuario/resumen"),
        api.get("/api/Negocio/solicitudes"),
      ]);
      setReportes(lista.data.items || []); setResumen(conteo.data); setSolicitudes(negocios.data);
    } catch (e) { setError(negocioError(e)); }
    finally { setCargando(false); }
  }, [filtro]);

  useEffect(() => { cargar(); }, [cargar]);

  async function cambiarEstado(id, estado) {
    setOcupado(id); setError(""); setAviso("");
    try {
      await api.patch(`/api/ReporteUsuario/${id}/estado`, { estado });
      setReportes(items => items.filter(item => item.id !== id));
      setResumen(items => items.map(item => {
        if (item.estado === estado) return { ...item, cantidad: item.cantidad + 1 };
        const previo = reportes.find(reporte => reporte.id === id)?.estado;
        return item.estado === previo ? { ...item, cantidad: Math.max(0, item.cantidad - 1) } : item;
      }));
      setAviso(`Reporte actualizado: ${etiquetas[estado].toLowerCase()}.`);
    } catch (e) { setError(negocioError(e)); }
    finally { setOcupado(""); }
  }

  async function revisarNegocio(id, accion) {
    setOcupado(id); setError(""); setAviso("");
    try {
      await api.post(`/api/Negocio/solicitudes/${id}/${accion}`);
      setSolicitudes(items => items.filter(item => item.id !== id));
      setAviso(accion === "aprobar" ? "Negocio aprobado y publicado." : "Solicitud de negocio rechazada.");
    } catch (e) { setError(negocioError(e)); }
    finally { setOcupado(""); }
  }

  const cantidad = estado => resumen.find(item => item.estado === estado)?.cantidad ?? 0;

  return <main className="admin-page">
    <div className="admin-shell">
      <header className="admin-top"><Link to="/" className="admin-back">← Volver al mapa</Link><span className="admin-brand"><ShieldCheck size={18} /> MIUNI · ADMIN</span></header>
      <section className="admin-heading"><div><p className="admin-eyebrow">CENTRO DE CONTROL</p><h1>Administración</h1><p>Revisa los reportes de la comunidad y las solicitudes de negocios.</p></div><button className="admin-refresh" onClick={cargar} disabled={cargando}><RefreshCw size={16} /> Actualizar</button></section>
      {error && <p className="admin-alert" role="alert">{error}</p>}{aviso && <p className="admin-success" role="status">{aviso}</p>}
      <nav className="admin-tabs" aria-label="Secciones de administración">
        <button className={seccion === "reportes" ? "activo" : ""} onClick={() => setSeccion("reportes")}><AlertTriangle size={17} /> Reportes <span>{cantidad("Pendiente")}</span></button>
        <button className={seccion === "negocios" ? "activo" : ""} onClick={() => setSeccion("negocios")}><Building2 size={17} /> Negocios <span>{solicitudes.length}</span></button>
      </nav>
      {seccion === "reportes" ? <>
        <div className="admin-stats">{estados.map((estado, i) => <article key={estado}><span className={`admin-stat-icon stat-${i}`}><Clock3 size={18} /></span><div><small>{etiquetas[estado]}</small><strong>{cantidad(estado)}</strong></div></article>)}</div>
        <section className="admin-content"><div className="admin-content-head"><div><h2>Reportes de la comunidad</h2><p>Evalúa reportes de lugares y reseñas.</p></div><label className="admin-filter">Estado <select value={filtro} onChange={e => setFiltro(e.target.value)}>{estados.map(estado => <option key={estado}>{estado}</option>)}</select></label></div>
          {cargando ? <p className="admin-empty">Cargando reportes…</p> : reportes.length === 0 ? <div className="admin-empty"><Check size={24} /><strong>Todo al día</strong><span>No hay reportes con este estado.</span></div> : <div className="admin-list">{reportes.map(reporte => <article className="admin-card" key={reporte.id}><div className="admin-card-top"><span className="admin-kind">{reporte.resenaId ? "RESEÑA" : "LUGAR"}</span><time>{reporte.fechaCreacion ? new Intl.DateTimeFormat("es-MX", { dateStyle: "medium" }).format(new Date(reporte.fechaCreacion)) : "Fecha no disponible"}</time></div><h3>{reporte.nombreLugar || "Lugar reportado"}</h3>{reporte.comentarioResena && <blockquote>“{reporte.comentarioResena}”</blockquote>}<p className="admin-reason">{reporte.motivo}</p><p className="admin-reporter">Reportado por <strong>{reporte.nombreUsuario}</strong>{reporte.emailUsuario ? ` · ${reporte.emailUsuario}` : ""}</p><div className="admin-actions"><button className="admin-dismiss" disabled={ocupado === reporte.id} onClick={() => cambiarEstado(reporte.id, "Descartado")}><X size={15} /> Descartar</button><button className="admin-resolve" disabled={ocupado === reporte.id} onClick={() => cambiarEstado(reporte.id, "Revisado")}><Check size={15} /> Marcar revisado</button></div></article>)}</div>}
        </section>
      </> : <section className="admin-content"><div className="admin-content-head"><div><h2>Solicitudes de negocios</h2><p>Aprueba los negocios para publicarlos en el mapa.</p></div><span className="admin-pending">{solicitudes.length} pendientes</span></div>
        {cargando ? <p className="admin-empty">Cargando solicitudes…</p> : solicitudes.length === 0 ? <div className="admin-empty"><Check size={24} /><strong>Sin solicitudes pendientes</strong><span>Las nuevas solicitudes aparecerán aquí.</span></div> : <div className="admin-list">{solicitudes.map(item => <article className="admin-card" key={item.id}><div className="admin-card-top"><span className="admin-kind">NUEVO NEGOCIO</span><span className="admin-pending">Pendiente</span></div><h3>{item.nombre}</h3><p className="admin-reason">{item.descripcion || "Sin descripción"}</p><p className="admin-reporter">Ubicación: {item.latitud}, {item.longitud}</p><div className="admin-actions"><button className="admin-dismiss" disabled={ocupado === item.id} onClick={() => revisarNegocio(item.id, "rechazar")}><X size={15} /> Rechazar</button><button className="admin-resolve" disabled={ocupado === item.id} onClick={() => revisarNegocio(item.id, "aprobar")}><Check size={15} /> Aprobar y publicar</button></div></article>)}</div>}
      </section>}
    </div>
  </main>;
}
