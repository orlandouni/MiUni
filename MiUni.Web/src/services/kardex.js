import api from "./api";

export const ESTADOS_MATERIA = [
  { valor: "Aprobada", etiqueta: "Aprobada" },
  { valor: "Cursando", etiqueta: "Cursando" },
  { valor: "Reprobada", etiqueta: "Reprobada" },
  { valor: "Baja", etiqueta: "Baja" },
];

export const obtenerKardex = () => api.get("/api/Kardex").then(r => r.data);

// Convierte el formulario en la petición del API: campos vacíos van como null.
export function aPeticion(form) {
  const texto = valor => (valor ?? "").trim() || null;
  const numero = valor => (valor === "" || valor == null ? null : Number(valor));
  return {
    materia: (form.materia ?? "").trim(),
    clave: texto(form.clave),
    periodo: texto(form.periodo),
    semestre: numero(form.semestre),
    calificacion: numero(form.calificacion),
    creditos: numero(form.creditos) ?? 0,
    estado: form.estado,
  };
}

export const guardarMateria = (id, form) => id
  ? api.put(`/api/Kardex/${id}`, aPeticion(form))
  : api.post("/api/Kardex", aPeticion(form));

export const eliminarMateria = id => api.delete(`/api/Kardex/${id}`);
