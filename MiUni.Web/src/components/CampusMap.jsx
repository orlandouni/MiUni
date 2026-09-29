import { useEffect, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

const colorPorCategoria = {
  Biblioteca: "#2563eb",
  Comida: "#f97316",
  Ingeniería: "#16a34a",
  Departamentos: "#7c3aed",
  Baños: "#64748b",
};

function crearIcono(categoria) {
  const color = colorPorCategoria[categoria] ?? "#ef4444";
  return L.divIcon({
    className: "",
    html: `<div style="background:${color};width:16px;height:16px;border-radius:50%;border:2px solid white;"></div>`,
    iconSize: [16, 16],
  });
}

function CampusMap() {
  const posicionInicial = [29.0833, -110.9627];
  const [lugares, setLugares] = useState([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetch("/api/Lugar")
      .then((res) => {
        if (!res.ok) throw new Error("Respuesta no OK");
        return res.json();
      })
      .then(setLugares)
      .catch(() => setError("No se pudieron cargar los lugares del campus"))
      .finally(() => setCargando(false));
  }, []);

  if (cargando) return <p>Cargando mapa...</p>;
  if (error) return <p>{error}</p>;

  return (
    <MapContainer
      center={posicionInicial}
      zoom={17}
      style={{ height: "500px", width: "100%" }}
    >
      <TileLayer
        attribution='&copy; OpenStreetMap contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />

      {lugares.map((lugar) => (
        <Marker
          key={lugar.id}
          position={[lugar.latitud, lugar.longitud]}
          icon={crearIcono(lugar.categoria.nombre)}
        >
          <Popup>
            <strong>{lugar.nombre}</strong>
            <br />
            Categoría: {lugar.categoria.nombre}
            {lugar.descripcion && (
              <>
                <br />
                {lugar.descripcion}
              </>
            )}
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  );
}

export default CampusMap;