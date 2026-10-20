// Read plain-text streaming responses without buffering the complete answer.
export async function streamChat({
  baseURL = "", token, mensaje, sesionId = null, signal,
  onSession = () => {}, onText = () => {},
  fetchImpl = globalThis.fetch, now = () => performance.now(),
}) {
  const started = now();
  const response = await fetchImpl(`${baseURL.replace(/\/+$/, "")}/api/Chat/stream`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ sesionId, mensaje }),
    signal,
  });
  const headersMs = now() - started;
  const sessionId = response.headers.get("X-Session-Id");
  if (sessionId) onSession(sessionId);
  if (!response.ok) {
    let message = "El asistente no está disponible en este momento.";
    try {
      const body = await response.json();
      message = body.message || body.detail || message;
    } catch { /* Keep a useful message for non-JSON errors. */ }
    const error = new Error(message);
    error.status = response.status;
    throw error;
  }
  if (!response.body) throw new Error("El navegador no pudo recibir la respuesta en streaming.");
  if (!sessionId) throw new Error("La respuesta no incluye el identificador de conversación.");
  const reader = response.body.getReader();
  const decoder = new TextDecoder("utf-8", { fatal: true });
  let answer = "";
  let firstTextMs = null;
  let textChunks = 0;
  let ended = false;
  const append = text => {
    if (!text) return;
    if (firstTextMs === null) firstTextMs = now() - started;
    textChunks += 1;
    answer += text;
    onText(text, answer);
  };
  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) { ended = true; break; }
      append(decoder.decode(value, { stream: true }));
    }
    append(decoder.decode());
  } finally {
    if (!ended) await reader.cancel().catch(() => {});
    reader.releaseLock();
  }
  if (!answer.trim()) throw new Error("El asistente devolvió una respuesta vacía.");
  return {
    sessionId, answer,
    metrics: { headersMs, firstTextMs, totalMs: now() - started, textChunks, characters: answer.length },
  };
}
