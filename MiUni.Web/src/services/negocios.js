import api from "./api";

const datos = r => r.data;

export const negociosApi = {
  mios: () => api.get("/api/Negocio/mios").then(datos),
  categorias: () => api.get("/api/Categoria").then(datos),
  solicitar: body => api.post("/api/Negocio/solicitudes", body).then(datos),
  editar: (id, body) => api.put(`/api/Negocio/${id}`, body).then(datos),
  panel: id => api.get(`/api/Negocio/${id}/panel`).then(datos),
  info: id => api.get(`/api/Negocio/${id}/info`).then(datos),
  guardarHorarios: (id, horarios) => api.put(`/api/Negocio/${id}/horarios`, { horarios }).then(datos),
  crearProducto: (id, body) => api.post(`/api/Negocio/${id}/productos`, body).then(datos),
  editarProducto: (id, productoId, body) => api.put(`/api/Negocio/${id}/productos/${productoId}`, body).then(datos),
  eliminarProducto: (id, productoId) => api.delete(`/api/Negocio/${id}/productos/${productoId}`),
};

// 1 = lunes … 7 = domingo, igual que en la API.
export const DIAS = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];

export const precio = valor =>
  new Intl.NumberFormat("es-MX", { style: "currency", currency: "MXN" }).format(valor);
