// Capa base y límites compartidos por todos los mapas de MiUni.
// Teselas estándar de OpenStreetMap (sin API key). Se suavizan con CSS (.miuni-teselas) para un look más limpio.
// CARTO pidió API key en su capa "rastertiles", por eso no se usa.
export const TESELAS = {
  url: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
  attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  maxZoom: 19,
  className: "miuni-teselas",
};

// Perímetro del campus Unison centro (lugares del seed + ~150 m: Reforma, Luis Encinas, Rosales).
export const LIMITES_CAMPUS = [[29.0790, -110.9712], [29.0880, -110.9572]];

// Props para <MapContainer>. El zoom mínimo real lo fija <LimitarAlCampus /> según el tamaño del mapa.
export const LIMITES_MAPA = { maxBounds: LIMITES_CAMPUS, maxBoundsViscosity: 1, minZoom: 15, maxZoom: 19, zoomSnap: 0.25 };
