import { unified } from "unified";
import remarkParse from "remark-parse";
import remarkGfm from "remark-gfm";

const markdown = unified().use(remarkParse).use(remarkGfm);
const inlineNodes = new Set(["paragraph", "heading", "emphasis", "strong", "delete",
  "link", "linkReference", "tableCell"]);

// Keep visible inline text together; URLs, definitions and skipped HTML are not mentions.
export function textoVisibleMarkdown(texto) {
  function visible(node) {
    if (["definition", "html", "image", "imageReference"].includes(node.type)) return "";
    if (["text", "inlineCode", "code"].includes(node.type)) return node.value;
    if (node.type === "break") return "\n";
    const text = (node.children ?? []).map(visible).join(inlineNodes.has(node.type) ? "" : "\n\n");
    if (node.type === "link" && text === node.url && /^https?:\/\//i.test(text)) return "";
    return text;
  }
  return visible(markdown.parse(String(texto ?? "")));
}

const normalizar = texto => String(texto ?? "").normalize("NFD")
  .replace(/\p{M}/gu, "").toLocaleLowerCase("es")
  .replace(/[^\p{L}\p{N}]+/gu, " ").trim();

export function ubicacionValida(lugar) {
  return Number.isFinite(lugar?.latitud) && Math.abs(lugar.latitud) <= 90
    && Number.isFinite(lugar?.longitud) && Math.abs(lugar.longitud) <= 180;
}

function aliasesLugar(nombre) {
  const codigo = nombre.match(/^(?:edificio\s+)?(\d{1,2}[a-z]\d{0,2})(?:\s|$)/)?.[1];
  if (!codigo) return [];
  // Only derive institutional names, never degree names such as "Ingeniería Industrial".
  const dependencia = nombre.replace(
    /^(?:edificio\s+)?\d{1,2}[a-z]\d{0,2}\s+(?=(?:direccion|departamento|biblioteca|centro|oficina)\b)/,
    "",
  );
  return dependencia === nombre ? [codigo] : [codigo, dependencia];
}

export function detectarLugares(texto, catalogo = []) {
  const mensaje = ` ${textoVisibleMarkdown(texto).split(/\n{2,}/).map(normalizar).join(" | ")} `;
  const validos = catalogo.filter(lugar => lugar?.id && normalizar(lugar.nombre) && ubicacionValida(lugar));
  const propietarios = new Map();
  const candidatos = validos.map(lugar => {
    const nombre = normalizar(lugar.nombre);
    const aliases = aliasesLugar(nombre);
    for (const alias of [nombre, ...aliases]) {
      if (!propietarios.has(alias)) propietarios.set(alias, new Set());
      propietarios.get(alias).add(lugar.id);
    }
    return { lugar, nombre, aliases };
  });
  const vistos = new Set();
  return candidatos.filter(({ lugar, nombre, aliases }) => {
    if (vistos.has(lugar.id)) return false;
    const coincide = mensaje.includes(` ${nombre} `) || aliases.some(alias =>
      propietarios.get(alias).size === 1 && mensaje.includes(` ${alias} `));
    if (!coincide) return false;
    vistos.add(lugar.id);
    return true;
  }).map(({ lugar }) => lugar);
}

// Existing Markdown map links already provide the action; do not add it again.
export function lugaresEnlazados(texto) {
  const tree = markdown.parse(String(texto ?? ""));
  const definitions = new Map();
  const ids = new Set();
  function walk(node, visit) {
    visit(node);
    for (const child of node.children ?? []) walk(child, visit);
  }
  walk(tree, node => {
    if (node.type === "definition" && !definitions.has(node.identifier)) {
      definitions.set(node.identifier, node.url);
    }
  });
  walk(tree, node => {
    const href = node.type === "link" ? node.url
      : node.type === "linkReference" ? definitions.get(node.identifier) : null;
    if (!href) return;
    try {
      const url = new URL(href, "https://miuni.invalid");
      if (url.origin !== "https://miuni.invalid" || !["/", "/mapa"].includes(url.pathname)) return;
      const id = url.searchParams.get("lugar");
      if (id) ids.add(id.toLowerCase());
    } catch { /* A malformed link does not replace the catalog's valid action. */ }
  });
  return ids;
}

export function enlaceLugar(id) {
  return `/mapa?${new URLSearchParams({ lugar: id })}`;
}

// Share an in-flight request between messages; failures remain retryable.
let pendiente;
export function cargarLugaresChat() {
  if (!pendiente) {
    pendiente = fetch("/api/Lugar")
      .then(response => {
        if (!response.ok) throw new Error("No se pudieron consultar los lugares.");
        return response.json();
      })
      .then(data => {
        if (!Array.isArray(data)) throw new Error("El catálogo de lugares no es válido.");
        return data;
      })
      .finally(() => { pendiente = undefined; });
  }
  return pendiente;
}
