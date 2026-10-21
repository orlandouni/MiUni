import {chromium} from 'playwright-core';
import {mkdir,writeFile} from 'node:fs/promises';
import assert from 'node:assert/strict';
const base='http://127.0.0.1:5174';
const artifacts=new URL('./screenshots/',import.meta.url);await mkdir(artifacts,{recursive:true});
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader'],});
const context=await browser.newContext({viewport:{width:1366,height:768},reducedMotion:'reduce'});
await context.addInitScript(()=>localStorage.setItem('miuni_token','local-visual-test'));
const categories=[{id:'1',nombre:'Biblioteca'},{id:'2',nombre:'Comida'},{id:'3',nombre:'Edificio académico'},{id:'4',nombre:'Oficinas'}];
const places=[{id:'biblioteca',nombre:'Biblioteca Central',categoria:categories[0],descripcion:'Espacios de lectura, consulta y estudio en el campus.',latitud:29.0833,longitud:-110.9627},{id:'humanidades',nombre:'Biblioteca de Humanidades',categoria:categories[0],descripcion:'Consulta y salas de estudio.',latitud:29.0845,longitud:-110.9607},{id:'cafe',nombre:'Cafetería del campus',categoria:categories[1],descripcion:'Una pausa entre clases.',latitud:29.0825,longitud:-110.9635}];
let messages=[],lastAnswer='';
await context.route('**/api/**',async route=>{
 const request=route.request(),url=new URL(request.url()),path=url.pathname;let data=[];
 if(path==='/api/Auth/me')data={id:'test',nombre:'Estudiante',email:'test@example.invalid',roles:['Estudiante','Administrador']};
 else if(path==='/api/Categoria')data=categories;
 else if(path==='/api/Lugar')data=places.filter(p=>(!url.searchParams.get('q')||p.nombre.toLowerCase().includes(url.searchParams.get('q').toLowerCase()))&&(!url.searchParams.get('categoria')||p.categoria.nombre===url.searchParams.get('categoria')));
 else if(path==='/api/Chat/sessions')data=messages.length?[{sesionId:'test-session',preview:messages[0].contenido}]:[];
 else if(path==='/api/Chat'&&request.method()==='POST'){
   const req=request.postDataJSON();await new Promise(resolve=>setTimeout(resolve,900));
   if(req.mensaje.includes('fallo'))return route.fulfill({status:502,json:{message:'No se pudo obtener respuesta del asistente.'}});
   lastAnswer=req.mensaje.includes('sin datos')?'No tengo información sobre ese lugar.':'La Biblioteca Central está en el campus. Puedes ubicarla en el mapa y calcular tu ruta.';
   messages=[...messages,{id:messages.length+1,rol:'user',contenido:req.mensaje,fechaCreacion:new Date().toISOString()},{id:messages.length+2,rol:'assistant',contenido:lastAnswer,fechaCreacion:new Date().toISOString()}];data={sesionId:'test-session',answer:lastAnswer,messages};
 }else if(path.startsWith('/api/Chat/sessions/'))data=messages;
 else if(path==='/api/Ruta')data={geometria:{type:'LineString',coordinates:[[-110.9627,29.0833],[-110.9607,29.0845]]},distanciaMetros:265,distanciaConexionAMetros:5,distanciaConexionBMetros:7};
 else if(path==='/api/ReporteUsuario/resumen')data=[];
 else if(path==='/api/ReporteUsuario')data={items:[]};
 await route.fulfill({json:data});
});
const page=await context.newPage(),errors=[];page.on('pageerror',error=>errors.push(error.message));
const results=[];
async function capture(name){await page.screenshot({path:new URL(`${name}.png`,artifacts).pathname.replace(/^\/(\w:)/,'$1'),fullPage:false});}
async function fit(name){const dims=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));assert.ok(dims.scroll<=dims.width+1,`${name}: horizontal overflow ${JSON.stringify(dims)}`);results.push({name,...dims});}
try {
 await page.goto(base);await page.getByRole('heading',{name:'Tu campus, a tu ritmo.'}).waitFor();await page.evaluate(()=>document.fonts.ready);await page.locator('.home-owl-3d[data-ready=true]').waitFor({timeout:20000});await fit('home-1366');await capture('home-1366');
 await page.setViewportSize({width:1920,height:1080});await fit('home-1920');await capture('home-1920');
 await page.setViewportSize({width:1366,height:768});await page.goto(`${base}/mapa`);await page.getByRole('button',{name:'Biblioteca',exact:true}).click();await page.getByRole('button',{name:/Biblioteca Central Biblioteca/}).click();await page.locator('.campus-place-card').getByRole('button',{name:'Desde aquí'}).click();await page.getByRole('button',{name:/Biblioteca de Humanidades Biblioteca/}).click();await page.locator('.campus-place-card').getByRole('button',{name:'Ir aquí'}).click();await page.getByRole('button',{name:'Calcular ruta',exact:true}).click();await page.getByText('265 m',{exact:true}).waitFor();await fit('map-route-1366');await capture('map-1366');
 await page.goto(`${base}/asistente`);await page.getByRole('heading',{name:'¿Qué necesitas encontrar hoy?'}).waitFor();await fit('chat-empty-1366');await capture('chat-empty-1366');
 await page.getByRole('button',{name:'¿Dónde está la Biblioteca Central?',exact:true}).click();assert.match(await page.locator('#chat-input').inputValue(),/Biblioteca/);await page.getByRole('button',{name:'Enviar mensaje',exact:true}).click();await page.getByText('Buscando una respuesta…',{exact:true}).waitFor();assert.equal(await page.locator('.chat-mascot-side .miuni-mascota-3d').getAttribute('data-motion'),'Reposo');await page.getByText('La Biblioteca Central está en el campus. Puedes ubicarla en el mapa y calcular tu ruta.',{exact:true}).waitFor();assert.equal(await page.locator('.chat-mascot-side .miuni-mascota-3d').getAttribute('data-motion'),'Asentir');await fit('chat-answer-1366');await capture('chat-answer-1366');
 await page.locator('#chat-input').fill('sin datos');await page.getByRole('button',{name:'Enviar mensaje',exact:true}).click();await page.getByText('No tengo información sobre ese lugar.',{exact:true}).waitFor();assert.equal(await page.locator('.chat-mascot-side .miuni-mascota-3d').getAttribute('data-motion'),'Negar');
 await page.locator('#chat-input').fill('fallo');await page.getByRole('button',{name:'Enviar mensaje',exact:true}).click();await page.getByRole('alert').waitFor();await page.getByRole('button',{name:'Revisar pregunta'}).click();assert.equal(await page.locator('#chat-input').inputValue(),'fallo');await capture('chat-error-1366');
 await page.setViewportSize({width:1920,height:1080});await fit('chat-1920');await capture('chat-1920');
 for(const route of ['/login','/registro','/favoritos','/resenas','/negocios','/admin','/resenas-reportes','/no-existe']) {await page.setViewportSize({width:1366,height:768});await page.goto(base+route);await page.locator('h1').waitFor();await fit(route);if(route==='/login')await capture('login-1366');}
 for(const route of ['/','/mapa','/asistente','/login','/registro','/favoritos','/negocios']) {
   await page.setViewportSize({width:390,height:844});await page.goto(base+route);await page.locator('h1').waitFor();await fit(`mobile-${route}`);
   if(route==='/')await capture('home-mobile');if(route==='/mapa')await capture('map-mobile');if(route==='/asistente'){await capture('chat-mobile');await page.getByRole('button',{name:'Abrir historial'}).click();await page.getByRole('button',{name:'Cerrar historial',exact:true}).first().click();}
 }
 assert.deepEqual(errors,[]);
 const report={fixtures:true,note:'Pruebas de navegador aisladas con respuestas API simuladas. No se modificaron datos del servidor.',checks:results,errors};await writeFile(new URL('./report.json',import.meta.url),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
}catch(error){await capture('failure');console.error(error);process.exitCode=1;}finally{await browser.close();}
