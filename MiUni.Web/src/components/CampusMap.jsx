import { useEffect, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Link } from "react-router-dom";
import { CapaRuta, PanelRuta } from "./RutaMapa";
import { useRuta } from "../services/useRuta";
import { Search, SlidersHorizontal } from "lucide-react";
import "./CampusMap.css";

const CENTRO_INICIAL = [29.0833, -110.9627];
const ZOOM_INICIAL = 17;
const SIN_LUGARES = []; // referencia estable para no re-disparar efectos

const colorPorCategoria = {
  Biblioteca: "#2563eb",
  Comida: "#f97316",
  "Edificio académico": "#16a34a",
  Departamentos: "#7c3aed",
  Oficinas: "#0891b2",
  Deportes: "#dc2626",
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

function useDebounce(value, delay = 300) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);
  return debounced;
}

// Sin filtro: vuelve a la vista del campus. Con resultados: los encuadra.
function AjustarVista({ lugares, hayFiltro }) {
  const map = useMap();
  useEffect(() => {
    if (!hayFiltro) {
      map.setView(CENTRO_INICIAL, ZOOM_INICIAL);
      return;
    }
    if (lugares.length === 0) return;
    const bounds = L.latLngBounds(lugares.map((l) => [l.latitud, l.longitud]));
    map.fitBounds(bounds, { padding: [40, 40], maxZoom: 18 });
  }, [lugares, hayFiltro, map]);
  return null;
}

function CampusMap() {
  const ruta = useRuta();
  const [categorias, setCategorias] = useState([]);
  const [texto, setTexto] = useState("");
  const [categoria, setCategoria] = useState(null); // nombre de la categoría
  const [resultado, setResultado] = useState({
    clave: null,
    lugares: SIN_LUGARES,
    error: false,
  });

  const q = useDebounce(texto).trim();
  const hayFiltro = q !== "" || categoria !== null;
  const clave = `${q}|${categoria ?? ""}`;

  // Categorías para los chips: una sola vez
  useEffect(() => {
    fetch("/api/Categoria")
      .then((res) => (res.ok ? res.json() : []))
      .then(setCategorias)
      .catch(() => {});
  }, []);

  // Solo se consulta la API cuando hay algo que buscar
  useEffect(() => {
    if (!hayFiltro) return;

    const controller = new AbortController();
    const params = new URLSearchParams();
    if (q) params.set("q", q);
    if (categoria) params.set("categoria", categoria);

    fetch(`/api/Lugar?${params}`, { signal: controller.signal })
      .then((res) => {
        if (!res.ok) throw new Error("Respuesta no OK");
        return res.json();
      })
      .then((data) => setResultado({ clave, lugares: data, error: false }))
      .catch((err) => {
        if (err.name === "AbortError") return;
        setResultado({ clave, lugares: SIN_LUGARES, error: true });
      });

    return () => controller.abort();
  }, [q, categoria, hayFiltro, clave]);

  // Lo que realmente se pinta en el mapa
  const lugares = hayFiltro ? resultado.lugares : SIN_LUGARES;
  const cargando = hayFiltro && resultado.clave !== clave;
  const error = hayFiltro && !cargando && resultado.error;
  const sinResultados = hayFiltro && !cargando && !error && lugares.length === 0;

  return (
    <div className="campus-map-shell">
      <div className="campus-controls">
        <div className="campus-search"><Search size={17} />
        <input
          type="search"
          placeholder="Buscar edificio o lugar (ej. 5K, biblioteca)"
          aria-label="Buscar lugar"
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
        />
        </div>

        <div className="campus-filters"><button className="campus-filter-toggle" type="button" onClick={() => document.querySelector(".campus-category-list")?.classList.toggle("visible")}><SlidersHorizontal size={15} /> Filtros</button><div className="campus-category-list">
          {categorias.map((c) => (
            <button
              key={c.id}
              type="button"
              aria-pressed={categoria === c.nombre}
              onClick={() => setCategoria(categoria === c.nombre ? null : c.nombre)}
            >
              <span
                className="campus-dot"
                style={{
                  display: "inline-block",
                  width: 10,
                  height: 10,
                  borderRadius: "50%",
                  marginRight: 6,
                  background: colorPorCategoria[c.nombre] ?? "#ef4444",
                }}
              />
              {c.nombre}
            </button>
          ))}</div>
        </div>

        {(cargando || error || sinResultados) && <p className="campus-status" role={error ? "alert" : "status"}>{cargando ? "Buscando lugares…" : error ? "No se pudieron cargar los lugares del campus" : "No encontramos lugares con esa búsqueda."}</p>}
      </div>

      <PanelRuta estado={ruta} />
      <MapContainer
        center={CENTRO_INICIAL}
        zoom={ZOOM_INICIAL}
        className="campus-leaflet"
      >
        <TileLayer
          attribution="&copy; OpenStreetMap contributors"
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <AjustarVista lugares={lugares} hayFiltro={hayFiltro} />
        <CapaRuta estado={ruta} />

        {lugares.map((lugar) => (
          <Marker
            key={lugar.id}
            position={[lugar.latitud, lugar.longitud]}
            icon={crearIcono(lugar.categoria.nombre)}
          >
            <Popup>
              <strong>{lugar.nombre}</strong>
              <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                <button type="button" onClick={() => ruta.elegir("origen", lugar)}>Usar como origen</button>
                <button type="button" onClick={() => ruta.elegir("destino", lugar)}>Usar como destino</button>
              </div>
              <br />
              Categoría: {lugar.categoria.nombre}
              {lugar.descripcion && (
                <>
                  <br />
                  {lugar.descripcion}
                </>
              )}
              <div style={{ marginTop: 12 }}>
                <Link to={`/lugares/${lugar.id}`}>
                Ver reseñas y guardar favorito
                </Link>
              </div>
            </Popup>
          </Marker>
        ))}
      </MapContainer>
    </div>
  );
}

export default CampusMap;
