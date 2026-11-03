import { useEffect, useState } from "react";
import { RefreshCw } from "lucide-react";
import { reenviarVerificacionRequest } from "../services/api";

const ESPERA = 60;

// Botón para pedir otro correo de verificación, con espera de 60 s entre envíos (igual que la API).
export default function ReenviarVerificacion({ email }) {
  const [restante, setRestante] = useState(0);
  const [mensaje, setMensaje] = useState("");
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    if (restante <= 0) return undefined;
    const id = setTimeout(() => setRestante(r => r - 1), 1000);
    return () => clearTimeout(id);
  }, [restante]);

  async function reenviar() {
    setEnviando(true); setMensaje("");
    try {
      const data = await reenviarVerificacionRequest(email);
      setMensaje(data.message || "Te enviamos un nuevo correo.");
      setRestante(ESPERA);
    } catch {
      setMensaje("No pudimos enviar el correo. Intenta de nuevo en un momento.");
    } finally { setEnviando(false); }
  }

  return <div className="ma-reenviar">
    <button type="button" onClick={reenviar} disabled={!email || enviando || restante > 0}>
      <RefreshCw size={15} aria-hidden="true" />
      {enviando ? "Enviando…" : restante > 0 ? `Reenviar en ${restante} s` : "Reenviar correo de verificación"}
    </button>
    {mensaje && <p role="status">{mensaje}</p>}
  </div>;
}
