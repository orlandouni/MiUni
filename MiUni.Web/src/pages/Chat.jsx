import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, BookOpen, ChevronRight, Clock3, Menu, Plus, Send, Sparkles, Trash2, X } from "lucide-react";
import api, { clearSession, getMe, getToken } from "../services/api";
import { streamChat } from "../services/chatStream";
import LugaresDelMensaje from "../components/LugaresDelMensaje";
import ChatAnswer from "../components/ChatAnswer";
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
  const streamRef = useRef(null);
  const [receiving, setReceiving] = useState(false);
  useEffect(() => () => streamRef.current?.abort(), []);
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
    const assistantId = `stream-${Date.now()}`;
    const controller = new AbortController();
    streamRef.current = controller;
    setReceiving(false);
    let activeSessionId = sessionId;
    try {
      const result = await streamChat({
        baseURL: api.defaults.baseURL || "",
        token: getToken(), mensaje, sesionId: sessionId, signal: controller.signal,
        onSession: id => { activeSessionId = id; setSessionId(id); },
        onText: (_part, contenido) => {
          setReceiving(true);
          setMessages(actual => {
            const exists = actual.some(message => message.id === assistantId);
            return exists
              ? actual.map(message => message.id === assistantId ? { ...message, contenido } : message)
              : [...actual, { id: assistantId, rol: "assistant", contenido, fechaCreacion: new Date().toISOString() }];
          });
        },
      });
      if (import.meta.env.DEV) console.info("miuni_chat_stream", result.metrics);
      const { data } = await api.get(`/api/Chat/sessions/${result.sessionId}`, { signal: controller.signal });
      const persisted = data.at(-1);
      if (persisted?.rol !== "assistant" || persisted.contenido !== result.answer) {
        throw new Error("La respuesta se interrumpió o no pudo guardarse. Inténtalo de nuevo.");
      }
      setMessages(data);
    } catch (e) {
      if (e.name !== "AbortError" && e.code !== "ERR_CANCELED") {
        if (e.status === 401) clearSession();
        setError(e.response?.data?.message || e.message || "El asistente no está disponible en este momento.");
      }
    } finally {
      if (!controller.signal.aborted) {
        if (activeSessionId) {
          try {
            const historial = await api.get("/api/Chat/sessions", { signal: controller.signal });
            setSessions(historial.data || []);
          } catch { /* Preserve the answer if refreshing the sidebar fails. */ }
        }
        setSending(false);
        setReceiving(false);
      }
      if (streamRef.current === controller) streamRef.current = null;
    }
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
    <section className="chat-main"><header className="chat-topbar"><button type="button" className="chat-mobile-back" aria-label="Abrir conversaciones" aria-controls="chat-sidebar" aria-expanded={sidebarOpen} onClick={() => setSidebarOpen(true)}><Menu size={19} /></button><span className="chat-top-icon"><BookOpen size={16} /></span><strong>MIUNI</strong><div className="chat-user"><span>{initials}</span><ChevronRight size={14} /></div></header><div className={`chat-content ${messages.length ? "chat-content-active" : ""}`}>{!messages.length && <div className="chat-intro"><div className="chat-hero-icon"><Sparkles size={25} /></div><h1>Asistente MIUNI</h1><p>Pregúntame sobre edificios, tienditas, horarios y servicios del campus.</p>{!messages.length && <div className="chat-suggestions">{sugerencias.map(sugerencia => <button key={sugerencia} onClick={() => { setDraft(sugerencia); }}>{sugerencia}</button>)}</div>}</div>}<div className="chat-messages" ref={messagesRef} tabIndex={0} aria-label="Mensajes de la conversación">{messages.map(message => <div className={`chat-message ${message.rol === "user" ? "usuario" : "asistente"}`} key={message.id}><div className="chat-bubble">{message.rol === "assistant" ? (
                <ChatAnswer messageId={message.id} ownerId={user?.id} content={message.contenido} timestamp={fecha(message.fechaCreacion)} streaming={sending && message.id.startsWith("stream-")}>
                  {!message.id.startsWith("stream-") && <LugaresDelMensaje texto={message.contenido} />}
                </ChatAnswer>
              ) : <>{message.contenido}<small>{fecha(message.fechaCreacion)}</small></>}</div></div>)}{sending && !receiving && <div className="chat-message asistente"><div className="chat-bubble"><span className="chat-generating" role="status"><span className="chat-generating-dot" aria-hidden="true" /> Preparando respuesta…</span></div></div>}</div>{error && <p className="chat-error" role="alert">{error}</p>}<form className="chat-composer" onSubmit={enviar}><textarea value={draft} onChange={event => setDraft(event.target.value)} onKeyDown={event => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); enviar(event); } }} placeholder="Escribe tu duda sobre el campus…" rows={2} disabled={busy} /><button type="submit" aria-label="Enviar mensaje" disabled={!draft.trim() || busy}><Send size={18} /></button><small>Enter para enviar · Shift + Enter para una nueva línea</small></form></div></section>
  </main>;
}
