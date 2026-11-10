import { useCallback, useEffect, useRef, useState } from "react";
import { calcularRuta } from "./rutas";
import { iniciarUbicacionAutomatica, recordarSolicitud, solicitarUbicacion } from "./ubicacion";
export function useRuta() {
  const [origen, setOrigen] = useState(null);
  const [destino, setDestino] = useState(null);
  const [seleccion, cambiarSeleccion] = useState(null);
  const [localizando, setLocalizando] = useState(false);
  const [siguiendo, setSiguiendo] = useState(false);
  const [errorUbicacion, setErrorUbicacion] = useState("");
  const cancelarGps = useRef(null);
  const [ruta, setRuta] = useState(null);
  const [error, setError] = useState("");
  const [cargando, setCargando] = useState(false);
  const solicitud = useRef(null);
  const iniciarGps = useCallback(() => {
    setLocalizando(true);
    setSiguiendo(true);
    return solicitarUbicacion(punto => {
      setOrigen(punto);
      setLocalizando(false);
    }, mensaje => {
      setErrorUbicacion(mensaje);
      setLocalizando(false);
      setSiguiendo(false);
    });
  }, []);

  useEffect(() => {
    cancelarGps.current = iniciarUbicacionAutomatica(iniciarGps);
    return () => { solicitud.current?.abort(); cancelarGps.current?.(); };
  }, [iniciarGps]);

  function cancelarUbicacion() {
    cancelarGps.current?.();
    cancelarGps.current = null;
    setLocalizando(false);
    setSiguiendo(false);
    setErrorUbicacion("");
  }

  function setSeleccion(valor) {
    if (valor === "origen") cancelarUbicacion();
    cambiarSeleccion(valor);
  }

  function usarUbicacion() {
    cancelarUbicacion();
    cancelar();
    cambiarSeleccion(null);
    recordarSolicitud();
    cancelarGps.current = iniciarGps();
  }

  function cancelar() {
    solicitud.current?.abort();
    solicitud.current = null;
    setCargando(false);
    setRuta(null);
    setError("");
  }

  function elegir(tipo, punto) {
    cancelar();
    if (tipo === "origen") { cancelarUbicacion(); setOrigen(punto); }
    else setDestino(punto);
    setSeleccion(null);
  }

  async function calcular() {
    if (!origen || !destino || localizando) return;
    cancelar();
    setSeleccion(null);
    const controller = new AbortController();
    solicitud.current = controller;
    setCargando(true);
    try {
      const resultado = await calcularRuta(origen, destino, controller.signal);
      if (solicitud.current === controller) setRuta({ ...resultado, origenCalculo: origen });
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
    cancelarUbicacion();
    setOrigen(null);
    setDestino(null);
    setSeleccion(null);
  }

  return { origen, destino, seleccion, setSeleccion, ruta, error, cargando, elegir, calcular, limpiar,
    localizando, siguiendo, errorUbicacion, usarUbicacion, cancelarUbicacion };
}
