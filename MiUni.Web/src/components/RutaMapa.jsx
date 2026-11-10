import { useEffect } from "react";
import { Circle, CircleMarker, Polyline, Tooltip, useMap, useMapEvents } from "react-leaflet";

import { posicionesRuta } from "../services/rutas";

const etiqueta = punto => punto?.nombre ?? (punto ? `${punto.latitud.toFixed(5)}, ${punto.longitud.toFixed(5)}` : "Sin seleccionar");

export function PanelRuta({ estado }) {
  const { origen, destino, seleccion, setSeleccion, ruta, error, cargando, calcular, limpiar,
    localizando, siguiendo, errorUbicacion, usarUbicacion, cancelarUbicacion } = estado;
  const origenCambio = ruta?.origenCalculo && origen && (ruta.origenCalculo.latitud !== origen.latitud || ruta.origenCalculo.longitud !== origen.longitud);
  return (
    <section className="ruta-panel" aria-label="Ruta por el campus">
      <div className="ruta-panel-heading"><div><span className="ruta-kicker">ASISTENTE DE RUTA</span><h2>Ruta por el campus</h2></div><button className="ruta-clear" type="button" onClick={limpiar}>Limpiar</button></div>
      <p className="ruta-help">Elige los puntos en el mapa o utiliza los botones de un lugar.</p>
      <div className="ruta-gps">
        <p className="ruta-help">Con tu permiso, tu ubicación se actualiza mientras usas el mapa. Sólo enviamos las coordenadas a MiUni cuando calculas una ruta; esta función no guarda tu recorrido.</p>
        <button type="button" onClick={usarUbicacion} disabled={siguiendo}>Usar mi ubicación como punto A</button>
        {siguiendo && <><p role="status">{localizando ? "Obteniendo tu ubicación…" : "Ubicación continua activa. La ruta se calcula sólo cuando lo solicitas."}</p><button type="button" onClick={cancelarUbicacion}>Detener ubicación</button></>}
        {errorUbicacion && <p className="ruta-error" role="alert">{errorUbicacion}</p>}
      </div>
      <div className="ruta-actions">
        <button type="button" className={seleccion === "origen" ? "ruta-selected" : ""} aria-pressed={seleccion === "origen"} onClick={() => setSeleccion(seleccion === "origen" ? null : "origen")}>Elegir origen</button>
        <button type="button" className={seleccion === "destino" ? "ruta-selected" : ""} aria-pressed={seleccion === "destino"} onClick={() => setSeleccion(seleccion === "destino" ? null : "destino")}>Elegir destino</button>
        <button type="button" className="ruta-calculate" disabled={!origen || !destino || cargando || localizando} onClick={calcular}>{cargando ? "Calculando…" : "Calcular ruta"}</button>
      </div>
      <div className="ruta-points"><span><b>Origen</b>{etiqueta(origen)}</span><span><b>Destino</b>{etiqueta(destino)}</span></div>
      <div role="status" aria-live="polite">
        {origen?.fuente === "gps" && <p>Precisión aproximada del punto A: {Math.ceil(origen.precision)} m. {origen.precision > 100 ? "La ubicación es poco precisa; elige el origen en el mapa si no coincide contigo." : "Comprueba que el origen coincide contigo."} {!siguiendo && "Ubicación detenida: se muestra la última posición recibida."}</p>}
        {origenCambio && <p>Tu ubicación cambió desde el último cálculo. El recorrido y la distancia corresponden al origen anterior; pulsa «Calcular ruta» para actualizarlos.</p>}
        {seleccion && <p>Haz clic en el mapa para marcar el {seleccion}.</p>}
        {cargando && <p>Buscando una ruta transitable…</p>}
        {ruta && <>
          <p>Distancia por los caminos: <strong>{Math.round(ruta.distanciaMetros)} m</strong>.</p>
          {ruta.geometria.type === "Point" && <p>Ambos puntos conectan al mismo nodo de la red; no hay un tramo de camino entre ellos.</p>}
          <p>Separación hasta la red: origen {Math.round(ruta.distanciaConexionAMetros)} m; destino {Math.round(ruta.distanciaConexionBMetros)} m. Estos accesos no están incluidos en la distancia ni representan caminos verificados.</p>
        </>}
      </div>
      {error && <p className="ruta-error" role="alert">{error}</p>}
    </section>
  );
}

export function CapaRuta({ estado }) {
  const { origen, destino, seleccion, elegir, ruta } = estado;
  const map = useMap();
  useMapEvents({ click(event) {
    if (seleccion) elegir(seleccion, { latitud: event.latlng.lat, longitud: event.latlng.lng });
  } });
  useEffect(() => {
    if (ruta) map.fitBounds(posicionesRuta(ruta.geometria), { padding: [40, 40], maxZoom: 18, animate: !window.matchMedia("(prefers-reduced-motion: reduce)").matches });
  }, [ruta, map]);
  useEffect(() => {
    const container = map.getContainer();
    const anterior = container.style.cursor;
    if (seleccion) container.style.cursor = "crosshair";
    return () => { container.style.cursor = anterior; };
  }, [seleccion, map]);
  return <>
    {origen?.fuente === "gps" && <Circle center={[origen.latitud, origen.longitud]} radius={origen.precision} interactive={false} pathOptions={{ color: "#15803d", fillOpacity: 0.1, weight: 1 }} />}
    {ruta?.geometria.type === "LineString" && <Polyline positions={posicionesRuta(ruta.geometria)} pathOptions={{ color: "#2563eb", weight: 6, opacity: 0.9 }} />}
    {ruta?.geometria.type === "Point" && <CircleMarker center={posicionesRuta(ruta.geometria)[0]} radius={6} pathOptions={{ color: "#2563eb" }}><Tooltip>Nodo compartido</Tooltip></CircleMarker>}
    {[ ["A · Origen", origen, "#15803d"], ["B · Destino", destino, "#b91c1c"] ].map(([nombre, punto, color]) => punto &&
      <CircleMarker key={nombre} center={[punto.latitud, punto.longitud]} radius={9} pathOptions={{ color, fillOpacity: 0.8 }}>
        <Tooltip permanent direction="top">{nombre}</Tooltip>
      </CircleMarker>)}
  </>;
}
