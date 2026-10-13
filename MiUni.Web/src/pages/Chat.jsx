import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, BookOpen, ChevronRight, Clock3, Menu, Plus, Send, Sparkles, UserRound } from "lucide-react";
import api, { getMe } from "../services/api";
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
  const endRef = useRef(null);

  useEffect(() => {
    let vigente = true;
    Promise.all([getMe(), api.get("/api/Chat/sessions")]).then(([me, result]) => {
      if (!vigente) return;
      setUser(me); setSessions(result.data || []);
    }).catch(() => { if (vigente) setError("No se pudo cargar el historial del asistente."); })
      .finally(() => { if (vigente) setLoading(false); });
    return () => { vigente = false; };
  }, []);

  useEffect(() => { endRef.current?.scrollIntoView({ behavior: "smooth" }); }, [messages, sending]);

  async function abrirSesion(id) {
    setError("");
    try { const { data } = await api.get(`/api/Chat/sessions/${id}`); setSessionId(id); setMessages(data); }
    catch { setError("No se pudo abrir esta conversación."); }
  }

  function nuevaConversacion() { setSessionId(null); setMessages([]); setDraft(""); setError(""); }

  async function enviar(evento) {
    evento?.preventDefault();
    const mensaje = draft.trim();
    if (!mensaje || sending) return;
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

  const initials = (user?.nombre || user?.email || "U").slice(0, 2).toUpperCase();
  return <main className="chat-page">
    <aside className="chat-sidebar"><div className="chat-side-brand"><span className="chat-side-logo"><BookOpen size={15} /></span><div><strong>MIUNI</strong><small>Tu campus, a la mano</small></div></div><Link to="/" className="chat-back"><ArrowLeft size={15} /> Volver al mapa</Link><button className="chat-new" onClick={nuevaConversacion}><Plus size={17} /> Nueva conversación</button><div className="chat-history"><span className="chat-history-label">Conversaciones recientes</span>{loading ? <small>Cargando…</small> : sessions.length === 0 ? <small>Aún no tienes conversaciones.</small> : sessions.map(session => <button className={session.sesionId === sessionId ? "seleccionada" : ""} key={session.sesionId} onClick={() => abrirSesion(session.sesionId)}><Clock3 size={14} /><span>{session.preview || "Conversación sin título"}</span></button>)}</div></aside>
    <section className="chat-main"><header className="chat-topbar"><Link to="/" className="chat-mobile-back" aria-label="Volver"><Menu size={19} /></Link><span className="chat-top-icon"><BookOpen size={16} /></span><strong>MIUNI</strong><div className="chat-user"><span>{initials}</span><ChevronRight size={14} /></div></header><div className="chat-content"><div className={`chat-intro ${messages.length ? "compacta" : ""}`}><div className="chat-hero-icon"><Sparkles size={25} /></div><h1>Asistente MIUNI</h1><p>Pregúntame sobre edificios, tienditas, horarios y servicios del campus.</p>{!messages.length && <div className="chat-suggestions">{sugerencias.map(sugerencia => <button key={sugerencia} onClick={() => { setDraft(sugerencia); }}>{sugerencia}</button>)}</div>}</div><div className="chat-messages">{messages.map(message => <div className={`chat-message ${message.rol === "user" ? "usuario" : "asistente"}`} key={message.id}><div className="chat-bubble">{message.contenido}<small>{fecha(message.fechaCreacion)}</small></div></div>)}{sending && <div className="chat-message asistente"><div className="chat-bubble chat-typing"><i></i><i></i><i></i></div></div>}<div ref={endRef} /></div>{error && <p className="chat-error" role="alert">{error}</p>}<form className="chat-composer" onSubmit={enviar}><textarea value={draft} onChange={event => setDraft(event.target.value)} onKeyDown={event => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); enviar(event); } }} placeholder="Escribe tu duda sobre el campus…" rows={2} disabled={sending} /><button type="submit" aria-label="Enviar mensaje" disabled={!draft.trim() || sending}><Send size={18} /></button><small>Enter para enviar · Shift + Enter para una nueva línea</small></form></div></section>
  </main>;
}
