"""Genera kardex-unison-ejemplo.pdf: copia del acomodo del "Kárdex electrónico" del portal
de alumnos de la UNISON (columnas CR CVE MATERIAS E1 E2 ORD REG CIC I R B, ciclo compacto
"2232" = 2023-2), con datos inventados. Uso: python3 generar_kardex_unison_ejemplo.py"""
from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas

c = canvas.Canvas("kardex-unison-ejemplo.pdf", pagesize=letter)
c.setFont("Helvetica-Bold", 9)
c.drawString(40, 720, "Universidad de Sonora")
c.drawString(40, 708, "KÁRDEX ELECTRÓNICO")
c.setFont("Helvetica", 7)
c.drawString(26, 640, "PROGRAMA: INGENIERÍA EN SISTEMAS DE INFORMACIÓN")
c.drawString(26, 614, "EXPEDIENTE: 000000000 ALUMNO DE PRUEBA")

y = 585
for texto, x in [("CR", 27.7), ("CVE", 50.3), ("------------------------------------", 107.0), ("MATERIAS", 205.1),
                 ("------------------------------------", 249.1), ("E1", 386.1), ("E2", 416.1), ("ORD", 444.6),
                 ("REG", 479.8), ("CIC", 516.6), ("I", 547.4), ("R", 560.6), ("B", 575.6)]:
    c.drawString(x, y, texto)
c.drawString(26, 573, "-" * 168)
c.drawString(26, 558, "ACREDITACIÓN DE INGLÉS: ACREDITADO 5.00 DE 5")

def fila(y, cr, cve, materia, e1, e2, ord_, reg, cic, i, r, b):
    for texto, x in [(cr, 29.1), (cve, 49.6), (materia, 76.0), (e1, 389.9), (e2, 418.3), (ord_, 446.8),
                     (reg, 482.0), (cic, 514.6), (i, 544.0), (r, 559.0), (b, 574.0)]:
        if texto: c.drawString(x, y, texto)

y = 544
fila(y, "03", "0119", "ACTIVIDADES CULTURALES Y DEPORTIVAS", "I", None, None, None, "2232", "00", "00", "00")
c.drawString(438.2, y + 1.9, "PAR. ACRED")  # en el PDF real queda un poco más arriba que su fila
filas = [
    ("08", "6880", "ÁLGEBRA", "I", "A", "098", None, "2232", "01", "00", "00"),
    ("08", "6881", "CÁLCULO DIFERENCIAL E INTEGRAL I", "I", "A", "040", "075", "2232", "01", "01", "00"),
    ("10", "6885", "FÍSICA I CON LABORATORIO", "I", "A", "052", None, "2241", "01", "01", "00"),
    ("07", "4114", "DESARROLLO DE SISTEMAS II", "I", "A", "100", None, "2241", "01", "00", "00"),
    ("06", "4121", "INGENIERÍA DE SISTEMAS DE INFORMACIÓN", "I", "2", None, None, "2251", "01", "00", "00"),
    ("09", "6883", "QUÍMICA I", "I", "14", None, None, "2251", "01", "00", "01"),
]
for datos in filas:
    y -= 13.6
    fila(y, *datos)
c.drawString(26, y - 12, "-" * 168)

c.drawString(45, y - 40, "PROMEDIO     PROMEDIO   -------------------CREDITOS------------------- -------------------MATERIAS-------------------")
c.drawString(57, y - 55, "2024-2      KARDEX      APR      REP      INS    APR   REP    NMR   INS")
c.drawString(58, y - 70, "*80.00       *80.00      26     **0      *15    *3    **0   **0    **2")
c.save()
