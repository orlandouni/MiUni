import { useEffect, useRef } from "react";
import { Trash2 } from "lucide-react";
import "./ConfirmarEliminarChat.css";

export default function ConfirmarEliminarChat({ session, busy, error, onCancel, onConfirm, fallbackRef }) {
  const dialogRef = useRef(null);
  const cancelRef = useRef(null);
  useEffect(() => {
    const dialog = dialogRef.current;
    const anterior = document.activeElement;
    const fallback = fallbackRef.current;
    dialog.showModal();
    cancelRef.current.focus();
    return () => {
      dialog.close();
      if (anterior?.isConnected) anterior.focus();
      else if (fallback?.isConnected) fallback.focus();
    };
  }, [fallbackRef]);

  return <dialog ref={dialogRef} className="chat-confirm" aria-labelledby="chat-confirm-title" aria-describedby="chat-confirm-description" aria-busy={busy}
    onCancel={event => { event.preventDefault(); if (!busy) onCancel(); }}>
    <div className="chat-confirm-icon"><Trash2 size={24} aria-hidden="true" /></div>
    <h2 id="chat-confirm-title">¿Eliminar conversación?</h2>
    <p className="chat-confirm-preview">{session.preview || "Conversación sin título"}</p>
    <p id="chat-confirm-description">Se eliminarán esta conversación y todos sus mensajes. Esta acción no se puede deshacer.</p>
    {error && <p className="chat-confirm-error" role="alert">{error}</p>}
    <div className="chat-confirm-actions">
      <button ref={cancelRef} type="button" disabled={busy} onClick={onCancel}>Cancelar</button>
      <button className="chat-confirm-delete" type="button" disabled={busy} onClick={onConfirm}>{busy ? "Eliminando…" : "Eliminar conversación"}</button>
    </div>
  </dialog>;
}
