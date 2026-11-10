import { useEffect } from "react";
import { useMap } from "react-leaflet";
import { LIMITES_CAMPUS } from "./mapaBase";

// El zoom mínimo es el que muestra todo el campus completo (con zoomSnap fino el margen de afuera es mínimo)
// y maxBounds impide arrastrar fuera de él.
export default function LimitarAlCampus() {
  const map = useMap();
  useEffect(() => {
    const ajustar = () => {
      map.invalidateSize({ animate: false });
      const { x, y } = map.getSize();
      if (!x || !y) return;
      const minimo = map.getBoundsZoom(LIMITES_CAMPUS, false);
      map.setMinZoom(minimo);
      if (map.getZoom() < minimo) map.setZoom(minimo, { animate: false });
      map.panInsideBounds(LIMITES_CAMPUS, { animate: false });
    };
    ajustar();
    const observer = new ResizeObserver(ajustar);
    observer.observe(map.getContainer());
    return () => observer.disconnect();
  }, [map]);
  return null;
}
