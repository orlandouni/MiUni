import {writeFile} from 'node:fs/promises';
import {GLTFExporter} from 'three/addons/exporters/GLTFExporter.js';
import {createOwl} from './owl.js';
globalThis.FileReader=class {
 readAsArrayBuffer(blob){blob.arrayBuffer().then(buffer=>{this.result=buffer;this.onloadend?.();});}
 readAsDataURL(blob){blob.arrayBuffer().then(buffer=>{this.result=`data:${blob.type};base64,${Buffer.from(buffer).toString('base64')}`;this.onloadend?.();});}
};
const {owl}=createOwl();
const binary=await new GLTFExporter().parseAsync(owl,{binary:true});
await writeFile(new URL('./miuni-buho.glb',import.meta.url),Buffer.from(binary));
console.log(`Modelo exportado: ${(binary.byteLength/1024/1024).toFixed(2)} MB`);
