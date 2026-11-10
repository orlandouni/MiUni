// Capa base y límites compartidos por todos los mapas de MiUni.
// CARTO Voyager: estilo limpio sobre datos de OpenStreetMap, sin API key (requiere atribución).
export const TESELAS = {
  url: "https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png",
  attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>',
  subdomains: "abcd",
  maxZoom: 20,
};

// Unison y ~500 m alrededor (la misma zona de la que se importan los caminos).
export const LIMITES_CAMPUS = [[29.0755, -110.975], [29.0915, -110.9535]];
export const ZOOM_MINIMO = 15;

// Props para <MapContainer>: no deja salir del campus ni alejarse hasta ver la ciudad o el mundo.
export const LIMITES_MAPA = { maxBounds: LIMITES_CAMPUS, maxBoundsViscosity: 1, minZoom: ZOOM_MINIMO, maxZoom: 20 };
