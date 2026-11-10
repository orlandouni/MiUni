import { useEffect, useRef } from "react";
import { Circle, CircleMarker, Polyline, Tooltip, useMap, useMapEvents } from "react-leaflet";
import { Footprints, LocateFixed, MapPin, X } from "lucide-react";

import { minutosCaminando, posicionesRuta } from "../services/rutas";

const etiqueta = punto => punto?.nombre ?? (punto ? "Punto en el mapa" : "");

function Origen({ estado }) {
  const { origen, localizando, siguiendo, errorUbicacion, eligiendoOrigen, setEligiendoOrigen, usarUbicacion } = estado;
  if (eligiendoOrigen) return <div className="ruta-origen">
    <span>Toca el mapa en el punto desde donde sales.</span>
    <button type="button" onClick={() => setEligiendoOrigen(false)}>Cancelar</button>
  </div>;
  if (origen?.fuente === "gps") return <div className="ruta-origen">
    <span><LocateFixed size={14} aria-hidden="true" /> Desde tu ubicación{origen.precision > 0 && <small> · ±{Math.ceil(origen.precision)} m</small>}</span>
    <button type="button" onClick={() => setEligiendoOrigen(true)}>Cambiar</button>
  </div>;
  if (localizando) return <div className="ruta-origen"><span><LocateFixed size={14} aria-hidden="true" /> Buscando tu ubicación…</span></div>;
  if (origen) return <div className="ruta-origen">
    <span><MapPin size={14} aria-hidden="true" /> Desde {etiqueta(origen)}</span>
    <button type="button" onClick={usarUbicacion}>Usar mi ubicación</button>
  </div>;
  return <div className="ruta-origen ruta-origen-falta">
    <span>{errorUbicacion || "Activa tu ubicación para saber desde dónde sales."}</span>
    <div>
      {!siguiendo && <button type="button" onClick={usarUbicacion}>Usar mi ubicación</button>}
      <button type="button" onClick={() => setEligiendoOrigen(true)}>Elegir en el mapa</button>
    </div>
  </div>;
}

export function PanelRuta({ estado }) {
  const { origen, destino, ruta, error, cargando, quitarDestino } = estado;
  return (
    <section className="ruta-panel" aria-label="Cómo llegar">
      {destino ? <>
        <div className="ruta-destino">
          <div><span className="ruta-kicker">CÓMO LLEGAR A</span><h2>{etiqueta(destino)}</h2></div>
          <button className="ruta-quitar" type="button" onClick={quitarDestino} aria-label="Quitar destino"><X size={16} /></button>
        </div>
        <div role="status" aria-live="polite" className="ruta-resultado">
          {cargando && !ruta && <p>Buscando el mejor camino…</p>}
          {ruta && <p className="ruta-tiempo"><Footprints size={18} aria-hidden="true" />
            <strong>{minutosCaminando(ruta)} min</strong> <span>· {Math.round(ruta.distanciaMetros)} m a pie</span></p>}
          {!origen && !cargando && <p>Falta tu punto de partida.</p>}
        </div>
        {error && <p className="ruta-error" role="alert">{error}</p>}
      </> : <p className="ruta-help">Toca el mapa o busca un lugar y te mostramos cómo llegar.</p>}
      <Origen estado={estado} />
    </section>
  );
}

export function CapaRuta({ estado }) {
  const { origen, destino, eligiendoOrigen, tocarMapa, ruta } = estado;
  const map = useMap();
  const encuadrado = useRef(null);
  useMapEvents({ click(event) { tocarMapa({ latitud: event.latlng.lat, longitud: event.latlng.lng }); } });
  // Encuadra la ruta sólo la primera vez para cada destino; si el GPS la recalcula, no mueve el mapa.
  useEffect(() => {
    if (!ruta || encuadrado.current === ruta.destinoCalculo) return;
    encuadrado.current = ruta.destinoCalculo;
    map.fitBounds([...posicionesRuta(ruta.geometria), [ruta.origenCalculo.latitud, ruta.origenCalculo.longitud],
      [ruta.destinoCalculo.latitud, ruta.destinoCalculo.longitud]],
      { padding: [50, 50], maxZoom: 18, animate: !window.matchMedia("(prefers-reduced-motion: reduce)").matches });
  }, [ruta, map]);
  useEffect(() => {
    const container = map.getContainer();
    const anterior = container.style.cursor;
    container.style.cursor = "crosshair";
    return () => { container.style.cursor = anterior; };
  }, [eligiendoOrigen, map]);
  const fijo = { interactive: false, bubblingMouseEvents: false };
  const linea = ruta?.geometria.type === "LineString" ? posicionesRuta(ruta.geometria) : null;
  const tramos = ruta && [
    [[ruta.origenCalculo.latitud, ruta.origenCalculo.longitud], [ruta.origenConectado.latitud, ruta.origenConectado.longitud]],
    [[ruta.destinoConectado.latitud, ruta.destinoConectado.longitud], [ruta.destinoCalculo.latitud, ruta.destinoCalculo.longitud]],
  ];
  return <>
    {origen?.fuente === "gps" && <Circle center={[origen.latitud, origen.longitud]} radius={origen.precision} {...fijo} pathOptions={{ color: "#245bdf", fillOpacity: 0.08, weight: 1 }} />}
    {tramos?.map((t, i) => <Polyline key={i} positions={t} {...fijo} pathOptions={{ color: "#245bdf", weight: 3, opacity: 0.7, dashArray: "2 8" }} />)}
    {linea && <Polyline positions={linea} {...fijo} pathOptions={{ color: "#ffffff", weight: 10, opacity: 0.9 }} />}
    {linea && <Polyline positions={linea} {...fijo} pathOptions={{ color: "#245bdf", weight: 6, opacity: 0.95 }} />}
    {origen && (origen.fuente === "gps"
      ? <CircleMarker center={[origen.latitud, origen.longitud]} radius={8} {...fijo} pathOptions={{ color: "#ffffff", weight: 3, fillColor: "#245bdf", fillOpacity: 1 }} />
      : <CircleMarker center={[origen.latitud, origen.longitud]} radius={8} {...fijo} pathOptions={{ color: "#ffffff", weight: 3, fillColor: "#287967", fillOpacity: 1 }}><Tooltip direction="top">Salida</Tooltip></CircleMarker>)}
    {destino && <CircleMarker center={[destino.latitud, destino.longitud]} radius={10} {...fijo} pathOptions={{ color: "#ffffff", weight: 3, fillColor: "#ae3b37", fillOpacity: 1 }}>
      <Tooltip permanent direction="top">{etiqueta(destino)}</Tooltip>
    </CircleMarker>}
  </>;
}
