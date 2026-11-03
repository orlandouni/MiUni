import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowLeft, Bike, CheckCircle2, Circle, Clock, ExternalLink, Eye, EyeOff, Heart, Info, MapPin,
  MessageSquare, Pencil, Plus, Save, Star, Store, Trash2, UtensilsCrossed, X, XCircle,
} from "lucide-react";
import { getMe } from "../services/api";
import { canManageBusiness } from "../services/roles";
import { negocioError } from "../services/negocioErrors";
import { DIAS, negociosApi, precio } from "../services/negocios";
import SelectorUbicacion from "../components/SelectorUbicacion";
import "./Negocios.css";

const ESTADO = {
  Pendiente: { tono: "pendiente", texto: "En revisión" },
  Aprobada: { tono: "aprobada", texto: "Publicado" },
  Rechazada: { tono: "rechazada", texto: "Rechazado" },
};

const fecha = v => new Intl.DateTimeFormat("es-MX", { dateStyle: "medium" }).format(new Date(v));

export default function Negocios() {
  const [estado, setEstado] = useState({ cargando: true, negocios: [], categorias: [], usuario: null, error: "" });
  const [seleccion, setSeleccion] = useState(null);
  const [creando, setCreando] = useState(false);
  const [aviso, setAviso] = useState("");

  useEffect(() => {
    let activo = true;
    Promise.all([negociosApi.mios(), negociosApi.categorias(), getMe()])
      .then(([negocios, categorias, usuario]) => {
        if (!activo) return;
        setEstado({ cargando: false, negocios, categorias, usuario, error: "" });
        setSeleccion(negocios[0]?.id ?? null);
      })
      .catch(e => { if (activo) setEstado(s => ({ ...s, cargando: false, error: negocioError(e) })); });
    return () => { activo = false; };
  }, []);

  const reemplazar = useCallback(negocio =>
    setEstado(s => ({ ...s, negocios: s.negocios.map(n => n.id === negocio.id ? negocio : n) })), []);

  function creado(negocio) {
    setEstado(s => ({ ...s, negocios: [...s.negocios, negocio] }));
    setSeleccion(negocio.id); setCreando(false);
    setAviso("Solicitud enviada. La administración revisará tu negocio antes de publicarlo en el mapa.");
  }

  const { cargando, negocios, categorias, usuario, error } = estado;
  const puedeGestionar = canManageBusiness(usuario?.roles || []);
  const actual = negocios.find(n => n.id === seleccion);
  const mostrarFormulario = creando || (!cargando && negocios.length === 0);

  return <main className="ng">
    <header className="ng-top">
      <Link to="/" className="ng-volver"><ArrowLeft size={16} /> Volver al mapa</Link>
      {usuario && <span className="ng-usuario"><Store size={15} /> {usuario.nombre}</span>}
    </header>

    <div className="ng-encabezado">
      <div>
        <p className="ng-eyebrow">Panel de propietario</p>
        <h1>Mis negocios</h1>
        <p>Administra cómo aparece tu negocio en MiUni: ubicación, horario, menú y lo que opina la comunidad.</p>
      </div>
      {negocios.length > 0 && !creando &&
        <button type="button" className="ng-btn primario" onClick={() => { setCreando(true); setAviso(""); }}><Plus size={16} /> Registrar negocio</button>}
    </div>

    {error && <p className="ng-alerta error" role="alert">{error}</p>}
    {aviso && <p className="ng-alerta ok" role="status">{aviso}<button type="button" aria-label="Cerrar" onClick={() => setAviso("")}><X size={14} /></button></p>}

    {cargando ? <p className="ng-vacio" role="status">Cargando tus negocios…</p> : <div className={`ng-layout${negocios.length > 0 ? "" : " solo"}`}>
      {negocios.length > 0 && <nav className="ng-lista" aria-label="Tus negocios">
        {negocios.map(n => {
          const e = ESTADO[n.estadoSolicitud] || ESTADO.Pendiente;
          return <button type="button" key={n.id} className={!creando && n.id === seleccion ? "activo" : ""} onClick={() => { setSeleccion(n.id); setCreando(false); }}>
            <span className="ng-lista-icono">{n.esAmbulante ? <Bike size={18} /> : <Store size={18} />}</span>
            <span className="ng-lista-texto"><strong>{n.nombre}</strong><small>{n.nombreCategoria}{n.esAmbulante ? " · Ambulante" : ""}</small></span>
            <span className={`ng-chip ${e.tono}`}>{e.texto}</span>
          </button>;
        })}
      </nav>}

      <section className="ng-contenido">
        {mostrarFormulario
          ? <NuevoNegocio categorias={categorias} primero={negocios.length === 0} onCreado={creado} onCancelar={negocios.length ? () => setCreando(false) : null} />
          : actual && <PanelNegocio key={actual.id} negocio={actual} categorias={categorias} puedeGestionar={puedeGestionar} onActualizado={reemplazar} />}
      </section>
    </div>}
  </main>;
}

