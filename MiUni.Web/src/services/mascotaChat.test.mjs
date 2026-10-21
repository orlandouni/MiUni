import {test} from 'node:test';
import assert from 'node:assert/strict';
import {gestoRespuesta} from './mascotaChat.js';

test('niega cuando falta información o no puede contestar', () => {
  for (const text of ['No tengo información sobre ese edificio.', 'Lo siento, no puedo responder esa pregunta.', 'No cuento con datos suficientes para responder.', 'No sé la respuesta.', 'No pude encontrar información.', 'No encontré información sobre ese sitio.', 'No hay información disponible.', '', null]) {
    assert.equal(gestoRespuesta(text), 'Negar', String(text));
  }
});
test('asiente ante respuestas útiles aunque contengan una negación', () => {
  for (const text of ['La biblioteca está junto a Rectoría.', 'No abre los domingos; abre de lunes a viernes.', 'No necesitas una credencial para entrar.', 'No tengo clases ahí, pero la biblioteca abre a las 8.']) {
    assert.equal(gestoRespuesta(text), 'Asentir', text);
  }
});
