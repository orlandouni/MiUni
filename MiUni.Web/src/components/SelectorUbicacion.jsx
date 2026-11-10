import { useEffect, useState } from "react";
import { Circle, CircleMarker, MapContainer, TileLayer, useMap, useMapEvents } from "react-leaflet";
import { Crosshair, MapPin } from "lucide-react";
import "leaflet/dist/leaflet.css";
import "./SelectorUbicacion.css";
import { LIMITES_MAPA, TESELAS } from "./mapaBase";
import LimitarAlCampus from "./LimitarAlCampus";

const CENTRO_CAMPUS = [29.0833, -110.9627];

function ClicEnMapa({ onElegir }) {
  useMapEvents({ click: e => onElegir([e.latlng.lat, e.latlng.lng]) });
  return null;
}

function Centrar({ posicion, version }) {
  const map = useMap();
  // Sólo recentra cuando la posición viene del GPS, no en cada clic.
  useEffect(() => { if (version) map.setView(posicion, 18); }, [version]); // eslint-disable-line react-hooks/exhaustive-deps
  return null;
}

// Mapa para elegir dónde está un negocio. `ambulante` dibuja una zona en lugar de un punto.
export default function SelectorUbicacion({ valor, onCambio, ambulante = false }) {
  const [gps, setGps] = useState({ estado: "", version: 0 });
  const posicion = valor || CENTRO_CAMPUS;

  function usarMiUbicacion() {
    if (!navigator.geolocation) { setGps(g => ({ ...g, estado: "Tu navegador no permite obtener la ubicación." })); return; }
    setGps(g => ({ ...g, estado: "Buscando tu ubicación…" }));
    navigator.geolocation.getCurrentPosition(
      p => { onCambio([p.coords.latitude, p.coords.longitude]); setGps(g => ({ estado: "", version: g.version + 1 })); },
      () => setGps(g => ({ ...g, estado: "No pudimos obtener tu ubicación. Marca el punto en el mapa." })),
      { enableHighAccuracy: true, timeout: 10000 },
    );
  }

  return <div className="sel-ubicacion">
    <div className="sel-ubicacion-barra">
      <span><MapPin size={15} aria-hidden="true" />
        {valor ? (ambulante ? "Zona marcada" : "Ubicación marcada") : (ambulante ? "Toca el mapa donde sueles vender" : "Toca el mapa donde está tu negocio")}
      </span>
      <button type="button" onClick={usarMiUbicacion}><Crosshair size={15} aria-hidden="true" /> Usar mi ubicación</button>
    </div>
    <MapContainer center={posicion} zoom={17} scrollWheelZoom={false} className="sel-ubicacion-mapa" {...LIMITES_MAPA}>
      <TileLayer {...TESELAS} />
          <LimitarAlCampus />
      <ClicEnMapa onElegir={onCambio} />
      <Centrar posicion={posicion} version={gps.version} />
      {valor && ambulante && <Circle center={valor} radius={45} pathOptions={{ color: "#245bdf", fillColor: "#245bdf", fillOpacity: 0.15, weight: 2, dashArray: "6 6" }} />}
      {valor && <CircleMarker center={valor} radius={10} pathOptions={{ color: "#102e50", fillColor: "#f4ca64", fillOpacity: 1, weight: 3 }} />}
    </MapContainer>
    {gps.estado && <p className="sel-ubicacion-nota" role="status">{gps.estado}</p>}
  </div>;
}