/* ---------- Registro ---------- */

function NuevoNegocio({ categorias, primero, onCreado, onCancelar }) {
  return <div className="ng-card">
    {primero && <ol className="ng-pasos">
      <li className="activo"><span>1</span> Registra tu negocio</li>
      <li><span>2</span> La administración lo revisa</li>
      <li><span>3</span> Aparece en el mapa</li>
    </ol>}
    <h2>{primero ? "Registra tu primer negocio" : "Registrar otro negocio"}</h2>
    <p className="ng-sub">Mientras se revisa, podrás preparar tu horario y tu menú.</p>
    <FormNegocio categorias={categorias} textoBoton="Enviar solicitud" onCancelar={onCancelar}
      onGuardar={async valores => onCreado(await negociosApi.solicitar(valores))} />
  </div>;
}

function FormNegocio({ inicial, categorias, textoBoton, onGuardar, onCancelar, soloLectura }) {
  const [valores, setValores] = useState(() => ({
    nombre: inicial?.nombre || "", descripcion: inicial?.descripcion || "",
    categoriaId: inicial?.categoriaId || "", esAmbulante: inicial?.esAmbulante || false,
    posicion: inicial ? [inicial.latitud, inicial.longitud] : null,
  }));
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");
  const set = (campo, valor) => setValores(v => ({ ...v, [campo]: valor }));

  async function enviar(e) {
    e.preventDefault();
    if (!valores.posicion) { setError(valores.esAmbulante ? "Marca en el mapa la zona donde sueles vender." : "Marca en el mapa dónde está tu negocio."); return; }
    setGuardando(true); setError("");
    try {
      await onGuardar({
        nombre: valores.nombre.trim(), descripcion: valores.descripcion.trim() || null, categoriaId: valores.categoriaId,
        esAmbulante: valores.esAmbulante, latitud: valores.posicion[0], longitud: valores.posicion[1],
      });
    } catch (err) { setError(negocioError(err)); }
    finally { setGuardando(false); }
  }

  return <form className="ng-form" onSubmit={enviar}>
    <fieldset disabled={guardando || soloLectura}>
      <div className="ng-tipo" role="radiogroup" aria-label="Tipo de negocio">
        {[
          { valor: false, icono: Store, titulo: "Local fijo", texto: "Tienes un lugar establecido" },
          { valor: true, icono: Bike, titulo: "Ambulante", texto: "Vendes recorriendo el campus" },
        ].map(({ valor, icono: Icono, titulo, texto }) =>
          <label key={titulo} className={valores.esAmbulante === valor ? "activo" : ""}>
            <input type="radio" name="tipo" checked={valores.esAmbulante === valor} onChange={() => set("esAmbulante", valor)} />
            <Icono size={20} aria-hidden="true" /><span><strong>{titulo}</strong><small>{texto}</small></span>
          </label>)}
      </div>

      <div className="ng-campos">
        <label>Nombre del negocio<input required maxLength={150} value={valores.nombre} onChange={e => set("nombre", e.target.value)} placeholder="Ej. Cafetería El Búho" /></label>
        <label>Categoría<select required value={valores.categoriaId} onChange={e => set("categoriaId", e.target.value)}>
          <option value="" disabled>Selecciona una categoría</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
        </select></label>
        <label className="ancho">Descripción<textarea maxLength={2000} rows={3} value={valores.descripcion} onChange={e => set("descripcion", e.target.value)}
          placeholder={valores.esAmbulante ? "Qué vendes y por dónde sueles andar (ej. afuera de la biblioteca por las mañanas)" : "Qué ofreces, promociones, cómo encontrarte…"} /></label>
      </div>

      <div className="ng-mapa-campo">
        <span className="ng-label">{valores.esAmbulante ? "Zona donde sueles vender" : "Ubicación del negocio"}</span>
        {valores.esAmbulante && <p className="ng-ayuda"><Info size={14} /> Marca una zona aproximada. En el mapa aparecerás como vendedor ambulante.</p>}
        <SelectorUbicacion valor={valores.posicion} ambulante={valores.esAmbulante} onCambio={p => { set("posicion", p); setError(""); }} />
      </div>

      {error && <p className="ng-alerta error" role="alert">{error}</p>}
      {!soloLectura && <div className="ng-acciones">
        {onCancelar && <button type="button" className="ng-btn" onClick={onCancelar}>Cancelar</button>}
        <button type="submit" className="ng-btn primario"><Save size={16} /> {guardando ? "Guardando…" : textoBoton}</button>
      </div>}
    </fieldset>
  </form>;
}

