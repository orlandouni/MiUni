import { useCallback, useEffect, useRef, useState } from "react";
import { calcularRuta, distanciaMetros } from "./rutas";
import { iniciarUbicacionAutomatica, recordarSolicitud, solicitarUbicacion } from "./ubicacion";

// Con el GPS activo la ruta se recalcula sola cuando te mueves al menos esto.
export const MOVIMIENTO_RECALCULO_METROS = 15;

// Como en una app de mapas: el origen es tu ubicación, eliges el destino y la ruta se calcula sola.
export function useRuta() {
  const [origen, setOrigen] = useState(null);
  const [destino, setDestino] = useState(null);
  const [eligiendoOrigen, setEligiendoOrigen] = useState(false);
  const [localizando, setLocalizando] = useState(false);
  const [siguiendo, setSiguiendo] = useState(false);
  const [errorUbicacion, setErrorUbicacion] = useState("");
  const cancelarGps = useRef(null);
  const [ruta, setRuta] = useState(null);
  const [error, setError] = useState("");
  const [cargando, setCargando] = useState(false);
  const solicitud = useRef(null);
  const ultimoCalculo = useRef(null);

  const iniciarGps = useCallback(() => {
    setLocalizando(true);
    setSiguiendo(true);
    setErrorUbicacion("");
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

  const cancelarCalculo = useCallback(() => {
    solicitud.current?.abort();
    solicitud.current = null;
    setCargando(false);
  }, []);

  // Calcula al tener origen y destino; con GPS, sólo vuelve a pedir la ruta si te moviste.
  useEffect(() => {
    if (!origen || !destino) return;
    const previo = ultimoCalculo.current;
    if (previo && previo.destino === destino
      && (previo.origen === origen || distanciaMetros(previo.origen, origen) < MOVIMIENTO_RECALCULO_METROS)) return;
    ultimoCalculo.current = { origen, destino };
    cancelarCalculo();
    const controller = new AbortController();
    solicitud.current = controller;
    setCargando(true);
    setError("");
    calcularRuta(origen, destino, controller.signal)
      .then(resultado => {
        if (solicitud.current === controller) setRuta({ ...resultado, origenCalculo: origen, destinoCalculo: destino });
      })
      .catch(err => {
        if (solicitud.current === controller && !controller.signal.aborted) { setRuta(null); setError(err.message); }
      })
      .finally(() => {
        if (solicitud.current === controller) { solicitud.current = null; setCargando(false); }
      });
  }, [origen, destino, cancelarCalculo]);

  function detenerGps() {
    cancelarGps.current?.();
    cancelarGps.current = null;
    setLocalizando(false);
    setSiguiendo(false);
  }

  function usarUbicacion() {
    detenerGps();
    setEligiendoOrigen(false);
    recordarSolicitud();
    cancelarGps.current = iniciarGps();
  }

  function reiniciarRuta() {
    cancelarCalculo();
    ultimoCalculo.current = null;
    setRuta(null);
    setError("");
  }

  // Mantiene la firma usada por los popups: elegir("origen" | "destino", punto).
  function elegir(tipo, punto) {
    reiniciarRuta();
    if (tipo === "origen") {
      detenerGps();
      setErrorUbicacion("");
      setOrigen({ ...punto, fuente: "manual" });
    } else setDestino(punto);
    setEligiendoOrigen(false);
  }

  // Un toque en el mapa marca el destino, salvo que se esté eligiendo el punto de partida.
  function tocarMapa(punto) {
    elegir(eligiendoOrigen ? "origen" : "destino", punto);
  }

  function quitarDestino() {
    reiniciarRuta();
    setDestino(null);
    setEligiendoOrigen(false);
  }

  return { origen, destino, ruta, error, cargando, localizando, siguiendo, errorUbicacion,
    eligiendoOrigen, setEligiendoOrigen, elegir, tocarMapa, quitarDestino, usarUbicacion };
}
