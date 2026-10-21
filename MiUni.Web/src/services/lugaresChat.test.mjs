import { test } from "node:test";
import assert from "node:assert/strict";
import { detectarLugares, enlaceLugar, ubicacionValida, cargarLugaresChat, lugaresEnlazados } from "./lugaresChat.js";

const biblioteca = { id: "biblioteca", nombre: "Biblioteca Central", latitud: 29.08, longitud: -110.96 };
const direccion = { id: "direccion", nombre: "Dirección de Servicios Estudiantiles", latitud: 29.09, longitud: -110.97 };
const edificio = { id: "edificio", nombre: "5K", latitud: 29.07, longitud: -110.95 };
const catalogo = [biblioteca, direccion, edificio];

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

test("detecta el texto visible de citas, referencias y tablas sin buscar dentro de URLs", () => {
  for (const texto of [
    "Visita [Biblioteca](https://unison.mx/fuente) Central.",
    "Visita [Biblioteca][fuente] Central.\n\n[fuente]: https://unison.mx/fuente",
    "| Lugar |\n| --- |\n| Biblioteca **Central** |",
    "Biblioteca &nbsp; Central",
  ]) assert.deepEqual(detectarLugares(texto, catalogo), [biblioteca]);
  for (const texto of [
    "Consulta [la fuente](https://unison.mx/biblioteca-central).",
    "https://unison.mx/biblioteca-central",
    "Consulta [la fuente][ref].\n\n[ref]: https://unison.mx/biblioteca-central",
    '<!-- Biblioteca Central -->',
    "![Biblioteca Central](https://unison.mx/imagen.png)",
    "Biblioteca\n\n# Central",
  ]) assert.deepEqual(detectarLugares(texto, catalogo), []);
});

test("resuelve códigos del catálogo real sin confundir números ni carreras", () => {
  const escolar = { ...direccion, nombre: "8A - Dirección de servicios escolares" };
  const industrial = { ...edificio, nombre: "Edificio 5K - Ingeniería Industrial" };
  const contabilidad = { ...edificio, id: "contabilidad", nombre: "Edificio 9Q2 Contabilidad y administración" };
  const lugares = [escolar, industrial, contabilidad];
  assert.deepEqual(detectarLugares("Ubicación: **Edificio 8A**, planta alta.", lugares), [escolar]);
  assert.deepEqual(detectarLugares("Dirección de servicios escolares", lugares), [escolar]);
  assert.deepEqual(detectarLugares("Ve al 5K o al 9Q2.", lugares), [industrial, contabilidad]);
  assert.deepEqual(detectarLugares("18A, 5K2, 9Q21 e Ingeniería Industrial.", lugares), []);
});

test("no resuelve un alias ambiguo ni permite que el formato junte nombres de bloques distintos", () => {
  const a = { ...edificio, id: "a", nombre: "Edificio 5K - Ingeniería Industrial" };
  const b = { ...edificio, id: "b", nombre: "Edificio 5K - Otro departamento" };
  assert.deepEqual(detectarLugares("Ve al 5K.", [a, b]), []);
  assert.deepEqual(detectarLugares(a.nombre, [a, b]), [a]);
  assert.deepEqual(detectarLugares("Biblioteca\n\nCentral", [biblioteca]), []);
});

test("reconoce enlaces al mapa existentes, incluidos los de referencia y la ruta anterior", () => {
  const texto = "[Ver biblioteca](/mapa?lugar=BIBLIOTECA) y [dirección][mapa].\n\n[mapa]: /?lugar=direccion";
  assert.deepEqual([...lugaresEnlazados(texto)], ["biblioteca", "direccion"]);
});

test("fuentes externas, imágenes y código no sustituyen un enlace al mapa", () => {
  const texto = "[Fuente](https://unison.mx/mapa?lugar=biblioteca) " +
    "![Imagen](/mapa?lugar=biblioteca) `[/mapa](/mapa?lugar=biblioteca)` " +
    "[Otro](/lugares?lugar=biblioteca)";
  assert.deepEqual([...lugaresEnlazados(texto)], []);
});
