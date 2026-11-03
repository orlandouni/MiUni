from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, HRFlowable, KeepTogether
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfbase import pdfmetrics
from reportlab.lib.colors import HexColor

OUT = 'output/pdf/Orlando_Bojorquez_CV_Tester.pdf'
NAVY = HexColor('#183B56')
BLUE = HexColor('#247BA0')
INK = HexColor('#263746')
MUTED = HexColor('#526579')
PALE = HexColor('#D7E4EC')

# Embedded font with broad Latin support for Spanish accents.
font_dir = r'C:\Windows\Fonts'
pdfmetrics.registerFont(TTFont('Aptos', font_dir + r'\arial.ttf'))
pdfmetrics.registerFont(TTFont('Aptos-Bold', font_dir + r'\arialbd.ttf'))

class CVDocTemplate(SimpleDocTemplate):
    pass

def draw_page(canvas, doc):
    canvas.saveState()
    w, h = letter
    canvas.setFillColor(NAVY)
    canvas.rect(0, h - 10, w, 10, stroke=0, fill=1)
    canvas.setStrokeColor(PALE)
    canvas.setLineWidth(0.55)
    canvas.line(doc.leftMargin, 27, w - doc.rightMargin, 27)
    canvas.setFont('Aptos', 7.2)
    canvas.setFillColor(MUTED)
    canvas.drawString(doc.leftMargin, 16, 'ORLANDO BOJORQUEZ PRECIADO  |  QA / TESTING')
    canvas.drawRightString(w - doc.rightMargin, 16, 'Hermosillo, Sonora')
    canvas.restoreState()

styles = getSampleStyleSheet()
styles.add(ParagraphStyle(name='Name', fontName='Aptos-Bold', fontSize=23, leading=25, textColor=NAVY, spaceAfter=2))
styles.add(ParagraphStyle(name='Target', fontName='Aptos-Bold', fontSize=10.2, leading=12.2, textColor=BLUE, spaceAfter=5))
styles.add(ParagraphStyle(name='Contact', fontName='Aptos', fontSize=8.1, leading=10.2, textColor=MUTED, spaceAfter=2))
styles.add(ParagraphStyle(name='Section', fontName='Aptos-Bold', fontSize=10.3, leading=12, textColor=NAVY, spaceBefore=6.2, spaceAfter=3, keepWithNext=True))
styles.add(ParagraphStyle(name='Body', fontName='Aptos', fontSize=8.65, leading=11.2, textColor=INK, spaceAfter=2.2))
styles.add(ParagraphStyle(name='BulletCV', fontName='Aptos', fontSize=8.35, leading=10.5, textColor=INK, leftIndent=10, firstLineIndent=-7, bulletIndent=0, spaceAfter=1.2))
styles.add(ParagraphStyle(name='Role', fontName='Aptos-Bold', fontSize=9.15, leading=11.1, textColor=INK, spaceBefore=2, spaceAfter=1, keepWithNext=True))
styles.add(ParagraphStyle(name='Project', fontName='Aptos-Bold', fontSize=8.8, leading=10.5, textColor=INK, spaceBefore=1.5, spaceAfter=0.3, keepWithNext=True))
styles.add(ParagraphStyle(name='Compact', fontName='Aptos', fontSize=8.25, leading=10.3, textColor=INK, spaceAfter=1.4))

story=[]
def p(text, style='Body'):
    story.append(Paragraph(text, styles[style]))
def section(text):
    story.append(Paragraph(text.upper(), styles['Section']))
    story.append(HRFlowable(width='100%', thickness=0.55, color=PALE, spaceBefore=0, spaceAfter=3))
def bullet(text):
    story.append(Paragraph('• ' + text, styles['BulletCV']))
def project(title, text):
    story.append(KeepTogether([Paragraph(title, styles['Project']), Paragraph(text, styles['Compact'])]))

p('Orlando Bojorquez Preciado', 'Name')
p('ESTUDIANTE DE INGENIERÍA EN SISTEMAS  |  PRACTICANTE DE QA / TESTING', 'Target')
p('Hermosillo, Sonora  ·  662 194 4414  ·  <link href="mailto:orlandito.bjz@gmail.com" color="#247BA0">orlandito.bjz@gmail.com</link>', 'Contact')
p('<link href="https://github.com/orlandouni" color="#247BA0">GitHub: github.com/orlandouni</link>  ·  <link href="https://www.linkedin.com/in/orlando-boj%C3%B3rquez-preciado-299b06362/" color="#247BA0">LinkedIn: linkedin.com/in/orlando-bojórquez-preciado-299b06362</link>', 'Contact')

