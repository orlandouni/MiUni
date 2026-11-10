import api from "./api";
import { aSolicitud } from "./horarioUtils";

export async function obtenerHorario() {
  const { data } = await api.get("/api/Horario");
  return data;
}

// Lee el PDF del portal y devuelve las clases encontradas, sin guardarlas.
export async function leerHorarioPdf(archivo) {
  const formulario = new FormData();
  formulario.append("archivo", archivo);
  const { data } = await api.post("/api/Horario/importar", formulario);
  return data;
}

// Reemplaza todo el horario con estas clases.
export async function guardarHorario(clases) {
  const { data } = await api.put("/api/Horario", clases.map(aSolicitud));
  return data;
}

export async function agregarClase(clase) {
  const { data } = await api.post("/api/Horario", aSolicitud(clase));
  return data;
}

export async function eliminarClase(id) {
  try {
    await api.delete(`/api/Horario/${id}`);
  } catch (error) {
    if (error.response?.status !== 404) throw error;
  }
}
