import test from "node:test";
import assert from "node:assert/strict";
import { iniciarUbicacionAutomatica, solicitarUbicacion } from "./ubicacion.js";

function dispositivo() {
  const callbacks = {};
  const entorno = { isSecureContext: true, navigator: { geolocation: {
    watchPosition(success, error, options) { Object.assign(callbacks, { success, error, options }); return 0; },
    clearWatch(id) { callbacks.cancelado = id; },
  } } };
  return { callbacks, entorno };
}

function permisos(state, previa = false) {
  let tarea;
  const datos = new Map(previa ? [["miuni.ubicacion.solicitada", "1"]] : []);
  return {
    entorno: {
      navigator: { permissions: { query: async () => ({ state }) } },
      localStorage: { getItem: key => datos.get(key), setItem: (key, value) => datos.set(key, value) },
      setTimeout: callback => { tarea = callback; return 1; },
      clearTimeout: () => {},
    },
    ejecutar: () => tarea(),
  };
}

test("solicita al entrar una vez y recuerda sólo el intento", async () => {
  const escenario = permisos("prompt");
  let inicios = 0;
  iniciarUbicacionAutomatica(() => { inicios++; }, escenario.entorno);
  await escenario.ejecutar();
  iniciarUbicacionAutomatica(() => { inicios++; }, escenario.entorno);
  await escenario.ejecutar();
  assert.equal(inicios, 1);
});

test("permiso concedido reactiva seguimiento, denegado no lo solicita", async () => {
  for (const state of ["granted", "denied"]) {
    const escenario = permisos(state, true);
    let inicios = 0;
    iniciarUbicacionAutomatica(() => { inicios++; }, escenario.entorno);
    await escenario.ejecutar();
    assert.equal(inicios, state === "granted" ? 1 : 0);
  }
});

test("salir antes de resolver permisos evita iniciar ubicación", async () => {
  const escenario = permisos("granted");
  const cancelar = iniciarUbicacionAutomatica(assert.fail, escenario.entorno);
  const pendiente = escenario.ejecutar();
  cancelar();
  await pendiente;
});

test("salir del mapa detiene el seguimiento automático", async () => {
  const escenario = permisos("granted");
  let detenido = false;
  const cancelar = iniciarUbicacionAutomatica(() => () => { detenido = true; }, escenario.entorno);
  await escenario.ejecutar();
  cancelar();
  assert.equal(detenido, true);
});

test("actualiza sucesivamente la posición con precisión", () => {
  const { callbacks, entorno } = dispositivo();
  let punto;
  solicitarUbicacion(valor => { punto = valor; }, assert.fail, entorno);
  callbacks.success({ coords: { latitude: 29.08, longitude: -110.96, accuracy: 20 } });
  assert.deepEqual(punto, { latitud: 29.08, longitud: -110.96, precision: 20, fuente: "gps", nombre: "Mi ubicación" });
  assert.deepEqual(callbacks.options, { enableHighAccuracy: true, maximumAge: 0, timeout: 15000 });
  callbacks.success({ coords: { latitude: 29.09, longitude: -110.97, accuracy: 10 } });
  assert.equal(punto.latitud, 29.09);
  assert.equal(punto.precision, 10);
});

test("cancelar descarta resultados y errores tardíos", () => {
  const { callbacks, entorno } = dispositivo();
  const cancelar = solicitarUbicacion(assert.fail, assert.fail, entorno);
  cancelar();
  assert.equal(callbacks.cancelado, 0);
  callbacks.success({ coords: { latitude: 29, longitude: -110, accuracy: 5 } });
  callbacks.error({ code: 1 });
});

for (const code of [1, 2, 3]) test(`error GPS ${code} ofrece selección manual`, () => {
  const { callbacks, entorno } = dispositivo();
  let mensaje;
  solicitarUbicacion(assert.fail, valor => { mensaje = valor; }, entorno);
  callbacks.error({ code });
  assert.equal(callbacks.cancelado, 0);
  assert.match(mensaje, /mapa/);
});

test("rechaza coordenadas inválidas", () => {
  const { callbacks, entorno } = dispositivo();
  let mensaje;
  solicitarUbicacion(assert.fail, valor => { mensaje = valor; }, entorno);
  callbacks.success({ coords: { latitude: NaN, longitude: 0, accuracy: 5 } });
  assert.match(mensaje, /inválida/);
});

test("sin HTTPS o sin soporte no solicita ubicación", () => {
  for (const entorno of [{ isSecureContext: false }, { isSecureContext: true, navigator: {} }]) {
    let mensaje;
    solicitarUbicacion(assert.fail, valor => { mensaje = valor; }, entorno);
    assert.match(mensaje, /mapa/);
  }
});
