import { useState } from "react";
import { Check, MapPin, X } from "lucide-react";
import { adminApi } from "../../services/admin";
import { Aviso, Cargando, Encabezado, Insignia, Vacio, fecha, useAccion, useCarga } from "./ui";

const ESTADOS = [["Pendiente", "Pendientes"], ["Aprobada", "Aprobados"], ["Rechazada", "Rechazados"], ["", "Todos"]];
const TONO = { Pendiente: "alerta", Aprobada: "ok", Rechazada: "peligro" };

export default function Negocios({ onCambio }) {
  const [estado, setEstado] = useState("Pendiente");
  const { datos, cargando, error, recargar } = useCarga(() => adminApi.negocios(estado), estado);
  const { ocupado, aviso, setAviso, ejecutar } = useAccion();

  async function revisar(n, accion) {
    const ok = await ejecutar(n.id, () => adminApi.revisarNegocio(n.id, accion),
      accion === "aprobar" ? `"${n.nombre}" aprobado y publicado en el mapa.` : `Solicitud de "${n.nombre}" rechazada.`);
    if (ok) { recargar(); onCambio(); }
  }

  return <section className="adm-card">
    <Encabezado titulo="Negocios" descripcion="Solicitudes de propietarios para publicar su negocio en el mapa.">
      <div className="adm-segmented" role="group" aria-label="Filtrar por estado">
        {ESTADOS.map(([valor, texto]) => <button type="button" key={texto} className={estado === valor ? "activo" : ""} onClick={() => setEstado(valor)}>{texto}</button>)}
      </div>
    </Encabezado>
    <Aviso aviso={aviso || (error && { tipo: "error", texto: error })} onCerrar={() => setAviso(null)} />

    {cargando && !datos ? <Cargando texto="Cargando negocios…" /> : datos?.length === 0 ?
      <Vacio titulo="Sin negocios aquí" texto="Las nuevas solicitudes aparecerán en Pendientes." /> :
      <div className={`adm-list${cargando ? " recargando" : ""}`}>
        {datos?.map(n => <article className="adm-item" key={n.id}>
          <div className="adm-item-top">
            <span className="adm-kind">{n.nombreCategoria}</span>
            <Insignia tono={TONO[n.estadoSolicitud]}>{n.estadoSolicitud}</Insignia>
            <time>{fecha(n.fechaCreacion)}</time>
          </div>
          <h3>{n.nombre}</h3>
          <p className="adm-reason">{n.descripcion || "Sin descripción"}</p>
          <p className="adm-meta">Propietario: <strong>{n.nombrePropietario || "Cuenta eliminada"}</strong>{n.emailPropietario ? ` · ${n.emailPropietario}` : ""}</p>
          <p className="adm-meta">
            <a href={`https://www.openstreetmap.org/?mlat=${n.latitud}&mlon=${n.longitud}#map=19/${n.latitud}/${n.longitud}`} target="_blank" rel="noreferrer">
              <MapPin size={12} /> {n.latitud.toFixed(5)}, {n.longitud.toFixed(5)}
            </a>
            {n.estadoSolicitud === "Aprobada" && <> · {n.activo ? "Visible en el mapa" : "Oculto"}</>}
          </p>
          {n.estadoSolicitud === "Pendiente" && <div className="adm-item-actions">
            <button type="button" className="adm-btn" disabled={ocupado === n.id} onClick={() => revisar(n, "rechazar")}><X size={15} /> Rechazar</button>
            <button type="button" className="adm-btn primario" disabled={ocupado === n.id} onClick={() => revisar(n, "aprobar")}><Check size={15} /> Aprobar y publicar</button>
          </div>}
        </article>)}
      </div>}
  </section>;
}
