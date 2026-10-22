import {chromium} from 'playwright-core';import {readFile,writeFile} from 'node:fs/promises';
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});const page=await browser.newPage();
// Use the local development origin so the renderer loads the same actual model.
await page.route('**/mascot-render',async route=>route.fulfill({body:await readFile(new URL('./render-mascot.html',import.meta.url),'utf8'),contentType:'text/html'}));
await page.goto('http://127.0.0.1:5174/mascot-render');await page.locator('body[data-ready=true]').waitFor({timeout:30000});const png=await page.locator('canvas').evaluate(c=>c.toDataURL('image/png'));await writeFile(new URL('../../MiUni.Web/public/mascot/miuni-fallback.png',import.meta.url),Buffer.from(png.split(',')[1],'base64'));await browser.close();console.log('Vista estática transparente exportada desde el modelo 3D real.');
