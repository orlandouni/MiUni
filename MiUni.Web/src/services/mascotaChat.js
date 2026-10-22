// The current chat API returns plain text, without a structured answer status.
// Only explicit inability to answer triggers the negative gesture.
export function gestoRespuesta(answer) {
  const text = String(answer || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  if (!text.trim()) return 'Negar';
  if (/\bno (?:encontre|encontramos|se encontro) (?:informacion|datos|una respuesta)\b|\bno hay (?:suficiente )?informacion (?:disponible|sobre|acerca|para)\b/.test(text)) return 'Negar';
  const unavailable = /\bno (?:puedo|podemos) (?:responder|contestar|ayudar(?:te)? con (?:esa|esta|tu) (?:pregunta|consulta))\b|\bno (?:tengo|tenemos|dispongo de|cuento con) (?:suficiente )?(?:informacion|datos)(?: disponibles?| suficientes?| confiables?| fiables?| verificad[oa]s?)?(?: sobre| acerca| para| de| en|[.,;!?]|$)|\bno (?:se|conozco) (?:la respuesta|esa informacion)\b|\b(?:no pude|no se pudo) (?:encontrar|obtener) (?:informacion|una respuesta|respuesta)\b|\bfuera de (?:mi|nuestro) (?:alcance|ambito)\b/;
  return unavailable.test(text) ? 'Negar' : 'Asentir';
}
