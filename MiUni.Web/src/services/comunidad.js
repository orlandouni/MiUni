import api from "./api";

export async function obtenerLugares() {
  const { data } = await api.get("/api/Lugar");
  return data;
}

export async function obtenerResenas(lugarId) {
  const { data } = await api.get("/api/Resena", {
    params: lugarId ? { lugarId } : {},
  });

  return data;
}

export async function crearResena(lugarId, calificacion, comentario) {
  const { data } = await api.post("/api/Resena", {
    lugarId,
    calificacion: Number(calificacion),
    comentario: comentario.trim() || null,
  });

  return data;
}

export async function editarResena(id, calificacion, comentario) {
  await api.put(`/api/Resena/${id}`, {
    calificacion: Number(calificacion),
    comentario: comentario.trim() || null,
  });
}

export async function eliminarResena(id) {
  await api.delete(`/api/Resena/${id}`);
}

export async function obtenerFavoritos() {
  const { data } = await api.get("/api/Favorito");
  return data;
}

export async function guardarFavorito(lugarId) {
  await api.put(`/api/Favorito/${lugarId}`);
}

export async function quitarFavorito(lugarId) {
  try {
    await api.delete(`/api/Favorito/${lugarId}`);
  } catch (error) {
    // Si ya se quitó desde otra pestaña, el resultado deseado se cumplió.
    if (error.response?.status !== 404) throw error;
  }
}

export function mensajeError(error) {
  const status = error.response?.status;
  const data = error.response?.data;

  if (!error.response) {
    return "No se pudo conectar con el servidor. Comprueba que la API esté encendida.";
  }

  if (status === 401) {
    return "Tu sesión no está disponible o venció. Vuelve a iniciar sesión.";
  }

  if (status === 403) {
    return "Solo puedes modificar tus propias reseñas.";
  }

  if (status === 404) {
    return "El registro ya no está disponible. Actualiza la lista.";
  }

  if (data?.errors) {
    return Object.values(data.errors).flat().join(" ");
  }

  if (data?.message) return data.message;

  return "No se pudo completar la operación. Intenta nuevamente.";
}