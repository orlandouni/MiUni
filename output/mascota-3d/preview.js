import * as T from 'three';
import {OrbitControls} from 'three/addons/controls/OrbitControls.js';
import {createOwl} from './owl.js';
const stage=document.getElementById('stage');
try {
 const renderer=new T.WebGLRenderer({antialias:true,alpha:true});renderer.setPixelRatio(Math.min(devicePixelRatio,2));renderer.setClearColor('#f5f3ee');renderer.toneMapping=T.ACESFilmicToneMapping;renderer.toneMappingExposure=1.25;stage.append(renderer.domElement);
 const scene=new T.Scene(),camera=new T.PerspectiveCamera(34,1,.1,60);camera.position.set(1.4,1.9,5.8);
 const controls=new OrbitControls(camera,renderer.domElement);controls.target.set(0,1.2,0);controls.minDistance=3.6;controls.maxDistance=10;controls.enableDamping=true;controls.maxPolarAngle=Math.PI*.85;
 scene.add(new T.HemisphereLight('#eaf4ff','#b4a997',2.4));
 const light=new T.DirectionalLight('#fff5e2',3.3);light.position.set(-3,5,5);scene.add(light);
 const fill=new T.DirectionalLight('#d9eaff',1.3);fill.position.set(4,3,-2);scene.add(fill);
 const {owl,eyes,smiles,left}=createOwl();scene.add(owl);
 const ground=new T.Mesh(new T.CircleGeometry(1.45,80),new T.MeshBasicMaterial({color:'#d9d9d2',transparent:true,opacity:.32}));ground.rotation.x=-Math.PI/2;ground.position.y=.045;ground.scale.z=.6;scene.add(ground);
 let expression='normal',waveUntil=0;
 document.querySelectorAll('[data-expression]').forEach(b=>b.addEventListener('click',()=>{expression=b.dataset.expression;document.querySelectorAll('[data-expression]').forEach(x=>x.setAttribute('aria-pressed',String(x===b)));}));
 document.getElementById('wave').onclick=()=>{waveUntil=performance.now()+1600;};
 document.getElementById('front').onclick=()=>{camera.position.set(0,1.9,6.5);controls.target.set(0,1.2,0);};
 new ResizeObserver(()=>{const w=stage.clientWidth,h=stage.clientHeight;renderer.setSize(w,h);camera.aspect=w/h;camera.updateProjectionMatrix();}).observe(stage);
 const reduce=matchMedia('(prefers-reduced-motion: reduce)').matches;
 renderer.setAnimationLoop(now=>{
   eyes.forEach((p,i)=>{p.visible=expression!=='happy';smiles[i].visible=expression==='happy';p.scale.y=expression==='think'?(i===0?.12:.17):.195;p.position.x=expression==='think'?.035:(i===0?.018:-.018);});
   owl.rotation.z=expression==='think'?-.07:expression==='happy'?.035:0;
   left.rotation.z=!reduce&&now<waveUntil?Math.sin(now*.014)*.24+.2:0;
   controls.update();renderer.render(scene,camera);
 });
 window.miuniPreview={scene,owl,renderer,camera};
} catch(error){document.getElementById('error').textContent='No se pudo abrir la vista 3D. Comprueba que el navegador tenga WebGL activado.';console.error(error);}
