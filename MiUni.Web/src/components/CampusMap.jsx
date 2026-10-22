import { useEffect, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Link, useSearchParams } from "react-router-dom";
import LugarDesdeChat from "./LugarDesdeChat";
import { useLugarDesdeChat } from "../services/useLugarDesdeChat";
import { CapaRuta, PanelRuta } from "./RutaMapa";
import { useRuta } from "../services/useRuta";
import { Search, SlidersHorizontal, MapPin, ArrowUpRight, X, Route, Compass } from "lucide-react";
import {crearIcono, categoryStyle} from "./mapMarkers";
import "./CampusMap.css";

const CENTRO_INICIAL = [29.0833, -110.9627];
const ZOOM_INICIAL = 17;
const SIN_LUGARES = []; // referencia estable para no re-disparar efectos

function useDebounce(value, delay = 300) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);
  return debounced;
}

// Sin filtro: vuelve a la vista del campus. Con resultados: los encuadra.
function AjustarVista({ lugares, hayFiltro, lugarSeleccionado }) {
  const map = useMap();
  useEffect(() => {
    if (lugarSeleccionado) return;
    if (!hayFiltro) {
      map.setView(CENTRO_INICIAL, ZOOM_INICIAL, {animate: !window.matchMedia("(prefers-reduced-motion: reduce)").matches});
      return;
    }
    if (lugares.length === 0) return;
    const bounds = L.latLngBounds(lugares.map((l) => [l.latitud, l.longitud]));
    map.fitBounds(bounds, { padding: [50, 50], maxZoom: 18, animate: !window.matchMedia("(prefers-reduced-motion: reduce)").matches });
  }, [lugares, hayFiltro, lugarSeleccionado, map]);
  return null;
}

function VistaMapa({lugar}) {
  const map = useMap();
  useEffect(() => {
    const resize = new ResizeObserver(() => map.invalidateSize({animate: false}));
    resize.observe(map.getContainer());
    return () => resize.disconnect();
  }, [map]);
  useEffect(() => {
    if (lugar) map.flyTo([lugar.latitud, lugar.longitud], 18, {duration: .65, animate: !window.matchMedia("(prefers-reduced-motion: reduce)").matches});
  }, [lugar, map]);
  return null;
}

