import {chromium} from 'playwright-core';
import assert from 'node:assert/strict';
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});
const context=await browser.newContext({viewport:{width:1366,height:768},reducedMotion:'no-preference'});
await context.route('**/api/**',route=>route.fulfill({json:[]}));
const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
try {
 await page.goto('http://127.0.0.1:5174/');await page.locator('.home-owl-3d[data-ready=true]').waitFor({timeout:20000});
 await page.locator('.home-content').evaluate(el=>el.scrollTop=el.scrollHeight);
 await page.locator('.home-assistant-strip[data-reveal=visible]').waitFor();
 assert.equal(await page.locator('.home-owl-3d').getAttribute('data-motion'),'Saludo');
 await page.getByRole('link',{name:/Abrir el asistente/}).click();await page.getByLabel('Correo institucional',{exact:true}).waitFor();
 for(const width of [768,320]) {
   await page.setViewportSize({width,height:900});
   for(const route of ['/','/mapa','/login','/registro']) {
     await page.goto('http://127.0.0.1:5174'+route);await page.locator('h1').waitFor();
     const dimensions=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));
     assert.ok(dimensions.scroll<=dimensions.width+1,`${width} ${route} overflow`);
   }
 }
 await page.emulateMedia({reducedMotion:'reduce'});await page.goto('http://127.0.0.1:5174/');await page.locator('.home-hero h1').waitFor();
 assert.equal(await page.locator('.route-frame').evaluate(el=>getComputedStyle(el).animationName),'none');
 assert.deepEqual(errors,[]);console.log('Correcto: revelado al hacer scroll, saludo, navegación protegida sin sesión, movimiento reducido y layouts a 768/320 px.');
} finally {await browser.close();}
