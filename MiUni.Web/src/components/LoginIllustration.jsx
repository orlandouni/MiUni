import { useEffect, useRef } from "react";
import "./LoginIllustration.css";

const cards = [
  { name: "busca", depth: 3 },
  { name: "ubicate", depth: 4 },
  { name: "pregunta", depth: 3 },
];

export default function LoginIllustration() {
  const root = useRef(null);

  useEffect(() => {
    const element = root.current;
    const panel = element.closest(".ma-right");
    const motion = window.matchMedia("(prefers-reduced-motion: reduce)");
    const mouse = window.matchMedia("(hover: hover) and (pointer: fine)");
    let frame;

    const reset = () => {
      cancelAnimationFrame(frame);
      element.style.setProperty("--pointer-x", "0");
      element.style.setProperty("--pointer-y", "0");
    };
    const move = (event) => {
      if (motion.matches || !mouse.matches || event.pointerType !== "mouse") return;
      const { left, top, width, height } = panel.getBoundingClientRect();
      const x = Math.max(-1, Math.min(1, ((event.clientX - left) / width) * 2 - 1));
      const y = Math.max(-1, Math.min(1, ((event.clientY - top) / height) * 2 - 1));
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        element.style.setProperty("--pointer-x", String(x));
        element.style.setProperty("--pointer-y", String(y));
      });
    };

    panel.addEventListener("pointermove", move);
    panel.addEventListener("pointerleave", reset);
    panel.addEventListener("pointercancel", reset);
    motion.addEventListener("change", reset);
    mouse.addEventListener("change", reset);
    return () => {
      reset();
      panel.removeEventListener("pointermove", move);
      panel.removeEventListener("pointerleave", reset);
      panel.removeEventListener("pointercancel", reset);
      motion.removeEventListener("change", reset);
      mouse.removeEventListener("change", reset);
    };
  }, []);

  return (
    <div className="miuni-hero-fit">
      <div className="miuni-hero" ref={root} role="img"
        aria-label="MIUNI: buscador, mapa del campus y asistente universitario">
        <img className="miuni-hero-background" src="/auth/fondo.png" alt="" draggable="false" />
        {cards.map(({ name, depth }) => (
          <div key={name} className={`miuni-hero-card miuni-hero-${name}`} style={{ "--depth": `${depth}px` }}>
            <div className="miuni-hero-parallax">
              <div className="miuni-hero-entry">
                <img src={`/auth/${name}.png`} alt="" draggable="false" />
              </div>
            </div>
          </div>
        ))}
        <img className="miuni-hero-arrow miuni-hero-arrow-first" src="/auth/flechas.png" alt="" draggable="false" />
        <img className="miuni-hero-arrow miuni-hero-arrow-second" src="/auth/flechas.png" alt="" draggable="false" />
      </div>
    </div>
  );
}
