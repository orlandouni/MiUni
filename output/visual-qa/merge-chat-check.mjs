import {chromium} from 'playwright-core';
import assert from 'node:assert/strict';
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const context=await browser.newContext({viewport:{width:1366,height:768},reducedMotion:'reduce'});await context.addInitScript(()=>localStorage.setItem('miuni_token','merge-test'));
let stored=[],deleted=false;
const answer='## Biblioteca Central\n\nPuedes consultar libros y estudiar.\n\n[Universidad de Sonora](https://www.unison.mx/)';
await context.route('**/api/**',async route=>{
 const req=route.request(),url=new URL(req.url());let data=[];
 if(url.pathname==='/api/Auth/me')data={id:'owner-test',nombre:'Prueba local',roles:['Estudiante']};
 else if(url.pathname==='/api/Chat/stream'){
  const {mensaje}=req.postDataJSON();stored=[{id:'user-test',rol:'user',contenido:mensaje},{id:'answer-test',rol:'assistant',contenido:answer}];
  return route.fulfill({headers:{'Content-Type':'text/plain; charset=utf-8','X-Session-Id':'session-test'},body:answer});
 }else if(url.pathname==='/api/Chat/sessions')data=stored.length?[{sesionId:'session-test',preview:'Biblioteca Central'}]:[];
 else if(url.pathname==='/api/Chat/sessions/session-test'&&req.method()==='DELETE'){stored=[];deleted=true;return route.fulfill({status:204});}
 else if(url.pathname==='/api/Chat/sessions/session-test')data=stored;
 else if(url.pathname==='/api/Lugar')data=[{id:'biblioteca-id',nombre:'Biblioteca Central',latitud:29.0833,longitud:-110.9627}];
 await route.fulfill({json:data});
});
const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
try{
 await page.goto('http://127.0.0.1:5174/asistente');await page.locator('#chat-input').fill('Biblioteca Central');await page.getByRole('button',{name:'Enviar mensaje',exact:true}).click();
 await page.getByRole('button',{name:'Respuesta útil',exact:true}).waitFor();await page.getByRole('button',{name:'Respuesta útil',exact:true}).click();assert.equal(await page.getByRole('button',{name:'Respuesta útil',exact:true}).getAttribute('aria-pressed'),'true');
 await page.getByRole('button',{name:'Ver fuentes',exact:true}).click();await page.getByRole('region',{name:'Enlaces citados'}).waitFor();assert.equal(await page.locator('.chat-place-card').getAttribute('href'),'/mapa?lugar=biblioteca-id');assert.equal(await page.locator('.miuni-mascota img').count(),1);
 await page.screenshot({path:'output/visual-qa/screenshots/chat-merge-resolved.png'});
 page.once('dialog',d=>d.accept());await page.getByRole('button',{name:'Eliminar conversación: Biblioteca Central',exact:true}).click();await page.getByText('Un nuevo comienzo.',{exact:true}).waitFor();assert.equal(deleted,true);
 assert.deepEqual(errors,[]);console.log('Correcto: diseño, mascota estática, streaming, Markdown, valoración, fuentes, enlace al mapa y eliminación de sesión de prueba.');
}finally{await browser.close();}
