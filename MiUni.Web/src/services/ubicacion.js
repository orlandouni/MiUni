const CLAVE_SOLICITADA = "miuni.ubicacion.solicitada";

export function recordarSolicitud(entorno = globalThis) {
  try { entorno.localStorage.setItem(CLAVE_SOLICITADA, "1"); } catch { /* Almacenamiento opcional. */ }
}

// Guarda sólo si ya mostramos la solicitud, nunca un permiso ni coordenadas.
export function iniciarUbicacionAutomatica(iniciar, entorno = globalThis) {
  let cancelada = false;
  let detener;
  const timer = entorno.setTimeout(async () => {
    let permiso;
    try { permiso = (await entorno.navigator.permissions.query({ name: "geolocation" })).state; } catch { /* Navegadores sin Permissions API. */ }
    if (cancelada || permiso === "denied") return;
    let solicitada = false;
    try { solicitada = entorno.localStorage.getItem(CLAVE_SOLICITADA) === "1"; } catch { /* Almacenamiento opcional. */ }
    if (permiso !== "granted" && solicitada) return;
    recordarSolicitud(entorno);
    detener = iniciar();
  }, 0);
  return () => { cancelada = true; entorno.clearTimeout(timer); detener?.(); };
}

// El navegador exige su permiso antes de entregar posiciones.
export function solicitarUbicacion(onSuccess, onError, entorno = globalThis) {
  let activa = true;
  let watchId;
  const cancelar = () => {
    activa = false;
    if (watchId !== undefined) {
      entorno.navigator.geolocation.clearWatch(watchId);
      watchId = undefined;
    }
  };
  const fallar = mensaje => { if (activa) { cancelar(); onError(mensaje); } };
  if (!entorno.isSecureContext) {
    fallar("Para usar tu ubicación abre la aplicación con HTTPS o en localhost. Puedes elegir el origen en el mapa.");
    return cancelar;
  }
  if (!entorno.navigator?.geolocation) {
    fallar("Este navegador no ofrece ubicación. Elige el origen en el mapa.");
    return cancelar;
  }
  try {
    watchId = entorno.navigator.geolocation.watchPosition(({ coords }) => {
      if (!activa) return;
      const { latitude, longitude, accuracy } = coords;
      if (!Number.isFinite(latitude) || Math.abs(latitude) > 90 || !Number.isFinite(longitude) || Math.abs(longitude) > 180 || !Number.isFinite(accuracy) || accuracy < 0) {
        fallar("El dispositivo devolvió una ubicación inválida. Elige el origen en el mapa o vuelve a intentarlo.");
        return;
      }
      onSuccess({ latitud: latitude, longitud: longitude, precision: accuracy, nombre: "Mi ubicación", fuente: "gps" });
    }, error => {
      const mensajes = {
        1: "No se autorizó el acceso a tu ubicación. Puedes habilitarlo en los permisos del navegador o elegir el origen en el mapa.",
        2: "No se pudo determinar tu ubicación. Intenta de nuevo o elige el origen en el mapa.",
        3: "La ubicación tardó demasiado. Intenta de nuevo o elige el origen en el mapa.",
      };
      fallar(mensajes[error.code] || "No se pudo obtener tu ubicación. Elige el origen en el mapa.");
    }, { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 });
    // También limpia un error síncrono de proveedores o dobles de prueba.
    if (!activa) cancelar();
  } catch {
    fallar("El navegador bloqueó la ubicación. Elige el origen en el mapa.");
  }
  return cancelar;
}
