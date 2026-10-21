import * as T from 'three';
import {GLTFLoader} from 'three/addons/loaders/GLTFLoader.js';
import {clone} from 'three/addons/utils/SkeletonUtils.js';

let modelPromise;
function loadModel() {
  if (!modelPromise) modelPromise = new GLTFLoader().loadAsync('/mascot/miuni-meshy-animado.glb').catch(error => {
    modelPromise = undefined;
    throw error;
  });
  return modelPromise;
}

export function createMascotaScene(host, {signal, initialMotion}) {
  let renderer;
  try { renderer = new T.WebGLRenderer({antialias: true, alpha: true}); }
  catch { return {play() {}, dispose() {}}; }
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.5));
  renderer.setClearColor(0, 0);
  renderer.toneMapping = T.ACESFilmicToneMapping;
  const scene = new T.Scene();
  const camera = new T.PerspectiveCamera(33, 1, .01, 30);
  camera.position.set(0, 1.3, 5.2); camera.lookAt(0, 1.2, 0);
  scene.add(new T.HemisphereLight('#f4f8ff', '#a9a195', 2.7));
  const light = new T.DirectionalLight('#fff7e8', 2.6); light.position.set(-3, 5, 5); scene.add(light);
  host.append(renderer.domElement);
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  let mixer, model, active, actions = {}, requested = initialMotion, visible = false, disposed = false, last = 0;

  function render() { if (!disposed) renderer.render(scene, camera); }
  function play(name) {
    requested = name;
    if (!mixer || reducedMotion.matches) return;
    const next = actions[name] || actions.Reposo;
    if (!next) return;
    if (active && active !== next) active.fadeOut(.18);
    active = next;
    next.reset().setEffectiveWeight(1).setEffectiveTimeScale(1);
    const once = name === 'Asentir' || name === 'Negar';
    next.setLoop(once ? T.LoopOnce : T.LoopRepeat, once ? 1 : Infinity);
    next.clampWhenFinished = once;
    next.fadeIn(.18).play();
    syncLoop();
  }
  function syncLoop() {
    last = 0;
    renderer.setAnimationLoop(!disposed && visible && !document.hidden && !reducedMotion.matches && mixer ? tick : null);
    render();
  }
  function tick(now) {
    const delta = last ? Math.min((now-last)/1000, .05) : 0; last = now;
    mixer?.update(delta); render();
  }
  const resize = new ResizeObserver(() => {
    const width = host.clientWidth, height = host.clientHeight;
    if (!width || !height || disposed) return;
    renderer.setSize(width, height); camera.aspect = width/height; camera.updateProjectionMatrix(); render();
  });
  resize.observe(host);
  const observer = new IntersectionObserver(entries => {
    visible = entries[0].isIntersecting;
    if (visible && !model && !signal.aborted) initialize();
    syncLoop();
  });
  let initializing = false;
  async function initialize() {
    if (initializing) return;
    initializing = true;
    try {
      const gltf = await loadModel();
      if (disposed || signal.aborted) return;
      model = clone(gltf.scene);
      model.updateMatrixWorld(true);
      const bounds = new T.Box3().setFromObject(model), size = bounds.getSize(new T.Vector3()), center = bounds.getCenter(new T.Vector3());
      model.position.sub(new T.Vector3(center.x, bounds.min.y, center.z));
      const holder = new T.Group(); holder.add(model); holder.scale.setScalar(2.45/size.y); scene.add(holder);
      mixer = new T.AnimationMixer(model);
      for (const clip of gltf.animations) actions[clip.name] = mixer.clipAction(clip);
      mixer.addEventListener('finished', () => play('Reposo'));
      host.dataset.ready = 'true';
      play(requested); render();
    } catch { /* Keep the fallback image and do not interrupt the page. */ }
  }
  observer.observe(host);
  function motionPreferenceChanged() {
    if (reducedMotion.matches) { mixer?.stopAllAction(); model?.traverse(node => { if (node.isSkinnedMesh) node.pose(); }); }
    else play('Reposo');
    syncLoop();
  }
  reducedMotion.addEventListener('change', motionPreferenceChanged);
  document.addEventListener('visibilitychange', syncLoop);
  return {play, dispose() {
    disposed = true; resize.disconnect(); observer.disconnect();
    reducedMotion.removeEventListener('change', motionPreferenceChanged);
    document.removeEventListener('visibilitychange', syncLoop);
    renderer.setAnimationLoop(null); mixer?.stopAllAction();
    if (model) mixer?.uncacheRoot(model);
    renderer.dispose(); renderer.forceContextLoss(); renderer.domElement.remove();
    delete host.dataset.ready;
  }};
}
