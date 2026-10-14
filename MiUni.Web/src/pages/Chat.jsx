import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, BookOpen, ChevronRight, Clock3, Menu, Plus, Send, Sparkles, Trash2, X } from "lucide-react";
import api, { getMe } from "../services/api";
import LugaresDelMensaje from "../components/LugaresDelMensaje";
import "./Chat.css";

const sugerencias = ["¿Dónde está la Biblioteca Central?", "¿Qué tienditas hay en el campus?", "¿Cuál es el horario de Rectoría?"];

function fecha(fechaValor) {
  if (!fechaValor) return "";
  return new Intl.DateTimeFormat("es-MX", { hour: "2-digit", minute: "2-digit" }).format(new Date(fechaValor));
}

export default function Chat() {
  const [user, setUser] = useState(null);
  const [sessions, setSessions] = useState([]);
  const [sessionId, setSessionId] = useState(null);
  const [messages, setMessages] = useState([]);
  const [draft, setDraft] = useState("");
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const messagesRef = useRef(null);
  const [deleting, setDeleting] = useState(null);
  const [opening, setOpening] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const busy = sending || opening || deleting !== null;

  useEffect(() => {
    let vigente = true;
    Promise.all([getMe(), api.get("/api/Chat/sessions")]).then(([me, result]) => {
      if (!vigente) return;
      setUser(me); setSessions(result.data || []);
    }).catch(() => { if (vigente) setError("No se pudo cargar el historial del asistente."); })
      .finally(() => { if (vigente) setLoading(false); });
    return () => { vigente = false; };
  }, []);

  useEffect(() => { const panel = messagesRef.current; if (panel) panel.scrollTo({ top: panel.scrollHeight, behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "instant" : "smooth" }); }, [messages, sending]);

  async function abrirSesion(id) {
    if (busy) return;
    setOpening(true); setSidebarOpen(false);
    setError("");
    try { const { data } = await api.get(`/api/Chat/sessions/${id}`); setSessionId(id); setMessages(data); }
    catch { setError("No se pudo abrir esta conversación."); }
    finally { setOpening(false); }
  }

  function nuevaConversacion() { if (busy) return; setSidebarOpen(false); setSessionId(null); setMessages([]); setDraft(""); setError(""); }

  async function enviar(evento) {
    evento?.preventDefault();
    const mensaje = draft.trim();
    if (!mensaje || busy) return;
    setSending(true); setError(""); setDraft("");
    const optimista = { id: `local-${Date.now()}`, rol: "user", contenido: mensaje, fechaCreacion: new Date().toISOString() };
    setMessages(actual => [...actual, optimista]);
    try {
      const { data } = await api.post("/api/Chat", { sesionId: sessionId, mensaje });
      setSessionId(data.sesionId); setMessages(data.messages || []);
      const historial = await api.get("/api/Chat/sessions"); setSessions(historial.data || []);
    } catch (e) { setError(e.response?.data?.message || "El asistente no está disponible en este momento."); }
    finally { setSending(false); }
  }

  async function eliminarSesion(session) {
    if (busy || !window.confirm("¿Eliminar esta conversación y todos sus mensajes? Esta acción no se puede deshacer.")) return;
    setDeleting(session.sesionId); setError("");
    try {
      await api.delete(`/api/Chat/sessions/${session.sesionId}`);
      setSessions(actual => actual.filter(item => item.sesionId !== session.sesionId));
      if (sessionId === session.sesionId) { setSessionId(null); setMessages([]); setDraft(""); }
    } catch { setError("No se pudo eliminar la conversación. Inténtalo de nuevo."); }
    finally { setDeleting(null); }
  }

  const initials = (user?.nombre || user?.email || "U").slice(0, 2).toUpperCase();
  return <main className="chat-page">
    <aside id="chat-sidebar" className={`chat-sidebar ${sidebarOpen ? "abierto" : ""}`}><button className="chat-sidebar-close" onClick={() => setSidebarOpen(false)} aria-label="Cerrar conversaciones"><X size={18} /></button><div className="chat-side-brand"><span className="chat-side-logo"><BookOpen size={15} /></span><div><strong>MIUNI</strong><small>Tu campus, a la mano</small></div></div><Link to="/" className="chat-back"><ArrowLeft size={15} /> Volver al mapa</Link><button className="chat-new" disabled={busy} onClick={nuevaConversacion}><Plus size={17} /> Nueva conversación</button><div className="chat-history"><span className="chat-history-label">Conversaciones recientes</span>{loading ? <small>Cargando…</small> : sessions.length === 0 ? <small>Aún no tienes conversaciones.</small> : sessions.map(session => <div className="chat-history-row" key={session.sesionId}><button className={session.sesionId === sessionId ? "seleccionada" : ""} disabled={busy} onClick={() => abrirSesion(session.sesionId)}><Clock3 size={14} /><span>{session.preview || "Conversación sin título"}</span></button><button className="chat-delete" disabled={busy} onClick={() => eliminarSesion(session)} aria-label={`Eliminar conversación: ${session.preview || "Sin título"}`} title="Eliminar conversación"><Trash2 size={15} /></button></div>)}</div></aside>
    <section className="chat-main"><header className="chat-topbar"><button type="button" className="chat-mobile-back" aria-label="Abrir conversaciones" aria-controls="chat-sidebar" aria-expanded={sidebarOpen} onClick={() => setSidebarOpen(true)}><Menu size={19} /></button><span className="chat-top-icon"><BookOpen size={16} /></span><strong>MIUNI</strong><div className="chat-user"><span>{initials}</span><ChevronRight size={14} /></div></header><div className={`chat-content ${messages.length ? "chat-content-active" : ""}`}>{!messages.length && <div className="chat-intro"><div className="chat-hero-icon"><Sparkles size={25} /></div><h1>Asistente MIUNI</h1><p>Pregúntame sobre edificios, tienditas, horarios y servicios del campus.</p>{!messages.length && <div className="chat-suggestions">{sugerencias.map(sugerencia => <button key={sugerencia} onClick={() => { setDraft(sugerencia); }}>{sugerencia}</button>)}</div>}</div>}<div className="chat-messages" ref={messagesRef} tabIndex={0} aria-label="Mensajes de la conversación">{messages.map(message => <div className={`chat-message ${message.rol === "user" ? "usuario" : "asistente"}`} key={message.id}><div className="chat-bubble">{message.contenido}{message.rol === "assistant" && <LugaresDelMensaje texto={message.contenido} />}<small>{fecha(message.fechaCreacion)}</small></div></div>)}{sending && <div className="chat-message asistente"><div className="chat-bubble chat-typing"><i></i><i></i><i></i></div></div>}</div>{error && <p className="chat-error" role="alert">{error}</p>}<form className="chat-composer" onSubmit={enviar}><textarea value={draft} onChange={event => setDraft(event.target.value)} onKeyDown={event => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); enviar(event); } }} placeholder="Escribe tu duda sobre el campus…" rows={2} disabled={busy} /><button type="submit" aria-label="Enviar mensaje" disabled={!draft.trim() || busy}><Send size={18} /></button><small>Enter para enviar · Shift + Enter para una nueva línea</small></form></div></section>
  </main>;
}
