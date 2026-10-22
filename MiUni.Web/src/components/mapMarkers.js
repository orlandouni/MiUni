import L from 'leaflet';
export const categoryStyle = {
  Biblioteca: {color: '#245bdf', label: 'BI'},
  Comida: {color: '#bc652d', label: 'CO'},
  'Edificio académico': {color: '#287967', label: 'ED'},
  Departamentos: {color: '#526aa0', label: 'DE'},
  Oficinas: {color: '#287e94', label: 'OF'},
  Deportes: {color: '#a84a48', label: 'DP'},
  Baños: {color: '#64748b', label: 'WC'},
};
export function crearIcono(categoria, selected = false) {
  const {color, label} = categoryStyle[categoria] || {color: '#52677f', label: 'LU'};
  return L.divIcon({
    className: `campus-marker ${selected ? 'selected' : ''}`,
    html: `<span class="campus-marker-pin" style="--pin-color:${color}"><span>${label}</span></span>`,
    iconSize: [34, 42], iconAnchor: [17, 39], popupAnchor: [0, -37],
  });
}
