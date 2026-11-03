import { AlertTriangle, Ban, Building2, MapPin, MessageSquare, Star, UserPlus, Users } from "lucide-react";
import { Cargando, Encabezado, Vacio } from "./ui";

const diaCorto = new Intl.DateTimeFormat("es-MX", { day: "numeric", month: "short", timeZone: "UTC" });

export default function Resumen({ panel, cargando, error, irA }) {
  if (cargando && !panel) return <Cargando texto="Calculando métricas…" />;
  if (!panel) return <p className="adm-error">{error || "No hay datos disponibles."}</p>;

  const tarjetas = [
    { icono: Users, etiqueta: "Usuarios", valor: panel.usuarios, detalle: `+${panel.usuariosNuevos30Dias} en 30 días`, ir: "usuarios" },
    { icono: MapPin, etiqueta: "Lugares activos", valor: panel.lugaresActivos, detalle: `${panel.lugaresInactivos} inactivos`, ir: "lugares" },
    { icono: Star, etiqueta: "Reseñas", valor: panel.resenas, detalle: panel.promedioCalificacion ? `Promedio ${panel.promedioCalificacion.toFixed(1)} ★` : "Sin calificaciones", ir: "resenas" },
    { icono: MessageSquare, etiqueta: "Sesiones de chat", valor: panel.sesionesChat7Dias, detalle: `${panel.mensajesChat7Dias} preguntas en 7 días` },
    { icono: AlertTriangle, etiqueta: "Reportes pendientes", valor: panel.reportesPendientes, detalle: "Requieren revisión", ir: "reportes", alerta: panel.reportesPendientes > 0 },
    { icono: Building2, etiqueta: "Solicitudes de negocio", valor: panel.solicitudesPendientes, detalle: "Por aprobar", ir: "negocios", alerta: panel.solicitudesPendientes > 0 },
    { icono: UserPlus, etiqueta: "Nuevos (30 días)", valor: panel.usuariosNuevos30Dias, detalle: "Registros recientes", ir: "usuarios" },
    { icono: Ban, etiqueta: "Cuentas suspendidas", valor: panel.usuariosBloqueados, detalle: "Sin acceso", ir: "usuarios" },
  ];
  const maxRegistro = Math.max(1, ...panel.registrosPorDia.map(d => d.cantidad));
  const maxCategoria = Math.max(1, ...panel.lugaresPorCategoria.map(d => d.cantidad));

  return <>
    <div className="adm-kpis">
      {tarjetas.map(({ icono: Icono, ...t }) => {
        const contenido = <><span className="adm-kpi-icon"><Icono size={18} /></span><small>{t.etiqueta}</small><strong>{t.valor}</strong><em>{t.detalle}</em></>;
        return t.ir
          ? <button type="button" key={t.etiqueta} className={`adm-kpi${t.alerta ? " alerta" : ""}`} onClick={() => irA(t.ir)}>{contenido}</button>
          : <article key={t.etiqueta} className="adm-kpi">{contenido}</article>;
      })}
    </div>

    <div className="adm-grid-2">
      <section className="adm-card">
        <Encabezado titulo="Registros" descripcion="Nuevas cuentas en los últimos 14 días" />
        <div className="adm-bars" role="img" aria-label="Registros por día">
          {panel.registrosPorDia.map(d => <div key={d.etiqueta} className="adm-bar" title={`${d.cantidad} registros`}>
            <span className="adm-bar-value">{d.cantidad || ""}</span>
            <span className="adm-bar-fill" style={{ height: `${(d.cantidad / maxRegistro) * 100}%` }} />
            <span className="adm-bar-label">{diaCorto.format(new Date(d.etiqueta))}</span>
          </div>)}
        </div>
      </section>

      <section className="adm-card">
        <Encabezado titulo="Lugares por categoría" descripcion="Solo lugares publicados" />
        {panel.lugaresPorCategoria.length === 0 ? <Vacio titulo="Sin categorías" /> :
          <ul className="adm-hbars">
            {panel.lugaresPorCategoria.map(c => <li key={c.etiqueta}>
              <span>{c.etiqueta}</span>
              <span className="adm-hbar-track"><span style={{ width: `${(c.cantidad / maxCategoria) * 100}%` }} /></span>
              <strong>{c.cantidad}</strong>
            </li>)}
          </ul>}
      </section>
    </div>

    <section className="adm-card">
      <Encabezado titulo="Lugares más comentados" descripcion="Según reseñas y favoritos" />
      {panel.lugaresDestacados.length === 0 ? <Vacio titulo="Aún no hay actividad" /> :
        <div className="adm-table-wrap"><table className="adm-table">
          <thead><tr><th>Lugar</th><th>Reseñas</th><th>Promedio</th><th>Favoritos</th></tr></thead>
          <tbody>{panel.lugaresDestacados.map(l => <tr key={l.id}>
            <td><strong>{l.nombre}</strong></td><td>{l.resenas}</td>
            <td>{l.promedio ? `${l.promedio.toFixed(1)} ★` : "—"}</td><td>{l.favoritos}</td>
          </tr>)}</tbody>
        </table></div>}
    </section>
  </>;
}
