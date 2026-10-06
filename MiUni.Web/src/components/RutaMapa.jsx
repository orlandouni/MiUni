import { useEffect } from "react";
import { CircleMarker, Polyline, Tooltip, useMap, useMapEvents } from "react-leaflet";

import { posicionesRuta } from "../services/rutas";

const etiqueta = punto => punto?.nombre ?? (punto ? `${punto.latitud.toFixed(5)}, ${punto.longitud.toFixed(5)}` : "Sin seleccionar");

export function PanelRuta({ estado }) {
  const { origen, destino, seleccion, setSeleccion, ruta, error, cargando, calcular, limpiar } = estado;
  return (
    <section aria-label="Ruta por el campus" style={{ padding: 12, marginBottom: 12, border: "1px solid #94a3b8", borderRadius: 8 }}>
      <h2>Ruta por el campus</h2>
      <p>Elige los puntos en el mapa o utiliza los botones de un lugar.</p>
      <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
        <button type="button" aria-pressed={seleccion === "origen"} onClick={() => setSeleccion(seleccion === "origen" ? null : "origen")}>Elegir origen</button>
        <button type="button" aria-pressed={seleccion === "destino"} onClick={() => setSeleccion(seleccion === "destino" ? null : "destino")}>Elegir destino</button>
        <button type="button" disabled={!origen || !destino || cargando} onClick={calcular}>{cargando ? "Calculando…" : "Calcular ruta"}</button>
        <button type="button" onClick={limpiar}>Limpiar ruta</button>
      </div>
      <p>Origen: {etiqueta(origen)}<br />Destino: {etiqueta(destino)}</p>
      <div role="status" aria-live="polite">
        {seleccion && <p>Haz clic en el mapa para marcar el {seleccion}.</p>}
        {cargando && <p>Buscando una ruta transitable…</p>}
        {ruta && <>
          <p>Distancia por los caminos: <strong>{Math.round(ruta.distanciaMetros)} m</strong>.</p>
          {ruta.geometria.type === "Point" && <p>Ambos puntos conectan al mismo nodo de la red; no hay un tramo de camino entre ellos.</p>}
          <p>Separación hasta la red: origen {Math.round(ruta.distanciaConexionAMetros)} m; destino {Math.round(ruta.distanciaConexionBMetros)} m. Estos accesos no están incluidos en la distancia ni representan caminos verificados.</p>
        </>}
      </div>
      {error && <p role="alert">{error}</p>}
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
    if (ruta) map.fitBounds(posicionesRuta(ruta.geometria), { padding: [40, 40], maxZoom: 18 });
  }, [ruta, map]);
  useEffect(() => {
    const container = map.getContainer();
    const anterior = container.style.cursor;
    if (seleccion) container.style.cursor = "crosshair";
    return () => { container.style.cursor = anterior; };
  }, [seleccion, map]);
  return <>
    {ruta?.geometria.type === "LineString" && <Polyline positions={posicionesRuta(ruta.geometria)} pathOptions={{ color: "#2563eb", weight: 6, opacity: 0.9 }} />}
    {ruta?.geometria.type === "Point" && <CircleMarker center={posicionesRuta(ruta.geometria)[0]} radius={6} pathOptions={{ color: "#2563eb" }}><Tooltip>Nodo compartido</Tooltip></CircleMarker>}
    {[ ["Origen", origen, "#15803d"], ["Destino", destino, "#b91c1c"] ].map(([nombre, punto, color]) => punto &&
      <CircleMarker key={nombre} center={[punto.latitud, punto.longitud]} radius={9} pathOptions={{ color, fillOpacity: 0.8 }}>
        <Tooltip permanent direction="top">{nombre}</Tooltip>
      </CircleMarker>)}
  </>;
}
