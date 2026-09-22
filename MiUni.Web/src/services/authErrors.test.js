import test from "node:test";
import assert from "node:assert/strict";
import { authErrorMessage } from "./authErrors.js";

test("explains a missing API route instead of an unexpected error", () => {
  assert.match(authErrorMessage({ response: { status: 404 } }), /servicio de registro/);
});
test("distinguishes unavailable backend and network failure", () => {
  assert.match(authErrorMessage({ response: { status: 500, data: "private stack trace" } }), /servicio no está disponible/);
  assert.match(authErrorMessage({ request: {} }), /conectar/);
});
test("shows field validation errors returned by ASP.NET", () => {
  assert.equal(authErrorMessage({ response: { status: 400, data: { errors: { Nombre: ["El nombre es obligatorio."], Password: ["Mínimo 8 caracteres."] } } } }), "El nombre es obligatorio. Mínimo 8 caracteres.");
});
test("translates Identity errors and preserves unknown descriptions", () => {
  assert.equal(authErrorMessage({ response: { status: 400, data: [{ code: "PasswordRequiresDigit", description: "English" }, { description: "Otro error." }] } }), "Incluye un número en la contraseña. Otro error.");
});
test("preserves API business validation messages", () => {
  assert.equal(authErrorMessage({ response: { status: 400, data: { message: "Usa tu correo institucional." } } }), "Usa tu correo institucional.");
});
