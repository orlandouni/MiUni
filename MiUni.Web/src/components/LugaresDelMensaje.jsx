import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { cargarLugaresChat, detectarLugares, enlaceLugar, lugaresEnlazados } from "../services/lugaresChat";
import { MapPin, ArrowUpRight } from "lucide-react";
import "./LugaresDelMensaje.css";

// Render below assistant messages only. A shared catalog can be passed by the chat.
export default function LugaresDelMensaje({ texto, lugares }) {
  const [catalogo, setCatalogo] = useState([]);
  const [error, setError] = useState(false);
  const [intento, setIntento] = useState(0);
  const consultar = lugares === undefined && Boolean(texto?.trim());

  useEffect(() => {
    if (!consultar) return;
    let vigente = true;
    cargarLugaresChat().then(data => {
      if (vigente) { setCatalogo(data); setError(false); }
    }).catch(() => { if (vigente) setError(true); });
    return () => { vigente = false; };
  }, [consultar, intento]);

  if (consultar && error) return (
    <p role="status">No se pudieron consultar las ubicaciones. {" "}
      <button type="button" onClick={() => { setError(false); setIntento(value => value + 1); }}>Reintentar</button>
    </p>
  );

  const enlazados = lugaresEnlazados(texto);
  const encontrados = detectarLugares(texto, lugares ?? catalogo)
    .filter(lugar => !enlazados.has(String(lugar.id).toLowerCase()));
  if (!encontrados.length) return null;
  return (
    <nav className="chat-places" aria-label="Lugares mencionados en esta respuesta">
      <ul>{encontrados.map(lugar => (
        <li key={lugar.id}>
          <Link className="chat-place-card" to={enlaceLugar(lugar.id)}
            aria-label={`Ver ${lugar.nombre} en el mapa del campus`}>
            <span className="chat-place-icon"><MapPin size={19} aria-hidden="true" /></span>
            <span className="chat-place-text">
              <strong>{lugar.nombre}</strong>
              <span>Ver en el mapa del campus</span>
            </span>
            <ArrowUpRight className="chat-place-arrow" size={17} aria-hidden="true" />
          </Link>
        </li>
      ))}</ul>
    </nav>
  );
}
