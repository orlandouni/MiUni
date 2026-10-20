import { useEffect, useRef, useState } from "react";
import { calcularRuta } from "./rutas";
export function useRuta() {
  const [origen, setOrigen] = useState(null);
  const [destino, setDestino] = useState(null);
  const [seleccion, setSeleccion] = useState(null);
  const [ruta, setRuta] = useState(null);
  const [error, setError] = useState("");
  const [cargando, setCargando] = useState(false);
  const solicitud = useRef(null);
  useEffect(() => () => solicitud.current?.abort(), []);

  function cancelar() {
    solicitud.current?.abort();
    solicitud.current = null;
    setCargando(false);
    setRuta(null);
    setError("");
  }

  function elegir(tipo, punto) {
    cancelar();
    if (tipo === "origen") setOrigen(punto);
    else setDestino(punto);
    setSeleccion(null);
  }

  async function calcular() {
    if (!origen || !destino) return;
    cancelar();
    setSeleccion(null);
    const controller = new AbortController();
    solicitud.current = controller;
    setCargando(true);
    try {
      const resultado = await calcularRuta(origen, destino, controller.signal);
      if (solicitud.current === controller) setRuta(resultado);
    } catch (err) {
      if (solicitud.current === controller && !controller.signal.aborted) setError(err.message);
    } finally {
      if (solicitud.current === controller) {
        solicitud.current = null;
        setCargando(false);
      }
    }
  }

  function limpiar() {
    cancelar();
    setOrigen(null);
    setDestino(null);
    setSeleccion(null);
  }

  return { origen, destino, seleccion, setSeleccion, ruta, error, cargando, elegir, calcular, limpiar };
}
