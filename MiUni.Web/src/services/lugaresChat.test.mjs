import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { detectarLugares, enlaceLugar, ubicacionValida, cargarLugaresChat } from "./lugaresChat.js";

const biblioteca = { id: "biblioteca", nombre: "Biblioteca Central", latitud: 29.08, longitud: -110.96 };
const direccion = { id: "direccion", nombre: "Dirección de Servicios Estudiantiles", latitud: 29.09, longitud: -110.97 };
const edificio = { id: "edificio", nombre: "5K", latitud: 29.07, longitud: -110.95 };
const catalogo = [biblioteca, direccion, edificio];

test("genera enlaces para cada uno de los 51 lugares del catálogo inicial", () => {
  const sql = readFileSync(new URL("../../../seed_lugares.sql", import.meta.url), "utf8");
  const nombres = [...sql.matchAll(/\('([^']+)',\s*'[^']+',\s*29\./g)].map(match => match[1]);
  assert.equal(nombres.length, 51);
  const lugares = nombres.map((nombre, index) => ({ ...biblioteca, id: String(index), nombre }));
  for (const lugar of lugares) {
    const encontrados = detectarLugares(`Puedes ir a **${lugar.nombre}**.`, lugares);
    assert.ok(encontrados.some(encontrado => encontrado.id === lugar.id), lugar.nombre);
    assert.equal(new URL(enlaceLugar(lugar.id), "https://miuni.test").searchParams.get("lugar"), lugar.id);
  }
});

test("reconoce varios nombres con acentos, mayúsculas y formato de chat", () => {
  assert.deepEqual(detectarLugares("Ve a **BIBLIOTECA CENTRAL**, luego a direccion de servicios estudiantiles.", catalogo), [biblioteca, direccion]);
});
test("no inventa lugares ni confunde fragmentos o códigos de edificios", () => {
  assert.deepEqual(detectarLugares("Hay una biblioteca y un edificio 15K; ve al 5K2.", catalogo), []);
  assert.deepEqual(detectarLugares("Ve al (5K).", catalogo), [edificio]);
  assert.deepEqual(detectarLugares("Biblioteca Centralizada", catalogo), []);
});
test("evita duplicados y admite mensajes vacíos", () => {
  assert.deepEqual(detectarLugares("Biblioteca Central. Biblioteca Central.", [...catalogo, biblioteca]), [biblioteca]);
  assert.deepEqual(detectarLugares(null, catalogo), []);
});
test("rechaza coordenadas ausentes o inválidas", () => {
  for (const latitud of [null, undefined, "29", NaN, 91]) {
    assert.equal(ubicacionValida({ ...biblioteca, latitud }), false);
    assert.deepEqual(detectarLugares("Biblioteca Central", [{ ...biblioteca, latitud }]), []);
  }
  assert.equal(ubicacionValida({ ...biblioteca, longitud: -181 }), false);
  assert.equal(ubicacionValida({ latitud: 0, longitud: 0 }), true);
});
test("el enlace transporta únicamente el identificador y lo codifica", () => {
  const url = new URL(enlaceLugar("id & otro"), "https://miuni.test");
  assert.equal(url.pathname, "/mapa");
  assert.equal(url.searchParams.get("lugar"), "id & otro");
  assert.equal([...url.searchParams].length, 1);
});
test("comparte consultas concurrentes y permite reintentar tras un error", async () => {
  const original = globalThis.fetch;
  let llamadas = 0;
  globalThis.fetch = async () => { llamadas++; return { ok: false }; };
  try {
    const first = cargarLugaresChat();
    const second = cargarLugaresChat();
    assert.equal(first, second);
    await assert.rejects(first, /consultar/);
    assert.equal(llamadas, 1);
    globalThis.fetch = async () => ({ ok: true, json: async () => catalogo });
    assert.deepEqual(await cargarLugaresChat(), catalogo);
    globalThis.fetch = async () => ({ ok: true, json: async () => ({}) });
    await assert.rejects(cargarLugaresChat(), /válido/);
  } finally { globalThis.fetch = original; }
});
