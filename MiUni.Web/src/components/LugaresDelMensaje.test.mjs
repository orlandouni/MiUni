import { test } from "node:test";
import assert from "node:assert/strict";
import { createServer } from "vite";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { MemoryRouter } from "react-router-dom";

test("renderiza enlaces reales al mapa para una respuesta y ninguno sin coincidencias", async () => {
  // Compile the real JSX without listening on a port or requiring the backend.
  const server = await createServer({ server: { middlewareMode: true, watch: null } });
  try {
    const { default: LugaresDelMensaje } = await server.ssrLoadModule("/src/components/LugaresDelMensaje.jsx");
    const lugares = [
      { id: "biblioteca-id", nombre: "Biblioteca Central", latitud: 29.08, longitud: -110.96 },
      { id: "rectoria-id", nombre: "Rectoría", latitud: 29.09, longitud: -110.97 },
    ];
    const render = texto => renderToStaticMarkup(createElement(MemoryRouter, null,
      createElement(LugaresDelMensaje, { texto, lugares })));
    const html = render("Visita la BIBLIOTECA CENTRAL o rectoria.");
    assert.match(html, /href="\/\?lugar=biblioteca-id"/);
    assert.match(html, /href="\/\?lugar=rectoria-id"/);
    assert.match(html, /Ver Biblioteca Central en el mapa/);
    assert.equal(render("Hola, ¿cómo puedo ayudarte?"), "");
    assert.equal(render(""), "");
  } finally { await server.close(); }
});
