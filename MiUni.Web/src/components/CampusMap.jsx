import { useEffect, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Link, useSearchParams } from "react-router-dom";
import LugarDesdeChat from "./LugarDesdeChat";
import { useLugarDesdeChat } from "../services/useLugarDesdeChat";
import { CapaRuta, PanelRuta } from "./RutaMapa";
import { useRuta } from "../services/useRuta";
import { Search, SlidersHorizontal, MapPin, ArrowUpRight, X, Route, Compass, Clock } from "lucide-react";
import InfoNegocioPublica from "./InfoNegocioPublica";
import {crearIcono, categoryStyle} from "./mapMarkers";
import "./CampusMap.css";
import { LIMITES_MAPA, TESELAS } from "./mapaBase";

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

function EstadoApertura({ abierto }) {
  return <span className={`campus-open-badge ${abierto ? "abierto" : "cerrado"}`}>{abierto ? "Abierto ahora" : "Cerrado"}</span>;
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
  const [abierto, setAbierto] = useState(false);
  const [resultado, setResultado] = useState({
    clave: null,
    lugares: SIN_LUGARES,
    error: false,
  });

  const q = useDebounce(texto).trim();
  const hayFiltro = q !== "" || categoria !== null || abierto;
  const clave = `${q}|${categoria ?? ""}|${abierto}`;

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
    if (abierto) params.set("abierto", "true");

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
  }, [q, categoria, abierto, hayFiltro, clave]);

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
            <button className="campus-open-toggle" type="button" aria-pressed={abierto}
              onClick={() => { salirDeLugarChat(); setSeleccionado(null); setAbierto(a => !a); }}><Clock size={14} /> Abierto ahora</button>
            <div className={`campus-category-list ${filtrosAbiertos ? "visible" : ""}`}>
              {categorias.map(c => <button key={c.id} type="button" aria-pressed={categoria === c.nombre}
                onClick={() => { salirDeLugarChat(); setSeleccionado(null); setCategoria(categoria === c.nombre ? null : c.nombre); }}>
                <span className="campus-dot" style={{background: categoryStyle[c.nombre]?.color || "#52677f"}} />{c.nombre}
              </button>)}
              {!categorias.length && <span className="campus-status">Las categorías aparecerán al conectar con el campus.</span>}
            </div>
          </div>
          {(texto || categoria || abierto) && <button className="campus-clear" type="button" onClick={() => {setTexto(""); setCategoria(null); setAbierto(false); setSeleccionado(null);}}>Limpiar búsqueda <X size={12} /></button>}
          {(cargando || error || sinResultados) && <p className="campus-status" role={error ? "alert" : "status"}>{cargando ? "Buscando lugares…" : error ? "No se pudieron cargar los lugares del campus. Intenta otra búsqueda." : "No encontramos ese lugar. Prueba con otro nombre o categoría."}</p>}
          {lugarId && <p className="campus-status" role={errorChat ? "alert" : "status"}>{errorChat || (lugarChat ? `Desde el chat: ${lugarChat.nombre}` : "Cargando lugar del chat…")} <button type="button" onClick={salirDeLugarChat}>Volver a la búsqueda</button></p>}
          {hayFiltro && !cargando && !error && lugares.length > 0 && <div className="campus-results">
            <p className="campus-result-count">{lugares.length} {lugares.length === 1 ? "lugar encontrado" : "lugares encontrados"}</p>
            {lugares.map(lugar => <button key={lugar.id} className={detalle?.id === lugar.id ? "active" : ""} aria-pressed={detalle?.id === lugar.id} onClick={() => { setSeleccionado(lugar); ruta.elegir("destino", lugar); }}>
              <MapPin size={16} /><span><strong>{lugar.nombre}</strong><small>{lugar.categoria?.nombre}{lugar.abiertoAhora != null && <EstadoApertura abierto={lugar.abiertoAhora} />}</small></span><ArrowUpRight size={14} />
            </button>)}
          </div>}
        </div>
        <PanelRuta estado={ruta} />
        {detalle ? <section className="campus-place-card" aria-label="Lugar seleccionado">
          <span className="ui-eyebrow"><MapPin size={12} /> {detalle.categoria?.nombre || "En el campus"}</span>
          <h3>{detalle.nombre}</h3>
          {detalle.abiertoAhora != null && <EstadoApertura abierto={detalle.abiertoAhora} />}
          {detalle.piso != null && <p className="campus-place-meta">Piso {detalle.piso}</p>}
          {detalle.descripcion && <p>{detalle.descripcion}</p>}
          <div className="campus-place-actions">{ruta.destino?.id !== detalle.id && <button type="button" className="primario" onClick={() => ruta.elegir("destino", detalle)}><Route size={13} /> Cómo llegar</button>}<button type="button" onClick={() => ruta.elegir("origen", detalle)}>Salir desde aquí</button></div>
          <InfoNegocioPublica key={detalle.id} lugarId={detalle.id} />
          <Link to={`/lugares/${detalle.id}`}>Reseñas y favoritos <ArrowUpRight size={13} /></Link>
        </section> : !hayFiltro && !ruta.destino && <div className="campus-welcome"><MapPin size={23} /><p>El campus, sin perderte.</p><span>Busca un lugar o elige una categoría para empezar a explorar.</span></div>}
      </aside>
      <div className="campus-map-stage">
        <div className="campus-map-label"><span /> Universidad de Sonora <small>Campus Hermosillo</small></div>
        <MapContainer center={CENTRO_INICIAL} zoom={ZOOM_INICIAL} className="campus-leaflet" zoomControl={true} {...LIMITES_MAPA}>
          <TileLayer {...TESELAS} />
          <AjustarVista lugares={lugares} hayFiltro={hayFiltro} lugarSeleccionado={lugarId || seleccionado?.id} />
          <VistaMapa lugar={seleccionado} />
          <CapaRuta estado={ruta} />
          {lugarChat && <LugarDesdeChat lugar={lugarChat} elegir={ruta.elegir} icon={crearIcono(lugarChat.categoria?.nombre, true)} />}
          {lugares.filter(lugar => lugar.id !== lugarChat?.id).map(lugar => <Marker key={lugar.id} position={[lugar.latitud, lugar.longitud]}
            title={lugar.nombre} alt={lugar.nombre} icon={crearIcono(lugar.categoria?.nombre, seleccionado?.id === lugar.id)} eventHandlers={{click: () => { setSeleccionado(lugar); ruta.elegir("destino", lugar); }}}>
            <Popup><div className="campus-popup"><span>{lugar.categoria?.nombre}{lugar.esAmbulante ? " · Ambulante (zona aproximada)" : ""}</span><strong>{lugar.nombre}</strong><p>{lugar.descripcion}</p>
              <div><button type="button" onClick={() => ruta.elegir("origen", lugar)}>Salir desde aquí</button></div>
              <Link to={`/lugares/${lugar.id}`}>Ver ficha del lugar →</Link>
            </div></Popup>
          </Marker>)}
        </MapContainer>
      </div>
    </div>
  );
}

export default CampusMap;
