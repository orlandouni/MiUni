import test from "node:test";
import assert from "node:assert/strict";
import { agruparPorMateria, resumenSesiones, aSolicitud, esEnlacePerfilBuhoRater } from "./horarioUtils.js";

const clase = (materia, profesor, dia, inicio, fin, salon = "5G-A303") => ({
  materia, profesor, dia, horaInicio: `${inicio}:00`, horaFin: `${fin}:00`, salon,
  buhoRaterUrl: profesor ? `https://example.com/?q=${profesor}` : null,
});

test("agrupa las clases por materia y profesor, ordenadas por hora", () => {
  const grupos = agruparPorMateria([
    clase("Bases de datos", "Ana López", 4, "10:00", "12:00"),
    clase("Cultura", null, 5, "07:00", "08:00"),
    clase("Bases de datos", "ana lópez", 2, "10:00", "12:00", "5G-A302"),
  ]);
  assert.deepEqual(grupos.map((g) => g.materia), ["Cultura", "Bases de datos"]);
  assert.deepEqual(grupos[1].sesiones.map((s) => s.dia), [2, 4]);
  assert.deepEqual(grupos[1].salones, ["5G-A302", "5G-A303"]);
  assert.equal(grupos[0].buhoRaterUrl, null);
});

test("resume los días que comparten horario", () => {
  const texto = resumenSesiones([
    clase("X", null, 1, "07:00", "08:00"),
    clase("X", null, 3, "07:00", "08:00"),
    clase("X", null, 5, "09:00", "10:00"),
  ]);
  assert.equal(texto, "Lun, Mié 07:00–08:00 · Vie 09:00–10:00");
});

test("arma la solicitud para la API sin segundos ni textos vacíos", () => {
  assert.deepEqual(aSolicitud({ ...clase("X", "", "2", "07:00", "08:00"), salon: "" }), {
    materia: "X", profesor: null, dia: 2, horaInicio: "07:00", horaFin: "08:00", salon: null,
  });
});

test("reconoce solo enlaces de perfil de BuhoRater", () => {
  const id = "0136eb39-3a7c-4b75-af52-92ff2283d434";
  assert.ok(esEnlacePerfilBuhoRater(`https://www.buhorater.com/perfil?id=${id}`));
  assert.ok(esEnlacePerfilBuhoRater(` buhorater.com/perfil?id=${id} `));
  assert.ok(esEnlacePerfilBuhoRater(id));
  assert.ok(!esEnlacePerfilBuhoRater(`https://buhorater.com.mx/perfil?id=${id}`));
  assert.ok(!esEnlacePerfilBuhoRater("https://www.buhorater.com/"));
  assert.ok(!esEnlacePerfilBuhoRater(""));
});
