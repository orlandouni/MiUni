import { useEffect, useRef } from "react";
import { Marker, Popup, useMap } from "react-leaflet";
import { Link } from "react-router-dom";


export default function LugarDesdeChat({ lugar, elegir }) {
  const map = useMap();
  const marker = useRef(null);
  useEffect(() => {
    // No animation also respects reduced motion and avoids delayed popup races.
    map.setView([lugar.latitud, lugar.longitud], 18, { animate: false });
    marker.current?.openPopup();
  }, [lugar, map]);
  return (
    <Marker ref={marker} position={[lugar.latitud, lugar.longitud]}>
      <Popup>
        <strong>{lugar.nombre}</strong>
        {lugar.descripcion && <p>{lugar.descripcion}</p>}
        <p><button type="button" onClick={() => elegir("origen", lugar)}>Usar como origen</button>{" "}
          <button type="button" onClick={() => elegir("destino", lugar)}>Usar como destino</button></p>
        <Link to={`/lugares/${lugar.id}`}>Ver reseñas y guardar favorito</Link>
      </Popup>
    </Marker>
  );
}
