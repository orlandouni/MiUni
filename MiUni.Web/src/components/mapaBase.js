// Capa base y límites compartidos por todos los mapas de MiUni.
// Teselas estándar de OpenStreetMap (sin API key). Se suavizan con CSS (.miuni-teselas) para un look más limpio.
// CARTO pidió API key en su capa "rastertiles", por eso no se usa.
export const TESELAS = {
  url: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
  attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  maxZoom: 19,
  className: "miuni-teselas",
};

// Unison y ~500 m alrededor (la misma zona de la que se importan los caminos).
export const LIMITES_CAMPUS = [[29.0755, -110.975], [29.0915, -110.9535]];
export const ZOOM_MINIMO = 15;

// Props para <MapContainer>: no deja salir del campus ni alejarse hasta ver la ciudad o el mundo.
export const LIMITES_MAPA = { maxBounds: LIMITES_CAMPUS, maxBoundsViscosity: 1, minZoom: ZOOM_MINIMO, maxZoom: 19 };
