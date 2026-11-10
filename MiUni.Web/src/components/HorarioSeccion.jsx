import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { CalendarDays, ChevronRight, ExternalLink, Upload } from "lucide-react";
import { obtenerHorario } from "../services/horario";
import { agruparPorMateria, resumenSesiones } from "../services/horarioUtils";
import { mensajeError } from "../services/comunidad";

// Resumen del horario en el perfil; la subida del PDF y la edición viven en /horario.
export default function HorarioSeccion() {
  const [clases, setClases] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let vivo = true;
    obtenerHorario()
      .then(datos => { if (vivo) setClases(datos ?? []); })
      .catch(err => { if (vivo) { setError(mensajeError(err)); setClases([]); } });
    return () => { vivo = false; };
  }, []);

  const materias = clases ? agruparPorMateria(clases) : [];

  return <section id="mi-horario" className="perfil-seccion perfil-horario">
    <header><h2><CalendarDays size={18}/> Mi horario</h2><span>{materias.length}</span></header>
    {error && <div className="perfil-alerta" role="alert">{error}</div>}
    {clases === null ? <p className="perfil-estado" role="status">Cargando horario…</p>
      : materias.length ? <div className="perfil-lista">
        {materias.map(m => <div className="perfil-item" key={m.id}>
          <span className="perfil-item-icon"><CalendarDays size={17}/></span>
          <span className="perfil-item-text">
            <strong>{m.materia}</strong>
            <small>{resumenSesiones(m.sesiones)}{m.salones.length ? ` · ${m.salones.join(", ")}` : ""}{m.profesor ? ` · ${m.profesor}` : ""}</small>
          </span>
          {m.buhoRaterUrl && <a className="perfil-horario-buho" href={m.buhoRaterUrl} target="_blank" rel="noreferrer" title={m.buhoRaterPerfil ? `Ver el perfil de ${m.profesor} en BuhoRater` : `Buscar a ${m.profesor} en BuhoRater`}>BuhoRater <ExternalLink size={13}/></a>}
        </div>)}
      </div>
      : <p className="perfil-vacio">Aún no tienes horario. Sube el PDF del portal de la UNISON y lo llenamos por ti.</p>}
    <Link className="perfil-horario-ir" to="/horario">
      {materias.length ? <>Ver o editar mi horario <ChevronRight size={15}/></> : <><Upload size={15}/> Subir mi horario (PDF)</>}
    </Link>
  </section>;
}