/* ---------- Panel de un negocio ---------- */

const PESTANAS = [
  { id: "resumen", texto: "Resumen", icono: Eye },
  { id: "info", texto: "Información", icono: Pencil },
  { id: "horario", texto: "Horario", icono: Clock },
  { id: "menu", texto: "Menú", icono: UtensilsCrossed },
  { id: "resenas", texto: "Reseñas", icono: MessageSquare },
];

function PanelNegocio({ negocio, categorias, puedeGestionar, onActualizado }) {
  const [pestana, setPestana] = useState("resumen");
  const [panel, setPanel] = useState({ cargando: true, datos: null, error: "" });
  const [aviso, setAviso] = useState("");

  useEffect(() => {
    let activo = true;
    negociosApi.panel(negocio.id)
      .then(datos => { if (activo) setPanel({ cargando: false, datos, error: "" }); })
      .catch(e => { if (activo) setPanel({ cargando: false, datos: null, error: negocioError(e) }); });
    return () => { activo = false; };
  }, [negocio.id]);

  const setDatos = fn => setPanel(p => ({ ...p, datos: fn(p.datos) }));
  const rechazado = negocio.estadoSolicitud === "Rechazada";
  const editable = puedeGestionar && !rechazado;
  const e = ESTADO[negocio.estadoSolicitud] || ESTADO.Pendiente;

  return <div className="ng-panel">
    <div className="ng-panel-cabecera">
      <div>
        <span className={`ng-chip ${e.tono}`}>{e.texto}</span>
        <h2>{negocio.nombre}</h2>
        <p className="ng-sub">{negocio.nombreCategoria}{negocio.esAmbulante ? " · Vendedor ambulante" : " · Local fijo"} · Registrado el {fecha(negocio.fechaCreacion)}</p>
      </div>
      {negocio.activo && <Link to={`/lugares/${negocio.id}`} className="ng-btn"><ExternalLink size={15} /> Ver ficha pública</Link>}
    </div>

    <EstadoBanner negocio={negocio} puedeGestionar={puedeGestionar} />
    {aviso && <p className="ng-alerta ok" role="status">{aviso}<button type="button" aria-label="Cerrar" onClick={() => setAviso("")}><X size={14} /></button></p>}

    <div className="ng-tabs" role="tablist">
      {PESTANAS.map(({ id, texto, icono: Icono }) =>
        <button type="button" role="tab" aria-selected={pestana === id} key={id} className={pestana === id ? "activo" : ""} onClick={() => { setPestana(id); setAviso(""); }}>
          <Icono size={15} /> {texto}
          {id === "resenas" && negocio.resenas > 0 && <span>{negocio.resenas}</span>}
        </button>)}
    </div>

    {panel.error && <p className="ng-alerta error" role="alert">{panel.error}</p>}
    {panel.cargando ? <p className="ng-vacio">Cargando…</p> : panel.datos && <div className="ng-tab-contenido">
      {pestana === "resumen" && <Resumen negocio={negocio} panel={panel.datos} irA={setPestana} />}
      {pestana === "info" && <div className="ng-card">
        <FormNegocio inicial={negocio} categorias={categorias} textoBoton="Guardar cambios" soloLectura={!editable}
          onGuardar={async valores => { onActualizado(await negociosApi.editar(negocio.id, valores)); setAviso("Información actualizada."); }} />
      </div>}
      {pestana === "horario" && <Horario negocio={negocio} horarios={panel.datos.horarios} editable={editable}
        onGuardado={h => { setDatos(d => ({ ...d, horarios: h })); setAviso("Horario guardado."); }} />}
      {pestana === "menu" && <Menu negocio={negocio} productos={panel.datos.productos} editable={editable}
        onCambio={productos => setDatos(d => ({ ...d, productos }))} />}
      {pestana === "resenas" && <Resenas resenas={panel.datos.resenas} />}
    </div>}
  </div>;
}

