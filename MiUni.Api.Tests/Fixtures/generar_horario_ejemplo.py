"""Genera horario-ejemplo.pdf: copia del acomodo del "Horario de reinscripción" del
portal de alumnos de la UNISON, con datos inventados. Uso: python3 generar_horario_ejemplo.py"""
from reportlab.lib.pagesizes import landscape, letter
from reportlab.pdfgen import canvas

c = canvas.Canvas("horario-ejemplo.pdf", pagesize=landscape(letter))
c.setFont("Helvetica-Bold", 10)
c.drawString(150, 560, "Universidad de Sonora")
c.drawString(150, 548, "HORARIO DE REINSCRIPCIÓN, CICLO 2026-2")
c.setFont("Helvetica", 7)
c.drawString(17, 480, "Expediente: 000000000    Nombre: ALUMNO DE PRUEBA    Correo: prueba@example.com")
c.drawString(17, 470, "Promedio Kardex: 90.0000    Créditos Aprobados: 100 de 393")

# Encabezados centrados sobre su columna, como en el PDF real.
y = 437
c.setFont("Helvetica-Bold", 7)
for texto, centro in [("Prog.", 27.5), ("Clave", 54), ("Materia", 151), ("Gpo.", 248), ("Part.", 274),
                      ("Lun", 311), ("Mar", 358.6), ("Mié", 406.5), ("Jue", 454), ("Vie", 502),
                      ("Sáb", 550), ("Maestro", 657), ("Aula", 763)]:
    c.drawCentredString(centro, y, texto)

c.setFont("Helvetica", 7)
dias_x = [292.2, 340.0, 387.8, 435.6, 483.4, 531.2]
def fila(y, prog=None, clave=None, materia=None, gpo=None, part=None, dias=None, maestro=None, aula=None):
    for texto, x in [(prog, 23.3), (clave, 44.3), (materia, 67.5), (gpo, 245.9), (part, 269.8),
                     (maestro, 573.8), (aula, 748.8)]:
        if texto: c.drawString(x, y, texto)
    for texto, x in zip(dias or [], dias_x):
        if texto == "-": c.drawCentredString(x + 18.6, y, "-")
        elif texto: c.drawString(x, y, texto)

h = "08:00-09:00"
fila(427.4, "ISI", "04134", "PROGRAMACIÓN ORIENTADA A OBJETOS II", "1", "T1", [h, h, h, h, "-", "-"], "PÉREZ GARCÍA JUAN CARLOS", "5G-A303")
fila(417.9, "ISI", "04136", "BASES DE DATOS", "1", "T1", ["-", "10:00-12:00", "-", "10:00-12:00", "-", "-"], "LÓPEZ RUIZ ANA", "5G-A302")
# Materia de dos líneas con las horas a media fila.
fila(408.4, "ISI", "04138", "PROPIEDAD INTELECTUAL Y EJERCICIO", "1")
fila(404.4, part="T1", dias=["13:00-14:00", "-", "13:00-14:00", "-", "-", "-"], maestro="MORALES SOTO LUCÍA", aula="5G-A304")
fila(400.3, materia="PROFESIONAL")
fila(390.8, "ISI", "06906", "CULTURA EMPRENDEDORA", "2", "T1", ["-", "-", "-", "-", "07:00-08:00", "-"], None, "5G-A303")

c.setFont("Helvetica-Bold", 8)
c.drawString(14.5, 331.9, "NOTA(S)")
c.setFont("Helvetica", 7)
c.drawString(14.5, 320, "• El nivel de inglés que tienes registrado es 5 y el programa requiere un nivel de inglés 5 como mínimo para la titulación")
c.save()
