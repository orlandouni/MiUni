import { useEffect, useState } from "react";
import { cargarLugaresChat, ubicacionValida } from "./lugaresChat";

export function useLugarDesdeChat(id) {
  const [resultado, setResultado] = useState({ id: null });
  useEffect(() => {
    if (!id) return;
    let vigente = true;
    cargarLugaresChat().then(lugares => {
      const lugar = lugares.find(item => String(item.id).toLowerCase() === id.toLowerCase());
      if (vigente) setResultado({ id, lugar: ubicacionValida(lugar) ? lugar : null,
        error: !lugar ? "El lugar del enlace ya no está disponible." : !ubicacionValida(lugar) ? "Este lugar no tiene una ubicación válida." : null });
    }).catch(() => {
      if (vigente) setResultado({ id, error: "No se pudo cargar el lugar del enlace. Intenta abrirlo de nuevo." });
    });
    return () => { vigente = false; };
  }, [id]);
  return id && resultado.id === id ? resultado : { lugar: null, error: null };
}
