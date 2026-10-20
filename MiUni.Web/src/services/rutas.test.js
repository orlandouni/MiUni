import test from "node:test";
import assert from "node:assert/strict";
import axios from "axios";
import { calcularRuta, posicionesRuta } from "./rutas.js";

test("convierte GeoJSON a Leaflet conservando orden y puntos intermedios", () => {
  assert.deepEqual(posicionesRuta({ type: "LineString", coordinates: [[-110.96, 29.08], [-110.97, 29.09]] }), [[29.08, -110.96], [29.09, -110.97]]);
  assert.deepEqual(posicionesRuta({ type: "Point", coordinates: [-110.96, 29.08] }), [[29.08, -110.96]]);
});

test("envía A/B y radio al endpoint y conserva la respuesta", async () => {
  const anterior = axios.defaults.adapter;
  const respuesta = { distanciaMetros: 120, geometria: { type: "LineString", coordinates: [[-110.96, 29.08], [-110.97, 29.09]] } };
  try {
    axios.defaults.adapter = async config => {
      assert.equal(config.url, "/api/Ruta");
      assert.deepEqual(JSON.parse(config.data), { a: { latitud: 29.08, longitud: -110.96 }, b: { latitud: 29.09, longitud: -110.97 }, radioConexionMetros: 100 });
      return { data: respuesta, status: 200, headers: {}, config };
    };
    assert.deepEqual(await calcularRuta({ latitud: 29.08, longitud: -110.96, nombre: "A" }, { latitud: 29.09, longitud: -110.97 }), respuesta);
  } finally { axios.defaults.adapter = anterior; }
});

test("distingue red sin conexión, puntos inválidos y API inaccesible", async () => {
  const anterior = axios.defaults.adapter;
  try {
    for (const [status, mensaje] of [[404, /No hay una ruta/], [400, /Selecciona de nuevo/], [500, /conexión con la API/]]) {
      axios.defaults.adapter = async () => { throw Object.assign(new Error("HTTP"), { response: { status } }); };
      await assert.rejects(calcularRuta({ latitud: 29, longitud: -110 }, { latitud: 29, longitud: -110 }), mensaje);
    }
    const controller = new AbortController();
    controller.abort();
    await assert.rejects(calcularRuta({}, {}, controller.signal), error => axios.isCancel(error));
  } finally { axios.defaults.adapter = anterior; }
});
