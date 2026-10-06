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
    if (error.response?.status === 404) throw new Error("No hay una ruta disponible entre esos puntos. Prueba puntos más cercanos a los caminos cargados (hasta 100 m de sus extremos).", { cause: error });
    if (error.response?.status === 400) throw new Error("No se pudieron usar esos puntos. Selecciona de nuevo el origen y el destino.", { cause: error });
    throw new Error("No se pudo calcular la ruta. Comprueba la conexión con la API e inténtalo de nuevo.", { cause: error });
  }
}
