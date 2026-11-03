from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import StyleSheet1, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.lib import colors
from reportlab.lib.colors import HexColor
from reportlab.platypus import BaseDocTemplate, Frame, PageTemplate, Paragraph, Spacer, HRFlowable, KeepTogether, FrameBreak
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

OUT='output/pdf/Orlando_Bojorquez_CV_Tester.pdf'
W,H=letter
NAVY=HexColor('#344C61')
BLUE=HexColor('#68859A')
INK=HexColor('#344454')
MUTED=HexColor('#68798A')
SIDEBG=HexColor('#E8EDF1')
RULE=HexColor('#B7C5CF')
fd=r'C:\Windows\Fonts'
pdfmetrics.registerFont(TTFont('ArialCV',fd+r'\arial.ttf'))
pdfmetrics.registerFont(TTFont('ArialCV-Bold',fd+r'\arialbd.ttf'))

class CVDoc(BaseDocTemplate):
    pass

def page(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(NAVY); canvas.rect(0,H-8,W,8,stroke=0,fill=1)
    canvas.setFillColor(SIDEBG); canvas.rect(0,0,180,H-8,stroke=0,fill=1)
    canvas.setFillColor(NAVY); canvas.roundRect(71,H-68,40,40,6,stroke=0,fill=1)
    canvas.setStrokeColor(RULE); canvas.setLineWidth(.55)
    canvas.line(204,30,W-28,30)
    canvas.setFillColor(BLUE); canvas.setFont('ArialCV',6.8)
    canvas.drawString(204,18,'ORLANDO BOJORQUEZ PRECIADO  ·  SOFTWARE DEVELOPMENT')
    canvas.restoreState()

styles=StyleSheet1()
def add(name,**kw): styles.add(ParagraphStyle(name=name,**kw))
add('Initials',fontName='ArialCV-Bold',fontSize=16,leading=17,textColor=colors.white,alignment=1)
add('SideHead',fontName='ArialCV-Bold',fontSize=9.1,leading=11,textColor=BLUE,spaceBefore=10,spaceAfter=3,keepWithNext=True)
add('SideSub',fontName='ArialCV-Bold',fontSize=7.3,leading=8.5,textColor=NAVY,spaceBefore=4,spaceAfter=2,keepWithNext=True)
add('SideBody',fontName='ArialCV',fontSize=7.15,leading=9.1,textColor=INK,spaceAfter=2)
add('SideCert',fontName='ArialCV-Bold',fontSize=7.2,leading=8.5,textColor=INK,spaceBefore=4,spaceAfter=0.5,keepWithNext=True)
add('SideSmall',fontName='ArialCV',fontSize=6.8,leading=8.3,textColor=MUTED,spaceAfter=2)
add('Name',fontName='ArialCV-Bold',fontSize=21,leading=23,textColor=NAVY,spaceAfter=2)
add('Title',fontName='ArialCV',fontSize=8.6,leading=10.5,textColor=BLUE,spaceAfter=4)
add('LinkLine',fontName='ArialCV',fontSize=7.15,leading=9.3,textColor=INK,spaceAfter=1)
add('Sec',fontName='ArialCV-Bold',fontSize=9.5,leading=11,textColor=BLUE,spaceBefore=6.3,spaceAfter=3,keepWithNext=True)
add('Body',fontName='ArialCV',fontSize=7.9,leading=9.75,textColor=INK,spaceAfter=2)
add('Job',fontName='ArialCV-Bold',fontSize=8.35,leading=10,textColor=INK,spaceBefore=2.5,spaceAfter=1,keepWithNext=True)
add('BulletCV',fontName='ArialCV',fontSize=7.55,leading=9.15,textColor=INK,leftIndent=9,firstLineIndent=-6,spaceAfter=1)
add('Proj',fontName='ArialCV-Bold',fontSize=7.9,leading=9.25,textColor=INK,spaceBefore=2,spaceAfter=.5,keepWithNext=True)
add('Edu',fontName='ArialCV',fontSize=7.9,leading=9.5,textColor=INK,spaceAfter=2)

def sec(text):
    return [Paragraph(text.upper(),styles['Sec']),HRFlowable(width='100%',thickness=.5,color=RULE,spaceBefore=0,spaceAfter=2.5)]
def b(text): return Paragraph('• '+text,styles['BulletCV'])
def proj(title,desc): return KeepTogether([Paragraph(title,styles['Proj']),Paragraph(desc,styles['Body'])])

left=[]
left.append(Paragraph('OB',styles['Initials']))
left.append(Spacer(1,9))
left += sec('Contact')
left += [Paragraph('662 194 4414<br/>orlandito.bjz@gmail.com<br/>Hermosillo, Sonora, Mexico',styles['SideBody'])]
left += sec('Technical skills')
left += [Paragraph('Languages',styles['SideSub']),Paragraph('C#  ·  JavaScript<br/>TypeScript  ·  SQL<br/>HTML5 / CSS3',styles['SideBody'])]
left += [Paragraph('.NET',styles['SideSub']),Paragraph('.NET<br/>ASP.NET Core MVC<br/>Windows Forms<br/>Razor',styles['SideBody'])]
left += [Paragraph('Data &amp; Architecture',styles['SideSub']),Paragraph('SQL Server  ·  PostgreSQL<br/>Layered Architecture<br/>Clean Architecture<br/>CQRS  ·  Hangfire',styles['SideBody'])]
left += [Paragraph('Tools',styles['SideSub']),Paragraph('Git / GitHub  ·  AWS<br/>React  ·  Python<br/>Windows 10 / 11',styles['SideBody'])]
left += sec('Certifications')
left += [Paragraph('SQL (Basic)',styles['SideCert']),Paragraph('HackerRank',styles['SideSmall']),Paragraph('SQL (Intermediate)',styles['SideCert']),Paragraph('HackerRank · Sep 2026',styles['SideSmall']),Paragraph('Tester',styles['SideCert']),Paragraph('Capacítate para el Empleo<br/>In progress: 98% · Score: 9.25',styles['SideSmall'])]
left += sec('Languages')
left += [Paragraph('Spanish · Native<br/>English · Advanced',styles['SideBody'])]

right=[]
right += [Paragraph('Orlando Bojorquez Preciado',styles['Name']),Paragraph('Information Systems Engineering Student  ·  QA / Testing Intern',styles['Title'])]
right += [Paragraph('<link href="https://github.com/orlandouni" color="#344C61">GitHub: github.com/orlandouni</link>',styles['LinkLine']),Paragraph('<link href="https://www.linkedin.com/in/orlando-boj%C3%B3rquez-preciado-299b06362/" color="#344C61">LinkedIn: linkedin.com/in/orlando-boj&#243;rquez-preciado-299b06362</link>',styles['LinkLine'])]
right += sec('Profile')
right += [Paragraph('Seventh-semester Information Systems Engineering student at Universidad de Sonora with a 95.93/100 average. Experience developing and maintaining production web applications, diagnosing technical issues, and supporting users. Currently completing Tester training and seeking a QA/testing internship where I can apply analytical thinking, SQL knowledge, attention to detail, and a user-focused approach.',styles['Body'])]
right += sec('Experience')
right += [Paragraph('Junior Web Developer · Simetria (Remote)  <font color="#68859A">|  2025 – Oct 2026</font>',styles['Job'])]
right += [b('Contributed to development and maintenance of Cinemall.mx, a live movie theater website and app, using C# and ASP.NET Core MVC.'),b('Implemented QR-code redirection, legal pages, a presale section, and UI improvements; helped diagnose technical issues.'),b('Built and deployed simetria-agencia.com with Next.js, TypeScript, and AWS; managed admin-panel content and collaborated with the development team.')]
right += [Paragraph('Sales &amp; Customer Service Agent · RH Solutions  <font color="#68859A">|  2023</font>',styles['Job']),b('Served international customers in English, followed up on cases, and recorded information in internal systems while working under pressure.')]
right += sec('Projects & Achievements')
right += [proj('MiUni','Co-developed a university web platform with an interactive campus map, place search, route calculation, and conversational assistant. React frontend; .NET backend integrated with PostgreSQL and a Python-based RAG service.'),proj('Buho Go','Academic proposal for a campus ordering platform with nearby-business catalogs, advance orders, preparation estimates, ready notifications, and optional campus delivery.'),proj('Smart City Hackathon','Top 10 finalist.'),proj('Cinemall.mx','Production movie-theater website and app; contributed with C# and ASP.NET Core MVC.'),proj('simetria-agencia.com','Production agency website built with Next.js, TypeScript, Tailwind CSS, and AWS Amplify.'),proj('Point-of-sale systems','C# Windows Forms application using layered architecture and GDI+ ticket printing.')]
right += sec('Education')
right += [Paragraph('<b>B.S. in Information Systems Engineering</b>  <font color="#68859A">|  2023 – Present</font><br/>Universidad de Sonora · 7th semester · Average 95.93/100',styles['Edu'])]

leftframe=Frame(20,42,142,H-75,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0,id='left',showBoundary=0)
rightframe=Frame(204,42,W-232,H-75,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0,id='right',showBoundary=0)
doc=CVDoc(OUT,pagesize=letter,leftMargin=204,rightMargin=28,topMargin=32,bottomMargin=42,title='Orlando Bojorquez Preciado - CV',author='Orlando Bojorquez Preciado')
doc.addPageTemplates([PageTemplate(id='CV',frames=[leftframe,rightframe],onPage=page)])
doc.build(left+[FrameBreak()]+right)
print(OUT)




