import * as T from 'three';
import {OrbitControls} from 'three/addons/controls/OrbitControls.js';
import {GLTFLoader} from 'three/addons/loaders/GLTFLoader.js';
const stage=document.getElementById('stage'), status=document.getElementById('status');
try {
 const renderer=new T.WebGLRenderer({antialias:true});renderer.setPixelRatio(Math.min(devicePixelRatio,2));renderer.setClearColor('#f5f3ee');renderer.toneMapping=T.ACESFilmicToneMapping;stage.append(renderer.domElement);
 const scene=new T.Scene(), camera=new T.PerspectiveCamera(34,1,.01,100);camera.position.set(0,1.4,5.5);
 const controls=new OrbitControls(camera,renderer.domElement);controls.enableDamping=true;controls.target.set(0,1.15,0);controls.minDistance=2;controls.maxDistance=10;
 scene.add(new T.HemisphereLight('#f4f8ff','#a9a195',2.7));const key=new T.DirectionalLight('#fff7e8',2.6);key.position.set(-3,5,5);scene.add(key);
 let mixer,active, actions={},paused=false,last=0;
 function play(name){if(!actions[name])return;paused=false;document.getElementById('pause').textContent='Pausar';document.getElementById('pause').setAttribute('aria-pressed','false');if(active)active.fadeOut(.2);active=actions[name];active.reset().fadeIn(.2).play();document.querySelectorAll('[data-clip]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.clip===name)));}
 new GLTFLoader().load('./miuni-meshy-animado.glb',g=>{
   const holder=new T.Group(), model=g.scene;model.updateMatrixWorld(true);
   const bounds=new T.Box3().setFromObject(model),size=bounds.getSize(new T.Vector3()),center=bounds.getCenter(new T.Vector3());
   const scale=2.45/size.y;model.position.sub(new T.Vector3(center.x,bounds.min.y,center.z));holder.add(model);holder.scale.setScalar(scale);scene.add(holder);
   mixer=new T.AnimationMixer(model);for(const clip of g.animations){actions[clip.name]=mixer.clipAction(clip);}
   document.querySelectorAll('button[disabled]').forEach(b=>b.disabled=false);
   status.textContent='Tu modelo contiene 4 movimientos. Selecciona uno para probarlo.';
   if(!matchMedia('(prefers-reduced-motion: reduce)').matches)play('Reposo');
 },undefined,error=>{status.textContent='No se pudo abrir el modelo.';console.error(error);});
 document.querySelectorAll('[data-clip]').forEach(b=>b.onclick=()=>play(b.dataset.clip));
 document.getElementById('pause').onclick=()=>{paused=!paused;document.getElementById('pause').textContent=paused?'Continuar':'Pausar';document.getElementById('pause').setAttribute('aria-pressed',String(paused));};
 document.getElementById('reset').onclick=()=>{camera.position.set(0,1.4,5.5);controls.target.set(0,1.15,0);};
 new ResizeObserver(()=>{renderer.setSize(stage.clientWidth,stage.clientHeight);camera.aspect=stage.clientWidth/stage.clientHeight;camera.updateProjectionMatrix();}).observe(stage);
 renderer.setAnimationLoop(now=>{const delta=last?Math.min((now-last)/1000,.05):0;last=now;if(mixer&&!paused)mixer.update(delta);controls.update();renderer.render(scene,camera);});
}catch(error){status.textContent='No se pudo iniciar el visor 3D.';console.error(error);}
