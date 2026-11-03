import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ArrowRight, CircleAlert, LoaderCircle, MailCheck } from "lucide-react";
import { verificarCorreoRequest } from "../services/api";
import ReenviarVerificacion from "../components/ReenviarVerificacion";
import CampusPanel from "./CampusPanel";

// Un enlace se verifica una sola vez aunque el efecto se ejecute dos veces
// (StrictMode en desarrollo) o el componente se vuelva a montar.
const verificaciones = new Map();
function verificarUnaVez(usuario, token) {
  const clave = `${usuario}:${token}`;
  if (!verificaciones.has(clave)) verificaciones.set(clave, verificarCorreoRequest(usuario, token));
  return verificaciones.get(clave);
}

export default function VerificarCorreo() {
  const [params] = useSearchParams();
  const usuario = params.get("usuario");
  const token = params.get("token");
  const enlaceCompleto = Boolean(usuario && token);
  const [estado, setEstado] = useState({ cargando: enlaceCompleto, ok: false, mensaje: "" });
  const [email, setEmail] = useState("");

  useEffect(() => {
    if (!enlaceCompleto) return undefined;
    let activo = true;
    verificarUnaVez(usuario, token)
      .then(data => { if (activo) setEstado({ cargando: false, ok: true, mensaje: data.message }); })
      .catch(err => {
        if (activo) setEstado({ cargando: false, ok: false, mensaje: err.response?.data?.message || "No pudimos verificar tu correo. Intenta de nuevo." });
      });
    return () => { activo = false; };
  }, [enlaceCompleto, usuario, token]);

  const mensaje = enlaceCompleto ? estado.mensaje : "El enlace está incompleto. Ábrelo directamente desde el correo que te enviamos.";
  const Icono = estado.cargando ? LoaderCircle : estado.ok ? MailCheck : CircleAlert;

  return <CampusPanel animateIllustration>
    <section className="ma-card" aria-labelledby="verificar-title" aria-busy={estado.cargando}>
      <div className="ma-heading">
        <div className={`ma-emblem${estado.cargando ? " ma-girando" : ""}`}><Icono aria-hidden="true" /></div>
        <h1 id="verificar-title">{estado.cargando ? "Verificando tu correo…" : estado.ok ? "Correo verificado" : "No se pudo verificar"}</h1>
        {!estado.cargando && <p role={estado.ok ? "status" : "alert"}>{mensaje}</p>}
      </div>

      {estado.ok && <Link className="ma-submit ma-verificar-accion" to="/login">
        <span>Iniciar sesión</span><ArrowRight size={20} aria-hidden="true" />
      </Link>}

      {!estado.cargando && !estado.ok && <form className="ma-form" onSubmit={e => e.preventDefault()}>
        <div className="ma-field">
          <label htmlFor="correo-reenvio">Escribe tu correo para recibir un enlace nuevo</label>
          <div className="ma-input-wrap ma-sin-icono">
            <input id="correo-reenvio" type="email" autoComplete="email" placeholder="tu@correo.com" value={email} onChange={e => setEmail(e.target.value)} />
          </div>
        </div>
        <ReenviarVerificacion email={email.trim()} />
        <p className="ma-alternative"><Link to="/login">Volver a iniciar sesión</Link></p>
      </form>}
    </section>
  </CampusPanel>;
}
