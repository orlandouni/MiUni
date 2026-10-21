import { useEffect, useId, useRef, useState } from "react";
import { BookOpen, Check, Copy, Link2, ThumbsDown, ThumbsUp } from "lucide-react";
import Markdown from "react-markdown";
import remarkGfm from "remark-gfm";

const markdownPlugins = [remarkGfm];
const markdownComponents = {
  a: ({ href, children }) => (
    <a href={href} target={href?.startsWith("#") ? undefined : "_blank"} rel="noopener noreferrer">
      {children}
    </a>
  ),
  table: ({ children }) => (
    <div className="chat-table-scroll" role="region" aria-label="Tabla de la respuesta" tabIndex={0}>
      <table>{children}</table>
    </div>
  ),
};

export default function ChatAnswer({ content, timestamp, messageId, ownerId, streaming = false, children }) {
  const [copyState, setCopyState] = useState("idle");
  const storageKey = ownerId && messageId && !messageId.startsWith("stream-")
    ? `miuni:answer-feedback:${ownerId}:${messageId}` : null;
  const [feedback, setFeedback] = useState(() => {
    try {
      const saved = storageKey && window.localStorage.getItem(storageKey);
      return saved === "useful" || saved === "not-useful" ? saved : null;
    } catch { return null; }
  });
  const [feedbackStatus, setFeedbackStatus] = useState("");
  const [sources, setSources] = useState([]);
  const [sourcesOpen, setSourcesOpen] = useState(false);
  const sourcesId = useId();
  const markdownRef = useRef(null);
  const resetCopy = useRef(null);
  const actionsDisabled = streaming || !content?.trim();
  useEffect(() => () => clearTimeout(resetCopy.current), []);

  async function copyAnswer() {
    clearTimeout(resetCopy.current);
    setCopyState("copying");
    try {
      await navigator.clipboard.writeText(content);
      setCopyState("copied");
      resetCopy.current = setTimeout(() => setCopyState("idle"), 2000);
    } catch {
      setCopyState("error");
    }
  }

  function rateAnswer(value) {
    const next = feedback === value ? null : value;
    setFeedback(next);
    try {
      if (next) window.localStorage.setItem(storageKey, next);
      else window.localStorage.removeItem(storageKey);
      setFeedbackStatus(next ? "Valoración guardada solo en este navegador." : "Valoración eliminada.");
    } catch {
      setFeedbackStatus("La valoración se mantendrá mientras esta respuesta esté abierta.");
    }
  }

  function toggleSources() {
    if (!sourcesOpen) {
      // Read the sanitized Markdown links, including reference links and GFM autolinks.
      const links = new Map();
      for (const anchor of markdownRef.current?.querySelectorAll("a[href]") || []) {
        const href = anchor.getAttribute("href");
        if (!/^https?:\/\//i.test(href)) continue;
        try {
          const url = new URL(href);
          if (!links.has(url.href)) links.set(url.href, {
            href: url.href,
            label: anchor.textContent.trim() || url.hostname,
            host: url.hostname,
          });
        } catch { /* Ignore malformed citation URLs. */ }
      }
      setSources([...links.values()]);
    }
    setSourcesOpen(open => !open);
  }

  return <>
    <div className="chat-answer-heading">
      <span className="chat-answer-avatar" title="MIUNI"><BookOpen size={14} aria-hidden="true" /></span>
      <span className="chat-answer-name">MIUNI</span>
      {streaming && <span className="chat-generating" role="status">
        <span className="chat-generating-dot" aria-hidden="true" /> Generando…
      </span>}
    </div>
    <div className="chat-markdown" ref={markdownRef}>
      <Markdown remarkPlugins={markdownPlugins} components={markdownComponents} skipHtml>
        {content}
      </Markdown>
    </div>
    {children}
    <div className="chat-answer-footer">
      <small>{timestamp}</small>
      <div className="chat-answer-actions" role="group" aria-label="Acciones de la respuesta">
        <button
          className="chat-answer-action"
          type="button"
          onClick={copyAnswer}
          disabled={actionsDisabled || copyState === "copying"}
          title={streaming ? "Disponible cuando termine la respuesta" : "Copiar respuesta"}
          aria-label={copyState === "copied" ? "Respuesta copiada" : "Copiar respuesta"}
        >
          {copyState === "copied" ? <Check size={14} aria-hidden="true" /> : <Copy size={14} aria-hidden="true" />}
          <span aria-live="polite">{copyState === "copied" ? "Copiada" : "Copiar"}</span>
        </button>
        <button className="chat-answer-action" type="button" onClick={() => rateAnswer("useful")}
          disabled={actionsDisabled || !storageKey} aria-label="Respuesta útil" aria-pressed={feedback === "useful"}
          title="Útil (se guarda solo en este navegador)">
          <ThumbsUp size={14} aria-hidden="true" />
        </button>
        <button className="chat-answer-action" type="button" onClick={() => rateAnswer("not-useful")}
          disabled={actionsDisabled || !storageKey} aria-label="Respuesta no útil" aria-pressed={feedback === "not-useful"}
          title="No útil (se guarda solo en este navegador)">
          <ThumbsDown size={14} aria-hidden="true" />
        </button>
        <button className="chat-answer-action" type="button" onClick={toggleSources}
          disabled={actionsDisabled} aria-expanded={sourcesOpen} aria-controls={sourcesId}
          title="Ver los enlaces citados en esta respuesta">
          <Link2 size={14} aria-hidden="true" /><span>{sourcesOpen ? "Ocultar fuentes" : "Ver fuentes"}</span>
        </button>
      </div>
    </div>
    <span className="chat-sr-only" role="status">{feedbackStatus}</span>
    <section id={sourcesId} className="chat-answer-sources" aria-label="Enlaces citados" hidden={!sourcesOpen}>
      <strong>Enlaces citados</strong>
      {sources.length ? <ul>{sources.map(source => <li key={source.href}>
        <a href={source.href} target="_blank" rel="noopener noreferrer">{source.label}</a>
        <span>{source.host}</span>
      </li>)}</ul> : <p>Esta respuesta no incluye enlaces a fuentes.</p>}
    </section>
    {copyState === "error" && <p className="chat-copy-error" role="alert">
      No se pudo copiar. Puedes seleccionar el texto y copiarlo manualmente.
    </p>}
  </>;
}
