# Copia animada del archivo de Meshy

Origen: `C:\Users\orlan\Downloads\Meshy_AI_Character_output.glb`. El original no se modifica.

El original incluye SmartRigArmature con 15 articulaciones y pesos de deformación, pero no incluye clips de animación ni expresiones faciales (morph targets).

`miuni-meshy-animado.glb` conserva geometría, esqueleto, materiales y texturas; añade cuatro clips: Reposo, Saludo, Asentir y Negar. El saludo tiene un recorrido amplio y una pausa antes de repetirse. Asentir conserva el movimiento aprobado. No incluye parpadeos ni expresiones faciales.

Vista previa: `meshy.html`, servida desde la misma carpeta que el resto del visor. Los botones reproducen las animaciones guardadas en el GLB.

Integración: el inicio repite Saludo; el chatbot permanece en Reposo mientras consulta, ejecuta Asentir ante una respuesta o Negar ante un error o una frase explícita de falta de información, y vuelve a Reposo al terminar. El backend actual solo entrega texto; la detección de falta de información utiliza frases, no un estado estructurado. Las preferencias de movimiento reducido se respetan y se pausa el renderizado fuera de pantalla.

Regenerar: `python animate-meshy.py`.
