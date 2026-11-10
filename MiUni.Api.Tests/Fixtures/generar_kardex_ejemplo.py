"""Genera kardex-ejemplo.pdf: un kárdex en tabla con datos inventados, con secciones por
ciclo, un nombre de materia en dos líneas, una materia acreditada sin número, una
reprobada, una baja y una en curso. Uso: python3 generar_kardex_ejemplo.py"""
from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas

c = canvas.Canvas("kardex-ejemplo.pdf", pagesize=letter)
c.setFont("Helvetica-Bold", 10)
c.drawString(150, 750, "Universidad de Sonora")
c.drawString(150, 738, "KARDEX DEL ALUMNO")
c.setFont("Helvetica", 7)
c.drawString(30, 715, "Expediente: 000000000    Nombre: ALUMNO DE PRUEBA    Programa: INGENIERIA EN SISTEMAS DE INFORMACION")
c.drawString(30, 705, "Promedio Kardex: 85.5000    Créditos Aprobados: 31 de 393")

def encabezado(y):
    c.setFont("Helvetica-Bold", 7)
    for texto, centro in [("Clave", 50), ("Materia", 160), ("Créditos", 300), ("Calif.", 350), ("Tipo", 400)]:
        c.drawCentredString(centro, y, texto)
    c.setFont("Helvetica", 7)

def fila(y, clave=None, materia=None, cred=None, calif=None, tipo=None):
    for texto, x in [(clave, 35), (materia, 75)]:
        if texto: c.drawString(x, y, texto)
    for texto, centro in [(cred, 300), (calif, 350), (tipo, 400)]:
        if texto: c.drawCentredString(centro, y, texto)

c.setFont("Helvetica-Bold", 8)
c.drawString(30, 670, "CICLO 2025-1")
encabezado(658)
fila(648, "04120", "CÁLCULO DIFERENCIAL E INTEGRAL I", "10", "92", "ORD")
fila(638, "04121", "ÁLGEBRA SUPERIOR", "8", "55", "ORD")
fila(628, "06900", "ESTRATEGIAS PARA APRENDER A", "4", "AC", "ORD")
fila(620, materia="APRENDER")

c.setFont("Helvetica-Bold", 8)
c.drawString(30, 590, "CICLO 2025-2")
encabezado(578)
fila(568, "04121", "ÁLGEBRA SUPERIOR", "8", "78", "EXT")
fila(558, "04130", "FÍSICA I", "8", "-", "BAJA")
fila(548, "04134", "PROGRAMACIÓN ORIENTADA A OBJETOS II", "8", None, "INSCRITA")

c.setFont("Helvetica-Bold", 8)
c.drawString(30, 500, "NOTA(S)")
c.setFont("Helvetica", 7)
c.drawString(30, 488, "• Este documento es informativo y no tiene validez oficial para trámites ante otras instituciones")
c.save()
