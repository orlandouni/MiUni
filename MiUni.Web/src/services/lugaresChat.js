const normalizar = texto => String(texto ?? "").normalize("NFD")
  .replace(/\p{M}/gu, "").toLocaleLowerCase("es")
  .replace(/[^\p{L}\p{N}]+/gu, " ").trim();

export function ubicacionValida(lugar) {
  return Number.isFinite(lugar?.latitud) && Math.abs(lugar.latitud) <= 90
    && Number.isFinite(lugar?.longitud) && Math.abs(lugar.longitud) <= 180;
}

export function detectarLugares(texto, catalogo = []) {
  const mensaje = ` ${normalizar(texto)} `;
  const vistos = new Set();
  return catalogo.filter(lugar => {
    const nombre = normalizar(lugar.nombre);
    if (!lugar.id || !nombre || !ubicacionValida(lugar) || vistos.has(lugar.id)) return false;
    if (!mensaje.includes(` ${nombre} `)) return false;
    vistos.add(lugar.id);
    return true;
  });
}

export function enlaceLugar(id) {
  return `/?${new URLSearchParams({ lugar: id })}`;
}

// Share an in-flight request between messages; failures remain retryable.
let pendiente;
export function cargarLugaresChat() {
  if (!pendiente) {
    pendiente = fetch("/api/Lugar")
      .then(response => {
        if (!response.ok) throw new Error("No se pudieron consultar los lugares.");
        return response.json();
      })
      .then(data => {
        if (!Array.isArray(data)) throw new Error("El catálogo de lugares no es válido.");
        return data;
      })
      .finally(() => { pendiente = undefined; });
  }
  return pendiente;
}