function EstadoBanner({ negocio, puedeGestionar }) {
  if (negocio.estadoSolicitud === "Pendiente") return <p className="ng-banner pendiente"><Clock size={18} />
    <span><strong>Tu solicitud está en revisión.</strong> Cuando la administración la apruebe, tu negocio aparecerá en el mapa.
      {puedeGestionar ? " Mientras tanto ya puedes preparar tu horario y tu menú." : ""}</span></p>;
  if (negocio.estadoSolicitud === "Rechazada") return <p className="ng-banner rechazada"><XCircle size={18} />
    <span><strong>La solicitud fue rechazada.</strong> Revisa que la información sea correcta y registra el negocio de nuevo.</span></p>;
  if (!negocio.activo) return <p className="ng-banner oculto"><EyeOff size={18} />
    <span><strong>Tu negocio está oculto.</strong> La administración lo retiró temporalmente del mapa.</span></p>;
  return null;
}

function Resumen({ negocio, panel, irA }) {
  const pendientes = [
    { hecho: !!negocio.descripcion, texto: "Agrega una descripción", ir: "info" },
    { hecho: panel.horarios.length > 0, texto: "Publica tu horario", ir: "horario" },
    { hecho: panel.productos.length > 0, texto: "Agrega productos a tu menú", ir: "menu" },
  ];
  const completos = pendientes.filter(p => p.hecho).length;
  return <>
    <div className="ng-kpis">
      <article><Star size={18} /><small>Calificación</small><strong>{negocio.promedio ? negocio.promedio.toFixed(1) : "—"}</strong><em>{negocio.resenas} reseña{negocio.resenas === 1 ? "" : "s"}</em></article>
      <article><Heart size={18} /><small>Favoritos</small><strong>{negocio.favoritos}</strong><em>personas lo guardaron</em></article>
      <article><UtensilsCrossed size={18} /><small>Productos</small><strong>{panel.productos.length}</strong><em>{panel.productos.filter(p => p.estaDisponible).length} disponibles</em></article>
      <article><MapPin size={18} /><small>Visibilidad</small><strong className="ng-kpi-texto">{negocio.activo ? "En el mapa" : "Oculto"}</strong><em>{negocio.esAmbulante ? "Zona aproximada" : "Ubicación fija"}</em></article>
    </div>
    <div className="ng-grid-2">
      <div className="ng-card">
        <h3>Completa tu perfil <span className="ng-progreso">{completos}/{pendientes.length}</span></h3>
        <ul className="ng-checklist">
          {pendientes.map(p => <li key={p.texto} className={p.hecho ? "hecho" : ""}>
            {p.hecho ? <CheckCircle2 size={18} /> : <Circle size={18} />}
            {p.hecho ? <span>{p.texto}</span> : <button type="button" onClick={() => irA(p.ir)}>{p.texto}</button>}
          </li>)}
        </ul>
      </div>
      <div className="ng-card">
        <h3>Reseñas recientes</h3>
        {panel.resenas.length === 0 ? <p className="ng-sub">Aún no hay reseñas.</p> :
          <ul className="ng-resenas compacto">{panel.resenas.slice(0, 3).map(r => <ResenaItem key={r.id} r={r} />)}</ul>}
        {panel.resenas.length > 3 && <button type="button" className="ng-enlace" onClick={() => irA("resenas")}>Ver todas</button>}
      </div>
    </div>
  </>;
}

