import { useCallback, useEffect, useRef, useState } from "react";
import { Check, ChevronLeft, ChevronRight, X } from "lucide-react";
import { negocioError } from "../../services/negocioErrors";

/* eslint-disable react-refresh/only-export-components */

// Carga datos al montar y cada vez que cambia `clave` (p. ej. los filtros serializados).
export function useCarga(cargador, clave = "") {
  const [estado, setEstado] = useState({ cargando: true, datos: null, error: "" });
  const [version, setVersion] = useState(0);
  const ref = useRef(cargador);
  useEffect(() => { ref.current = cargador; });
  useEffect(() => {
    let activo = true;
    ref.current()
      .then(datos => { if (activo) setEstado({ cargando: false, datos, error: "" }); })
      .catch(e => { if (activo) setEstado(s => ({ ...s, cargando: false, error: negocioError(e) })); });
    return () => { activo = false; };
  }, [version, clave]);
  const recargar = useCallback(() => { setEstado(s => ({ ...s, cargando: true })); setVersion(v => v + 1); }, []);
  const setDatos = useCallback(fn => setEstado(s => ({ ...s, datos: typeof fn === "function" ? fn(s.datos) : fn })), []);
  return { ...estado, recargar, setDatos };
}

// Ejecuta una acción mostrando ocupado/éxito/error de forma uniforme.
export function useAccion() {
  const [ocupado, setOcupado] = useState("");
  const [aviso, setAviso] = useState(null);
  const ejecutar = useCallback(async (id, fn, exito) => {
    setOcupado(id); setAviso(null);
    try {
      const r = await fn();
      if (exito) setAviso({ tipo: "ok", texto: exito });
      return r ?? true;
    } catch (e) {
      setAviso({ tipo: "error", texto: negocioError(e) });
      return false;
    } finally { setOcupado(""); }
  }, []);
  return { ocupado, aviso, setAviso, ejecutar };
}

export function Aviso({ aviso, onCerrar }) {
  if (!aviso) return null;
  return <p className={aviso.tipo === "ok" ? "adm-ok" : "adm-error"} role={aviso.tipo === "ok" ? "status" : "alert"}>
    {aviso.texto}
    {onCerrar && <button type="button" aria-label="Cerrar aviso" onClick={onCerrar}><X size={14} /></button>}
  </p>;
}

export function Encabezado({ titulo, descripcion, children }) {
  return <div className="adm-section-head">
    <div><h2>{titulo}</h2>{descripcion && <p>{descripcion}</p>}</div>
    {children && <div className="adm-section-actions">{children}</div>}
  </div>;
}

export function Vacio({ titulo, texto }) {
  return <div className="adm-empty"><Check size={24} /><strong>{titulo}</strong>{texto && <span>{texto}</span>}</div>;
}

export function Cargando({ texto = "Cargando…" }) {
  return <p className="adm-empty" role="status">{texto}</p>;
}

export function Paginacion({ pagina, total, tamano, onCambio }) {
  const paginas = Math.max(1, Math.ceil(total / tamano));
  if (total <= tamano) return <p className="adm-pager-info">{total} resultado{total === 1 ? "" : "s"}</p>;
  return <div className="adm-pager">
    <span className="adm-pager-info">{total} resultados · página {pagina} de {paginas}</span>
    <button type="button" disabled={pagina <= 1} onClick={() => onCambio(pagina - 1)} aria-label="Página anterior"><ChevronLeft size={16} /></button>
    <button type="button" disabled={pagina >= paginas} onClick={() => onCambio(pagina + 1)} aria-label="Página siguiente"><ChevronRight size={16} /></button>
  </div>;
}

export function Modal({ titulo, onCerrar, children, ancho }) {
  useEffect(() => {
    const tecla = e => { if (e.key === "Escape") onCerrar(); };
    window.addEventListener("keydown", tecla);
    return () => window.removeEventListener("keydown", tecla);
  }, [onCerrar]);
  return <div className="adm-modal-backdrop" onMouseDown={e => { if (e.target === e.currentTarget) onCerrar(); }}>
    <div className="adm-modal" role="dialog" aria-modal="true" aria-label={titulo} style={ancho ? { maxWidth: ancho } : undefined}>
      <header><h3>{titulo}</h3><button type="button" className="adm-icon-btn" onClick={onCerrar} aria-label="Cerrar"><X size={18} /></button></header>
      {children}
    </div>
  </div>;
}

// Confirmación para acciones destructivas. `confirmar` = { titulo, texto, boton, accion }.
export function Confirmacion({ confirmar, onCerrar, ocupado }) {
  if (!confirmar) return null;
  return <Modal titulo={confirmar.titulo} onCerrar={onCerrar} ancho={440}>
    <p className="adm-modal-text">{confirmar.texto}</p>
    <div className="adm-modal-actions">
      <button type="button" className="adm-btn" onClick={onCerrar}>Cancelar</button>
      <button type="button" className="adm-btn peligro" disabled={ocupado} onClick={async () => { await confirmar.accion(); onCerrar(); }}>{confirmar.boton}</button>
    </div>
  </Modal>;
}

export function Insignia({ tono = "neutro", children }) {
  return <span className={`adm-badge ${tono}`}>{children}</span>;
}

const formatoFecha = new Intl.DateTimeFormat("es-MX", { dateStyle: "medium" });
export const fecha = valor => valor ? formatoFecha.format(new Date(valor)) : "—";

export function Estrellas({ valor }) {
  return <span className="adm-stars" aria-label={`${valor} de 5`}>{"★".repeat(valor)}<span>{"★".repeat(5 - valor)}</span></span>;
}
