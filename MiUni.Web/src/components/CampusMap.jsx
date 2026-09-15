import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";
import "leaflet/dist/leaflet.css";

function CampusMap() {
  const posicionInicial = [29.0833, -110.9627];

  const lugares = [
    {
      nombre: "Biblioteca Central",
      categoria: "Biblioteca",
      coordenadas: [29.083336885926236, -110.9631235545639],
    },
    {
      nombre: "Edificio 5K - Ingeniería Industrial",
      categoria: "Ingeniería",
      coordenadas: [29.082433958767652, -110.96229774960754],
    },
    {
      nombre: "Comedor Universitario",
      categoria: "Comida",
      coordenadas: [29.083479582078365, -110.96239170625606],
    },
  ];

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
        <Marker key={lugar.nombre} position={lugar.coordenadas}>
          <Popup>
            <strong>{lugar.nombre}</strong>
            <br />
            Categoría: {lugar.categoria}
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  );
}

export default CampusMap;