function Horario({ negocio, horarios, editable, onGuardado }) {
  // Un rango por día; si había varios se toma el primero.
  const [dias, setDias] = useState(() => DIAS.map((_, i) => {
    const h = horarios.find(x => x.diaSemana === i + 1);
    return h ? { abierto: true, abre: h.horaApertura, cierra: h.horaCierre } : { abierto: false, abre: "08:00", cierra: "16:00" };
  }));
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");
  const cambiar = (i, cambio) => setDias(ds => ds.map((d, j) => j === i ? { ...d, ...cambio } : d));
  const copiarLunes = () => setDias(ds => ds.map((d, j) => j > 0 && j < 5 ? { ...ds[0] } : d));

  async function guardar() {
    setGuardando(true); setError("");
    try {
      onGuardado(await negociosApi.guardarHorarios(negocio.id,
        dias.flatMap((d, i) => d.abierto ? [{ diaSemana: i + 1, horaApertura: d.abre, horaCierre: d.cierra }] : [])));
    } catch (e) { setError(negocioError(e)); }
    finally { setGuardando(false); }
  }

  return <div className="ng-card">
    <div className="ng-card-cabecera">
      <div><h3>Horario de atención</h3><p className="ng-sub">Así lo verán los estudiantes en la ficha de tu negocio.</p></div>
      {editable && <button type="button" className="ng-btn" onClick={copiarLunes}>Copiar lunes a martes–viernes</button>}
    </div>
    <div className="ng-horario">
      {dias.map((d, i) => <div key={DIAS[i]} className={`ng-dia${d.abierto ? "" : " cerrado"}`}>
        <label className="ng-switch"><input type="checkbox" checked={d.abierto} disabled={!editable} onChange={e => cambiar(i, { abierto: e.target.checked })} /><span>{DIAS[i]}</span></label>
        {d.abierto ? <div className="ng-horas">
          <input type="time" aria-label={`${DIAS[i]} abre`} value={d.abre} disabled={!editable} onChange={e => cambiar(i, { abre: e.target.value })} />
          <span>a</span>
          <input type="time" aria-label={`${DIAS[i]} cierra`} value={d.cierra} disabled={!editable} onChange={e => cambiar(i, { cierra: e.target.value })} />
        </div> : <span className="ng-cerrado">Cerrado</span>}
      </div>)}
    </div>
    {error && <p className="ng-alerta error" role="alert">{error}</p>}
    {editable && <div className="ng-acciones"><button type="button" className="ng-btn primario" disabled={guardando} onClick={guardar}><Save size={16} /> {guardando ? "Guardando…" : "Guardar horario"}</button></div>}
  </div>;
}

const PRODUCTO_VACIO = { nombre: "", descripcion: "", precio: "", estaDisponible: true };

