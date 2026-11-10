import axios from "axios";

export function posicionesRuta(geometria) {
  const coordenadas = geometria.type === "Point" ? [geometria.coordinates] : geometria.coordinates;
  return coordenadas.map(([longitud, latitud]) => [latitud, longitud]);
}

export async function calcularRuta(origen, destino, signal) {
  try {
    const { data } = await axios.post(`${import.meta.env?.VITE_API_URL || ""}/api/Ruta`, {
      a: { latitud: origen.latitud, longitud: origen.longitud },
      b: { latitud: destino.latitud, longitud: destino.longitud },
      radioConexionMetros: 100,
    }, { signal, timeout: 20000 });
    return data;
  } catch (error) {
    if (signal?.aborted) throw error;
    if (error.response?.status === 404) throw new Error("No hay una ruta a pie hasta ese punto. Elige un destino más cerca de una calle o andador; si estás fuera del campus, la red de caminos puede no llegar hasta ti.", { cause: error });
    if (error.response?.status === 400) throw new Error("No se pudieron usar esos puntos. Selecciona de nuevo el origen y el destino.", { cause: error });
    throw new Error("No se pudo calcular la ruta. Comprueba la conexión con la API e inténtalo de nuevo.", { cause: error });
  }
}

// Distancia aproximada (haversine) entre dos puntos { latitud, longitud }, en metros.
export function distanciaMetros(a, b) {
  const rad = Math.PI / 180;
  const dLat = (b.latitud - a.latitud) * rad;
  const dLon = (b.longitud - a.longitud) * rad;
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(a.latitud * rad) * Math.cos(b.latitud * rad) * Math.sin(dLon / 2) ** 2;
  return 2 * 6371000 * Math.asin(Math.sqrt(h));
}

// Minutos a pie (~4.5 km/h) contando también el trecho hasta y desde el camino.
export function minutosCaminando(ruta) {
  const metros = ruta.distanciaMetros + ruta.distanciaConexionAMetros + ruta.distanciaConexionBMetros;
  return Math.max(1, Math.round(metros / 75));
}
