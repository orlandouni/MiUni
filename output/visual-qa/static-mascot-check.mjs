import {chromium} from 'playwright-core';
import assert from 'node:assert/strict';
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
const context=await browser.newContext({viewport:{width:1366,height:768},reducedMotion:'reduce'});
await context.addInitScript(()=>localStorage.setItem('miuni_token','local-check'));
await context.route('**/api/**',route=>route.fulfill({json:route.request().url().endsWith('/Auth/me')?{nombre:'Estudiante',roles:['Estudiante']}:[]}));
const page=await context.newPage();const models=[];page.on('request',r=>{if(r.url().includes('.glb')||r.url().includes('mascotaScene'))models.push(r.url());});
for(const [url,name] of [['/','home'],['/asistente','chat']]) {
 await page.goto('http://127.0.0.1:5174'+url);await page.locator('.miuni-mascota img').waitFor();await page.locator('.miuni-mascota img').evaluate(i=>i.decode());assert.equal(await page.locator('canvas').count(),0);await page.screenshot({path:`output/visual-qa/screenshots/mascota-ilustrada-${name}.png`});
 if(name==='chat'){const b=await page.locator('.chat-mascot-side .miuni-mascota').boundingBox();assert.equal(Math.round(b.width),82);assert.equal(Math.round(b.height),100);}
}
assert.equal(models.length,0);console.log('Correcto: ilustración en inicio y chat, tamaño pequeño conservado, sin cargar modelo ni motor 3D.');await browser.close();
