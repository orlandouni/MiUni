import api from "./api";

// Envoltorios de los endpoints del panel. Devuelven directamente `data`.
const get = (url, params) => api.get(url, { params }).then(r => r.data);

export const adminApi = {
  panel: () => get("/api/Admin/panel"),

  reportes: params => get("/api/ReporteUsuario", params),
  estadoReporte: (id, estado) => api.patch(`/api/ReporteUsuario/${id}/estado`, { estado }),
  eliminarReporte: id => api.delete(`/api/ReporteUsuario/${id}`),

  negocios: estado => get("/api/Admin/negocios", estado ? { estado } : undefined),
  revisarNegocio: (id, accion) => api.post(`/api/Negocio/solicitudes/${id}/${accion}`),

  lugares: () => get("/api/Lugar/admin"),
  crearLugar: body => api.post("/api/Lugar", body).then(r => r.data),
  editarLugar: (id, body) => api.put(`/api/Lugar/${id}`, body),
  desactivarLugar: id => api.delete(`/api/Lugar/${id}`),

  categorias: () => get("/api/Categoria"),
  crearCategoria: body => api.post("/api/Categoria", body).then(r => r.data),
  editarCategoria: (id, body) => api.put(`/api/Categoria/${id}`, body),
  eliminarCategoria: id => api.delete(`/api/Categoria/${id}`),

  resenas: params => get("/api/Admin/resenas", params),
  eliminarResena: id => api.delete(`/api/Admin/resenas/${id}`),

  usuarios: params => get("/api/Admin/usuarios", params),
  rolesUsuario: (id, roles) => api.put(`/api/Admin/usuarios/${id}/roles`, { roles }).then(r => r.data),
  bloquearUsuario: (id, bloquear) => api.post(`/api/Admin/usuarios/${id}/${bloquear ? "bloquear" : "desbloquear"}`),

  carreras: () => get("/api/Admin/carreras"),
  crearCarrera: body => api.post("/api/Admin/carreras", body).then(r => r.data),
  editarCarrera: (id, body) => api.put(`/api/Admin/carreras/${id}`, body),
  eliminarCarrera: id => api.delete(`/api/Admin/carreras/${id}`),
};

// Quita claves vacías para no enviar filtros sin valor.
export function limpiarParams(params) {
  return Object.fromEntries(Object.entries(params).filter(([, v]) => v !== "" && v !== null && v !== undefined));
}
