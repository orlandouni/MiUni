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
    assert.match(html, /href="\/mapa\?lugar=biblioteca-id"/);
    assert.match(html, /href="\/mapa\?lugar=rectoria-id"/);
    assert.match(html, /Ver Biblioteca Central en el mapa/);
    assert.equal(render("Hola, ¿cómo puedo ayudarte?"), "");
    assert.equal(render(""), "");
    assert.match(render("Visita [Biblioteca](https://unison.mx/fuente) Central."), /href="\/mapa\?lugar=biblioteca-id"/);
    assert.equal(render("Consulta [fuente](https://unison.mx/biblioteca-central)."), "");
    const { default: ChatAnswer } = await server.ssrLoadModule("/src/components/ChatAnswer.jsx");
    for (const texto of [
      "La **Biblioteca Central** está en el campus. [Ver Biblioteca Central](/mapa?lugar=biblioteca-id). También está Rectoría.",
      "La **Biblioteca Central** está en el campus. [Ver mapa][mapa]. También está Rectoría.\n\n[mapa]: /mapa?lugar=biblioteca-id",
    ]) {
      const respuesta = renderToStaticMarkup(createElement(MemoryRouter, null,
        createElement(ChatAnswer, { content: texto },
          createElement(LugaresDelMensaje, { texto, lugares }))));
      assert.equal((respuesta.match(/href="\/mapa\?lugar=biblioteca-id"/g) ?? []).length, 1);
      assert.equal((respuesta.match(/href="\/mapa\?lugar=rectoria-id"/g) ?? []).length, 1);
    }

  } finally { await server.close(); }
});
