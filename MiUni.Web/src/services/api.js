import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL;

const api = axios.create({
  baseURL: API_URL,
});

const TOKEN_KEY = "miuni_token";
const EXPIRES_KEY = "miuni_token_expires";

// Guarda la sesión después de iniciar sesión
export function setSession(token, expires) {
  localStorage.setItem(TOKEN_KEY, token);

  if (expires) {
    localStorage.setItem(EXPIRES_KEY, expires);
  }
}

// Obtiene el token guardado
export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

// Cierra/elimina la sesión
export function clearSession() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EXPIRES_KEY);
}

// Saber si existe una sesión
export function isAuthenticated() {
  return !!getToken();
}

// Agrega automáticamente el JWT a las peticiones protegidas
api.interceptors.request.use((config) => {
  const token = getToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

// LOGIN
export async function loginRequest(email, password) {
  const { data } = await api.post("/api/Auth/login", {
    email,
    password,
  });

  return data;
}

// REGISTRO
export async function registerRequest(email, password) {
  const { data } = await api.post("/api/Auth/register", {
    email,
    password,
  });

  return data;
}

// Obtener usuario autenticado
export async function getMe() {
  const { data } = await api.get("/api/Auth/me");
  return data;
}

// Conservamos la función que ya teníamos para futuras consultas
export async function apiGet(endpoint) {
  const { data } = await api.get(endpoint);
  return data;
}

export default api;