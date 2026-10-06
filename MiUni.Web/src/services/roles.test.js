import test from "node:test";
import assert from "node:assert/strict";
import { landingFor, canManageBusiness } from "./roles.js";

test("el login dirige según los roles, con prioridad al administrador", () => {
  assert.equal(landingFor(), "/");
  assert.equal(landingFor(["Estudiante"]), "/");
  assert.equal(landingFor(["Estudiante", "Propietario"]), "/negocios");
  assert.equal(landingFor(["Administrador", "Propietario"]), "/admin");
});

test("una cuenta normal no puede administrar negocios", () => {
  assert.equal(canManageBusiness(["Estudiante"]), false);
  assert.equal(canManageBusiness(), false);
  assert.equal(canManageBusiness(["Propietario"]), true);
  assert.equal(canManageBusiness(["Administrador"]), true);
});