section('Perfil')
p('Estudiante de séptimo semestre de Ingeniería en Sistemas de Información en la Universidad de Sonora, con promedio de 95.93/100 y experiencia en desarrollo web, diagnóstico de incidencias y atención a usuarios. Busco iniciar mi trayectoria en QA/testing y aportar pensamiento analítico, cuidado al detalle, conocimientos de SQL y una perspectiva centrada en la experiencia del usuario.')

section('Fortalezas relevantes para QA / testing')
bullet('<b>Formación en testing:</b> curso Tester de Capacítate para el Empleo en curso (98% de avance; calificación reportada: 9.25).')
bullet('<b>Diagnóstico y seguimiento:</b> apoyo en la identificación de problemas técnicos y seguimiento de casos de clientes; colaboración con equipos de desarrollo.')
bullet('<b>Validación de información:</b> conocimientos de SQL y bases de datos relacionales; experiencia con sistemas internos y paneles de administración.')
bullet('<b>Enfoque de usuario:</b> atención a clientes internacionales en inglés y participación en mejoras de interfaz y funciones de sitios en producción.')

section('Experiencia')
p('Desarrollador web junior · Simetria (remoto)  <font color="#526579">|  2025 – oct. 2026</font>', 'Role')
bullet('Colaboré en el desarrollo y mantenimiento de Cinemall.mx, sitio y aplicación de salas de cine, con C# y ASP.NET Core MVC.')
bullet('Implementé redirección mediante códigos QR, páginas legales, sección de preventa y mejoras de interfaz; apoyé en el diagnóstico de incidencias.')
bullet('Construí y desplegué simetria-agencia.com con Next.js, TypeScript y AWS; gestioné contenido en paneles administrativos y coordiné cambios con el equipo.')
p('Agente de ventas y atención al cliente · RH Solutions  <font color="#526579">|  2023</font>', 'Role')
bullet('Atendí a clientes internacionales en inglés, di seguimiento a sus casos y registré información en sistemas internos, incluso en periodos de alta demanda.')

section('Proyectos')
project('MiUni · Plataforma web universitaria', 'Colaboré en una plataforma con mapa interactivo del campus, búsqueda de lugares, cálculo de rutas y asistente conversacional. React, .NET y PostgreSQL; integración con un servicio RAG en Python.')
project('Buho Go · Propuesta académica', 'Participé en la propuesta y planeación de una plataforma de pedidos para comercios cercanos a la UNISON, con catálogo, pedidos anticipados, estimación de preparación y aviso de pedido listo. Propuesta académica en fase de definición y planeación.')
project('Sistema de punto de venta', 'Aplicación de escritorio en C# y Windows Forms, con arquitectura por capas e impresión de tickets mediante GDI+.')
p('<b>Logro:</b> finalista Top 10 en Smart City Hackathon.', 'Compact')

section('Formación académica')
p('<b>Ingeniería en Sistemas de Información</b> · Universidad de Sonora  <font color="#526579">|  2023 – actual</font><br/>Séptimo semestre · Promedio: 95.93/100', 'Compact')

section('Conocimientos')
p('<b>QA y datos:</b> fundamentos de testing (en formación), SQL, SQL Server, PostgreSQL, diagnóstico de incidencias.<br/><b>Desarrollo:</b> C#, .NET, ASP.NET Core MVC, React, JavaScript, TypeScript, Python, HTML/CSS.<br/><b>Herramientas:</b> Git/GitHub, AWS, Windows.', 'Compact')

section('Cursos e idiomas')
p('<b>Tester</b> · Capacítate para el Empleo (98% de avance; calificación reportada: 9.25)  ·  <b>SQL Básico e Intermedio</b> · HackerRank (Intermedio, sep. 2026)<br/><b>Español:</b> nativo  ·  <b>Inglés:</b> avanzado', 'Compact')

doc=CVDocTemplate(OUT, pagesize=letter, rightMargin=0.62*inch, leftMargin=0.62*inch, topMargin=0.35*inch, bottomMargin=0.46*inch, title='CV - Orlando Bojorquez Preciado | QA / Testing', author='Orlando Bojorquez Preciado')
doc.build(story, onFirstPage=draw_page, onLaterPages=draw_page)
print(OUT)




