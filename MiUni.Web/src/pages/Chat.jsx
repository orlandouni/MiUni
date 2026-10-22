import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, BookOpen, ChevronRight, Clock3, Menu, Plus, Send, Sparkles, MapPin, Coffee, Building2, ArrowUpRight, X, AlertCircle, Map, Trash2 } from "lucide-react";
import api, { clearSession, getMe, getToken } from "../services/api";
import { streamChat } from "../services/chatStream";
import LugaresDelMensaje from "../components/LugaresDelMensaje";
import ChatAnswer from "../components/ChatAnswer";
import "./Chat.css";
import MascotaIlustrada from "../components/MascotaIlustrada";

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
  const composerRef = useRef(null);
  const streamRef = useRef(null);
  const [receiving, setReceiving] = useState(false);
  useEffect(() => () => streamRef.current?.abort(), []);
  const [deleting, setDeleting] = useState(null);
  const [opening, setOpening] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(() => window.matchMedia("(min-width: 701px)").matches);
  const sidebarToggleRef = useRef(null);

  function cerrarSidebar() {
    setSidebarOpen(false);
    sidebarToggleRef.current?.focus();
  }

  function cerrarSidebarEnMovil() {
    if (window.matchMedia("(max-width: 700px)").matches) cerrarSidebar();
  }
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

  useEffect(() => { const panel = messagesRef.current; if (panel) panel.scrollTo({ top: panel.scrollHeight, behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth" }); }, [messages, sending]);

  async function abrirSesion(id) {
    if (busy) return;
    setOpening(true);
    if (window.matchMedia("(max-width: 700px)").matches) setSidebarOpen(false);
    setError("");
    try { const { data } = await api.get(`/api/Chat/sessions/${id}`); setSessionId(id); setMessages(data); }
    catch { setError("No se pudo abrir esta conversación."); }
    finally { setOpening(false); }
  }

  function nuevaConversacion() { if (busy) return; if (window.matchMedia("(max-width: 700px)").matches) setSidebarOpen(false); setSessionId(null); setMessages([]); setDraft(""); setError(""); }

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
  const suggestionIcons = [MapPin, Coffee, Building2];
  const chooseSuggestion = value => { setDraft(value); composerRef.current?.focus(); };
  return <main className="chat-page">
    <a href="#chat-input" className="skip-link">Saltar a escribir un mensaje</a>
    <aside id="chat-sidebar" className={`chat-sidebar ${sidebarOpen ? "abierta" : ""}`}>
      <div className="chat-side-brand"><span className="chat-side-logo">M</span><div><strong>MIUNI</strong><small>Tu campus, a la mano</small></div><button className="chat-close" onClick={() => setSidebarOpen(false)} aria-label="Cerrar historial"><X size={18} /></button></div>
      <Link to="/" className="chat-back"><ArrowLeft size={16} /> Volver al inicio</Link>
      <button className="chat-new" disabled={busy} onClick={nuevaConversacion}><Plus size={17} /> Nueva conversación</button>
      <div className="chat-history"><span className="chat-history-label">Tus conversaciones</span>
        {loading ? <div className="chat-history-loading" role="status">Cargando historial…</div> : sessions.length === 0 ? <div className="chat-history-empty"><Clock3 size={20} /><p>Un nuevo comienzo.</p><small>Tus conversaciones aparecerán aquí.</small></div> : sessions.map(session => <div className="chat-history-row" key={session.sesionId}><button className={session.sesionId === sessionId ? "seleccionada" : ""} disabled={busy} onClick={() => abrirSesion(session.sesionId)}><Clock3 size={15} /><span>{session.preview || "Conversación sin título"}</span></button><button className="chat-delete" disabled={busy} onClick={() => eliminarSesion(session)} aria-label={`Eliminar conversación: ${session.preview || "Sin título"}`} title="Eliminar conversación"><Trash2 size={15} /></button></div>)}
      </div>
      <Link className="chat-map-link" to="/mapa"><Map size={19} /><span>¿Prefieres explorar?<strong>Abrir el mapa del campus</strong></span><ArrowUpRight size={15} /></Link>
    </aside>
    {sidebarOpen && <button className="chat-sidebar-overlay" aria-label="Cerrar historial" onClick={() => setSidebarOpen(false)} />}
    <section className="chat-main">
      <header className="chat-topbar"><button className="chat-mobile-back" aria-label={sidebarOpen ? "Ocultar historial" : "Abrir historial"} title={sidebarOpen ? "Ocultar historial" : "Abrir historial"} aria-controls="chat-sidebar" aria-expanded={sidebarOpen} onClick={() => setSidebarOpen(open => !open)}><Menu size={20} /></button><span className="chat-top-icon"><Sparkles size={17} /></span><div><strong>Asistente MiUni</strong><span className="chat-top-subtitle">Tu guía para la vida en el campus</span></div><div className="chat-top-status"><span /> A tu lado</div><div className="chat-user"><span>{initials}</span><ChevronRight size={14} /></div></header>
      <div className="chat-content">
        <section className={`chat-intro ${messages.length ? "compacta" : ""}`}>
          <div className="chat-intro-copy"><span className="ui-eyebrow"><Sparkles size={13} /> Menos dudas, más campus</span><h1>{messages.length ? "Lo encontramos juntos." : <>¿Qué necesitas<br /><em>encontrar hoy?</em></>}</h1><p>Edificios, un buen café o ese horario que no recuerdas.<br className="chat-desktop-break" /> Pregúntame y empezamos.</p></div>
          {!messages.length && <div className="chat-suggestions" aria-label="Ideas para empezar"><span className="chat-suggestion-label">Prueba con una pregunta</span>{sugerencias.map((sugerencia, i) => {const Icon = suggestionIcons[i]; return <button key={sugerencia} disabled={busy} onClick={() => chooseSuggestion(sugerencia)}><span className={`chat-suggestion-icon icon-${i}`}><Icon size={18} /></span><span>{sugerencia}</span><ArrowUpRight size={15} /></button>;})}</div>}
        </section>
        <div className="chat-conversation">
          <aside className="chat-mascot-side"><MascotaIlustrada /><span>{sending ? "Pensando…" : "MiUni"}</span></aside>
          <div className="chat-messages" ref={messagesRef} tabIndex={0} role="log" aria-label="Conversación con MiUni" aria-live="polite" aria-relevant="additions text">
            {!messages.length && <div className="chat-empty-note"><BookOpen size={18} /><p>Este espacio es tuyo.<span>Una pregunta basta para empezar a conocer tu universidad.</span></p></div>}
            {messages.map(message => <div className={`chat-message ${message.rol === "user" ? "usuario" : "asistente"}`} key={message.id}><div className="chat-bubble">{message.rol === "assistant" ? <ChatAnswer messageId={message.id} ownerId={user?.id} content={message.contenido} timestamp={fecha(message.fechaCreacion)} streaming={sending && String(message.id).startsWith("stream-")}>
              {!String(message.id).startsWith("stream-") && <LugaresDelMensaje texto={message.contenido} />}
            </ChatAnswer> : <><span className="chat-message-author">Tú</span><div className="chat-message-text">{message.contenido}</div><small>{fecha(message.fechaCreacion)}</small></>}</div></div>)}
            {sending && !receiving && <div className="chat-message asistente"><div className="chat-bubble chat-typing" role="status"><span className="sr-only">MiUni está preparando una respuesta</span><div aria-hidden="true"><i /><i /><i /></div><span aria-hidden="true">Buscando una respuesta…</span></div></div>}
          </div>
        </div>
        {error && <div className="chat-error" role="alert"><AlertCircle size={17} /><span>{error}</span><button onClick={() => {chooseSuggestion([...messages].reverse().find(message => message.rol === "user")?.contenido || "");}}>Revisar pregunta</button></div>}
        <form className="chat-composer" onSubmit={enviar}><label htmlFor="chat-input" className="sr-only">Tu pregunta para MiUni</label><textarea id="chat-input" ref={composerRef} value={draft} onChange={event => setDraft(event.target.value)} onKeyDown={event => { if (event.key === "Enter" && !event.shiftKey && !event.nativeEvent.isComposing) {event.preventDefault(); enviar(event);} }} placeholder="¿Qué quieres saber del campus?" rows={2} disabled={busy} /><button type="submit" aria-label="Enviar mensaje" disabled={!draft.trim() || busy}><Send size={18} /></button><div className="chat-composer-foot"><small>Enter para enviar <span>·</span> Shift + Enter para una nueva línea</small><span>Hecho para tu vida universitaria</span></div></form>
      </div>
    </section>
  </main>;
}
