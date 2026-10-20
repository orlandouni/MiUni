import test from "node:test";
import assert from "node:assert/strict";
import { streamChat } from "./chatStream.js";

test("envía JWT y sesión al endpoint stream y entrega texto antes del fin", async () => {
  let controller;
  const stream = new ReadableStream({ start(value) { controller = value; } });
  const pieces = [];
  let seenSession;
  let settled = false;
  let firstDelivered;
  const first = new Promise(resolve => { firstDelivered = resolve; });
  const job = streamChat({
    baseURL: "http://localhost:5124/",
    token: "test-only-token", mensaje: "Pregunta", sesionId: "existing",
    onSession: id => { seenSession = id; },
    onText: (part, full) => { pieces.push([part, full]); firstDelivered(); },
    fetchImpl: async (url, options) => {
      assert.equal(url, "http://localhost:5124/api/Chat/stream");
      assert.equal(options.headers.Authorization, "Bearer test-only-token");
      assert.deepEqual(JSON.parse(options.body), { sesionId: "existing", mensaje: "Pregunta" });
      return new Response(stream, { headers: { "X-Session-Id": "existing" } });
    },
  }).then(result => { settled = true; return result; });
  const bytes = new TextEncoder().encode("Sí 🌎");
  controller.enqueue(bytes.slice(0, 2)); // Split the accented character.
  await first;
  assert.equal(settled, false);
  controller.enqueue(bytes.slice(2, 5)); // Split the emoji.
  controller.enqueue(bytes.slice(5));
  controller.close();
  const result = await job;
  assert.equal(seenSession, "existing");
  assert.equal(result.answer, "Sí 🌎");
  assert.equal(pieces.at(-1)[1], result.answer);
  assert.ok(result.metrics.textChunks >= 2);
  assert.ok(result.metrics.totalMs >= result.metrics.firstTextMs);
});

test("propaga errores HTTP sin mostrarlos como texto del asistente", async () => {
  let delivered = false;
  await assert.rejects(streamChat({
    mensaje: "Prueba", onText: () => { delivered = true; },
    fetchImpl: async () => Response.json({ message: "Sesión vencida" }, { status: 401 }),
  }), error => error.status === 401 && error.message === "Sesión vencida");
  assert.equal(delivered, false);
});

test("detecta conexión interrumpida después de recibir una parte", async () => {
  let pulls = 0;
  let partial;
  const body = new ReadableStream({
    pull(controller) {
      if (pulls++ === 0) controller.enqueue(new TextEncoder().encode("Parcial"));
      else controller.error(new Error("Conexión interrumpida"));
    },
  });
  await assert.rejects(streamChat({
    mensaje: "Prueba", onText: (_, full) => { partial = full; },
    fetchImpl: async () => new Response(body, { headers: { "X-Session-Id": "test" } }),
  }), /Conexión interrumpida/);
  assert.equal(partial, "Parcial");
});

test("rechaza un stream vacío", async () => {
  await assert.rejects(streamChat({
    mensaje: "Prueba",
    fetchImpl: async () => new Response("", { headers: { "X-Session-Id": "test" } }),
  }), /vacía/);
});
