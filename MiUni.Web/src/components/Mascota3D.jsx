import {useEffect, useRef} from 'react';
import './Mascota3D.css';

export default function Mascota3D({motion = 'Reposo', reactionId = 0, className = ''}) {
  const host = useRef(null);
  const scene = useRef(null);
  const latest = useRef({motion, reactionId});

  useEffect(() => {
    latest.current = {motion, reactionId};
    scene.current?.play(motion);
  }, [motion, reactionId]);

  useEffect(() => {
    const element = host.current;
    let disposed = false;
    const controller = new AbortController();
    import('./mascotaScene.js').then(({createMascotaScene}) => {
      if (disposed) return;
      scene.current = createMascotaScene(element, {signal: controller.signal, initialMotion: latest.current.motion});
    }).catch(() => { /* The static fallback remains visible if 3D is unavailable. */ });
    return () => {
      disposed = true;
      controller.abort();
      scene.current?.dispose();
      scene.current = null;
    };
  }, []);

  return <div ref={host} className={`miuni-mascota-3d ${className}`} data-motion={motion} aria-hidden="true">
    <img className="miuni-mascota-fallback" src="/mascot/miuni-fallback.png" alt="" />
  </div>;
}