function Menu({ negocio, productos, editable, onCambio }) {
  const [editando, setEditando] = useState(null);
  const [ocupado, setOcupado] = useState("");
  const [error, setError] = useState("");

  async function accion(id, fn) {
    setOcupado(id); setError("");
    try { await fn(); return true; } catch (e) { setError(negocioError(e)); return false; }
    finally { setOcupado(""); }
  }

  async function guardar(valores) {
    const body = { ...valores, precio: Number(valores.precio), descripcion: valores.descripcion.trim() || null };
    const ok = await accion("form", async () => {
      if (editando.id) {
        const p = await negociosApi.editarProducto(negocio.id, editando.id, body);
        onCambio(productos.map(x => x.id === p.id ? p : x));
      } else {
        const p = await negociosApi.crearProducto(negocio.id, body);
        onCambio([...productos, p].sort((a, b) => a.nombre.localeCompare(b.nombre)));
      }
    });
    if (ok) setEditando(null);
  }

  const alternar = p => accion(p.id, async () => {
    const actualizado = await negociosApi.editarProducto(negocio.id, p.id, { ...p, estaDisponible: !p.estaDisponible });
    onCambio(productos.map(x => x.id === p.id ? actualizado : x));
  });
  const eliminar = p => window.confirm(`¿Eliminar "${p.nombre}" del menú?`) && accion(p.id, async () => {
    await negociosApi.eliminarProducto(negocio.id, p.id);
    onCambio(productos.filter(x => x.id !== p.id));
  });

  return <div className="ng-card">
    <div className="ng-card-cabecera">
      <div><h3>Menú y productos</h3><p className="ng-sub">Marca como no disponible lo que se te acabó sin tener que borrarlo.</p></div>
      {editable && !editando && <button type="button" className="ng-btn primario" onClick={() => setEditando(PRODUCTO_VACIO)}><Plus size={16} /> Agregar producto</button>}
    </div>
    {error && <p className="ng-alerta error" role="alert">{error}</p>}
    {editando && <FormProducto inicial={editando} guardando={ocupado === "form"} onGuardar={guardar} onCancelar={() => setEditando(null)} />}
    {productos.length === 0 && !editando ? <p className="ng-vacio">Tu menú está vacío. Agrega tu primer producto.</p> :
      <ul className="ng-productos">
        {productos.map(p => <li key={p.id} className={p.estaDisponible ? "" : "agotado"}>
          <div className="ng-producto-info">
            <strong>{p.nombre}</strong>{p.descripcion && <small>{p.descripcion}</small>}
          </div>
          <span className="ng-precio">{precio(p.precio)}</span>
          {editable ? <div className="ng-producto-acciones">
            <button type="button" className={`ng-disponible${p.estaDisponible ? " si" : ""}`} disabled={ocupado === p.id} onClick={() => alternar(p)}>{p.estaDisponible ? "Disponible" : "Agotado"}</button>
            <button type="button" className="ng-icono" aria-label={`Editar ${p.nombre}`} onClick={() => setEditando({ ...p, descripcion: p.descripcion || "", precio: String(p.precio) })}><Pencil size={15} /></button>
            <button type="button" className="ng-icono peligro" aria-label={`Eliminar ${p.nombre}`} disabled={ocupado === p.id} onClick={() => eliminar(p)}><Trash2 size={15} /></button>
          </div> : !p.estaDisponible && <span className="ng-chip rechazada">Agotado</span>}
        </li>)}
      </ul>}
  </div>;
}

function FormProducto({ inicial, guardando, onGuardar, onCancelar }) {
  const [v, setV] = useState(inicial);
  return <form className="ng-producto-form" onSubmit={e => { e.preventDefault(); onGuardar(v); }}>
    <label>Producto<input required maxLength={150} autoFocus value={v.nombre} onChange={e => setV({ ...v, nombre: e.target.value })} placeholder="Ej. Café americano" /></label>
    <label>Precio (MXN)<input required type="number" min="0" step="0.5" value={v.precio} onChange={e => setV({ ...v, precio: e.target.value })} placeholder="0.00" /></label>
    <label className="ancho">Descripción<input maxLength={500} value={v.descripcion} onChange={e => setV({ ...v, descripcion: e.target.value })} placeholder="Opcional" /></label>
    <label className="ng-check"><input type="checkbox" checked={v.estaDisponible} onChange={e => setV({ ...v, estaDisponible: e.target.checked })} /> Disponible</label>
    <div className="ng-acciones">
      <button type="button" className="ng-btn" onClick={onCancelar}>Cancelar</button>
      <button type="submit" className="ng-btn primario" disabled={guardando}>{guardando ? "Guardando…" : inicial.id ? "Guardar" : "Agregar"}</button>
    </div>
  </form>;
}

function ResenaItem({ r }) {
  return <li>
    <div className="ng-resena-top"><strong>{r.nombreUsuario}</strong><span className="ng-estrellas" aria-label={`${r.calificacion} de 5`}>{"★".repeat(r.calificacion)}<span>{"★".repeat(5 - r.calificacion)}</span></span><time>{fecha(r.fechaCreacion)}</time></div>
    {r.comentario ? <p>{r.comentario}</p> : <p className="ng-sub">Sin comentario</p>}
  </li>;
}

function Resenas({ resenas }) {
  return <div className="ng-card">
    <h3>Lo que opina la comunidad</h3>
    {resenas.length === 0 ? <p className="ng-vacio">Cuando los estudiantes califiquen tu negocio, las reseñas aparecerán aquí.</p> :
      <ul className="ng-resenas">{resenas.map(r => <ResenaItem key={r.id} r={r} />)}</ul>}
  </div>;
}