function CampusMap() {
  const [params, setParams] = useSearchParams();
  const lugarId = params.get("lugar");
  const { lugar: lugarChat, error: errorChat } = useLugarDesdeChat(lugarId);
  const salirDeLugarChat = () => {
    if (!lugarId) return;
    setParams(actuales => {
      const siguientes = new URLSearchParams(actuales);
      siguientes.delete("lugar");
      return siguientes;
    });
  };
  const ruta = useRuta();
  const [categorias, setCategorias] = useState([]);
  const [filtrosAbiertos, setFiltrosAbiertos] = useState(() => window.matchMedia("(min-width: 701px)").matches);
  const [seleccionado, setSeleccionado] = useState(null);
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

  const detalle = seleccionado || lugarChat;
  return (
    <div className="campus-map-shell">
      <aside className="campus-side" aria-label="Explorar lugares y rutas">
        <div className="campus-controls">
          <span className="ui-eyebrow"><Compass size={13} /> Encuentra tu lugar</span>
          <h3>¿A dónde vamos?</h3>
          <div className="campus-search"><Search size={18} aria-hidden="true" />
            <input type="search" placeholder="Edificio, biblioteca, café…" aria-label="Buscar lugar" value={texto}
              onChange={e => { salirDeLugarChat(); setSeleccionado(null); setTexto(e.target.value); }} />
          </div>
          <div className="campus-filters">
            <button className="campus-filter-toggle" type="button" aria-expanded={filtrosAbiertos} onClick={() => setFiltrosAbiertos(actual => !actual)}><SlidersHorizontal size={14} /> Categorías {categoria && <span className="campus-filter-count">1</span>}</button>
            <div className={`campus-category-list ${filtrosAbiertos ? "visible" : ""}`}>
              {categorias.map(c => <button key={c.id} type="button" aria-pressed={categoria === c.nombre}
                onClick={() => { salirDeLugarChat(); setSeleccionado(null); setCategoria(categoria === c.nombre ? null : c.nombre); }}>
                <span className="campus-dot" style={{background: categoryStyle[c.nombre]?.color || "#52677f"}} />{c.nombre}
              </button>)}
              {!categorias.length && <span className="campus-status">Las categorías aparecerán al conectar con el campus.</span>}
            </div>
          </div>
          {(texto || categoria) && <button className="campus-clear" type="button" onClick={() => {setTexto(""); setCategoria(null); setSeleccionado(null);}}>Limpiar búsqueda <X size={12} /></button>}
          {(cargando || error || sinResultados) && <p className="campus-status" role={error ? "alert" : "status"}>{cargando ? "Buscando lugares…" : error ? "No se pudieron cargar los lugares del campus. Intenta otra búsqueda." : "No encontramos ese lugar. Prueba con otro nombre o categoría."}</p>}
          {lugarId && <p className="campus-status" role={errorChat ? "alert" : "status"}>{errorChat || (lugarChat ? `Desde el chat: ${lugarChat.nombre}` : "Cargando lugar del chat…")} <button type="button" onClick={salirDeLugarChat}>Volver a la búsqueda</button></p>}
          {hayFiltro && !cargando && !error && lugares.length > 0 && <div className="campus-results">
            <p className="campus-result-count">{lugares.length} {lugares.length === 1 ? "lugar encontrado" : "lugares encontrados"}</p>
            {lugares.map(lugar => <button key={lugar.id} className={detalle?.id === lugar.id ? "active" : ""} aria-pressed={detalle?.id === lugar.id} onClick={() => setSeleccionado(lugar)}>
              <MapPin size={16} /><span><strong>{lugar.nombre}</strong><small>{lugar.categoria?.nombre}</small></span><ArrowUpRight size={14} />
            </button>)}
          </div>}
        </div>
        {detalle ? <section className="campus-place-card" aria-label="Lugar seleccionado">
          <span className="ui-eyebrow"><MapPin size={12} /> {detalle.categoria?.nombre || "En el campus"}</span>
          <h3>{detalle.nombre}</h3>
          {detalle.descripcion && <p>{detalle.descripcion}</p>}
          <div className="campus-place-actions"><button type="button" onClick={() => ruta.elegir("origen", detalle)}>Desde aquí</button><button type="button" onClick={() => ruta.elegir("destino", detalle)}><Route size={13} /> Ir aquí</button></div>
          <Link to={`/lugares/${detalle.id}`}>Reseñas y favoritos <ArrowUpRight size={13} /></Link>
        </section> : !hayFiltro && <div className="campus-welcome"><MapPin size={23} /><p>El campus, sin perderte.</p><span>Busca un lugar o elige una categoría para empezar a explorar.</span></div>}
        <PanelRuta estado={ruta} />
      </aside>
      <div className="campus-map-stage">
        <div className="campus-map-label"><span /> Universidad de Sonora <small>Campus Hermosillo</small></div>
        <MapContainer center={CENTRO_INICIAL} zoom={ZOOM_INICIAL} className="campus-leaflet" zoomControl={true}>
          <TileLayer attribution="&copy; OpenStreetMap contributors" url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
          <AjustarVista lugares={lugares} hayFiltro={hayFiltro} lugarSeleccionado={lugarId || seleccionado?.id} />
          <VistaMapa lugar={seleccionado} />
          <CapaRuta estado={ruta} />
          {lugarChat && <LugarDesdeChat lugar={lugarChat} elegir={ruta.elegir} icon={crearIcono(lugarChat.categoria?.nombre, true)} />}
          {lugares.filter(lugar => lugar.id !== lugarChat?.id).map(lugar => <Marker key={lugar.id} position={[lugar.latitud, lugar.longitud]}
            title={lugar.nombre} alt={lugar.nombre} icon={crearIcono(lugar.categoria?.nombre, seleccionado?.id === lugar.id)} eventHandlers={{click: () => setSeleccionado(lugar)}}>
            <Popup><div className="campus-popup"><span>{lugar.categoria?.nombre}</span><strong>{lugar.nombre}</strong><p>{lugar.descripcion}</p>
              <div><button type="button" onClick={() => ruta.elegir("origen", lugar)}>Desde aquí</button><button type="button" onClick={() => ruta.elegir("destino", lugar)}>Ir aquí</button></div>
              <Link to={`/lugares/${lugar.id}`}>Ver ficha del lugar →</Link>
            </div></Popup>
          </Marker>)}
        </MapContainer>
      </div>
    </div>
  );
}

export default CampusMap;
