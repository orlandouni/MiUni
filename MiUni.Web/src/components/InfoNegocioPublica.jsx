import { useEffect, useState } from "react";
import { Bike, Clock, UtensilsCrossed } from "lucide-react";
import { DIAS, negociosApi, precio } from "../services/negocios";
import "./InfoNegocioPublica.css";

// Horario y menú que el propietario publica; no muestra nada si el lugar no tiene.
export default function InfoNegocioPublica({ lugarId }) {
  const [info, setInfo] = useState(null);

  useEffect(() => {
    let activo = true;
    negociosApi.info(lugarId).then(datos => { if (activo) setInfo(datos); }).catch(() => {});
    return () => { activo = false; };
  }, [lugarId]);

  if (!info || (!info.esAmbulante && info.horarios.length === 0 && info.productos.length === 0)) return null;

  const ahora = new Date();
  const hoy = ((ahora.getDay() + 6) % 7) + 1; // getDay: 0 = domingo → 1 = lunes … 7 = domingo
  const hora = ahora.toTimeString().slice(0, 5);
  const abiertoAhora = info.horarios.some(h => h.diaSemana === hoy && h.horaApertura <= hora && hora < h.horaCierre);

  return <section className="info-negocio">
    {info.esAmbulante && <p className="info-negocio-ambulante"><Bike size={16} /> Vendedor ambulante: la ubicación en el mapa es la zona donde suele estar.</p>}

    {info.horarios.length > 0 && <div>
      <h3><Clock size={17} /> Horario <span className={abiertoAhora ? "abierto" : "cerrado"}>{abiertoAhora ? "Abierto ahora" : "Cerrado ahora"}</span></h3>
      <ul className="info-negocio-horario">
        {DIAS.map((dia, i) => {
          const rangos = info.horarios.filter(h => h.diaSemana === i + 1);
          return <li key={dia} className={i + 1 === hoy ? "hoy" : ""}>
            <span>{dia}</span>
            <span>{rangos.length ? rangos.map(h => `${h.horaApertura}–${h.horaCierre}`).join(", ") : "Cerrado"}</span>
          </li>;
        })}
      </ul>
    </div>}

    {info.productos.length > 0 && <div>
      <h3><UtensilsCrossed size={17} /> Menú</h3>
      <ul className="info-negocio-menu">
        {info.productos.map(p => <li key={p.id}>
          <span><strong>{p.nombre}</strong>{p.descripcion && <small>{p.descripcion}</small>}</span>
          <span>{precio(p.precio)}</span>
        </li>)}
      </ul>
    </div>}
  </section>;
}
