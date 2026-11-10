import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Bookmark, BookOpen, ChevronRight, Clock3, Compass, GraduationCap, Menu, MessageSquare, Pencil, Save, Star, Trash2, UserRound } from "lucide-react";
import api, { clearSession, getMe } from "../services/api";
import { obtenerFavoritos, obtenerLugares, obtenerResenas, editarResena, eliminarResena, mensajeError } from "../services/comunidad";
import CampusSidebar from "../components/CampusSidebar";
import KardexSeccion from "../components/KardexSeccion";
import HorarioSeccion from "../components/HorarioSeccion";
import "./Home.css";
import "./Perfil.css";

const fecha = (value) => value ? new Intl.DateTimeFormat("es-MX", { dateStyle: "medium" }).format(new Date(value)) : "";

export default function Perfil() {
  const navigate = useNavigate();
  const [datos, setDatos] = useState(null);
  const [carreras, setCarreras] = useState([]);
  const [favoritos, setFavoritos] = useState([]);
  const [resenas, setResenas] = useState([]);
  const [lugares, setLugares] = useState([]);
  const [sesiones, setSesiones] = useState([]);
  const [edicion, setEdicion] = useState(false);
  const [form, setForm] = useState({ nombre: "", carreraId: "", semestre: "" });
  const [resenaEdit, setResenaEdit] = useState(null);
  const [error, setError] = useState("");
  const [cargando, setCargando] = useState(true);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  async function cargar() {
    setError("");
    try {
      const [me, carrerasData, favoritosData, resenasData, lugaresData, historial] = await Promise.all([
        getMe(), api.get("/api/Carrera").then(r => r.data), obtenerFavoritos(), obtenerResenas(), obtenerLugares(), api.get("/api/Chat/sessions").then(r => r.data),
      ]);
      setDatos(me); setForm({ nombre: me.nombre ?? "", carreraId: me.carreraId ?? "", semestre: me.semestre ?? "" });
      setCarreras(carrerasData); setFavoritos(favoritosData); setLugares(lugaresData);
      setResenas(resenasData.filter(r => r.usuarioId?.toLowerCase() === me.id?.toLowerCase())); setSesiones(historial);
    } catch (err) { setError(mensajeError(err)); }
    finally { setCargando(false); }
  }
  useEffect(() => { void Promise.resolve().then(cargar); }, []);

  async function guardarPerfil(e) {
    e.preventDefault(); setError("");
    try {
      await api.put("/api/Auth/profile", { nombre: form.nombre.trim(), carreraId: form.carreraId || null, semestre: form.semestre ? Number(form.semestre) : null });
      setEdicion(false); setCargando(true); await cargar();
    } catch (err) { setError(mensajeError(err)); }
  }
  async function borrarResena(id) {
    if (!window.confirm("¿Eliminar esta reseña? Esta acción no se puede deshacer.")) return;
    try { await eliminarResena(id); setResenas(items => items.filter(r => r.id !== id)); }
    catch (err) { setError(mensajeError(err)); }
  }
  async function guardarResena(e) {
    e.preventDefault();
    try { await editarResena(resenaEdit.id, resenaEdit.calificacion, resenaEdit.comentario ?? ""); setResenas(items => items.map(r => r.id === resenaEdit.id ? { ...r, ...resenaEdit } : r)); setResenaEdit(null); }
    catch (err) { setError(mensajeError(err)); }
  }
  const nombreLugar = id => lugares.find(l => l.id?.toLowerCase() === id?.toLowerCase())?.nombre ?? "Lugar no disponible";
  const cerrarSesion = () => { clearSession(); navigate("/login", { replace: true }); };
  const iniciales = (datos?.nombre || datos?.email || "U").slice(0, 2).toUpperCase();

  return <div className="home-app perfil-app">
    <a href="#perfil-main" className="skip-link">Saltar al contenido</a>
    <CampusSidebar user={datos} open={sidebarOpen} onClose={() => setSidebarOpen(false)} onLogout={cerrarSesion} />
    <div className="home-main">
      <header className="home-topbar">
        <button className="home-menu" onClick={() => setSidebarOpen(true)} aria-label="Abrir menú"><Menu size={19} /></button>
        <div className="home-top-brand"><span className="home-top-icon"><BookOpen size={16} /></span><strong>MIUNI</strong></div>
        <span className="home-top-context">Mi espacio · Perfil</span>
        <Link to="/perfil" className="home-account" aria-label="Mi perfil" title="Mi perfil"><span>{iniciales}</span><ChevronRight size={15} /></Link>
      </header>
      <main className="home-content perfil-content" id="perfil-main">
        <div className="perfil-heading"><div><span className="home-section-label"><UserRound size={14}/> Cuenta universitaria</span><h1>Mi perfil</h1><p>Tu información y actividad en MiUni.</p></div></div>
        {error && <div className="perfil-alerta" role="alert"><span>{error}</span><button type="button" onClick={() => { setCargando(true); void cargar(); }} disabled={cargando}>Reintentar</button></div>}
        {cargando ? <p className="perfil-estado" role="status">Cargando perfil…</p> : datos && <>
          <section className="perfil-banner">
            <div className="perfil-banner-avatar"><UserRound size={30}/></div>
            <div className="perfil-banner-identidad"><span>ESTUDIANTE MIUNI</span><h2>{datos.nombre}</h2><p>{datos.email}</p></div>
            <button className="perfil-editar" type="button" onClick={() => setEdicion(v => !v)}><Pencil size={16}/>{edicion ? "Cancelar edición" : "Editar perfil"}</button>
          </section>
          {edicion ? <form className="perfil-edicion" onSubmit={guardarPerfil}>
            <div className="perfil-campo"><label htmlFor="perfil-nombre">Nombre</label><input id="perfil-nombre" required minLength={2} maxLength={120} value={form.nombre} onChange={e => setForm({...form,nombre:e.target.value})}/></div>
            <div className="perfil-campo"><label htmlFor="perfil-carrera">Carrera</label><select id="perfil-carrera" value={form.carreraId} onChange={e => setForm({...form,carreraId:e.target.value})}><option value="">Selecciona tu carrera</option>{carreras.map(c=><option key={c.id} value={c.id}>{c.nombre}</option>)}</select></div>
            <div className="perfil-campo"><label htmlFor="perfil-semestre">Semestre</label><select id="perfil-semestre" value={form.semestre} onChange={e => setForm({...form,semestre:e.target.value})}><option value="">Selecciona tu semestre</option>{Array.from({length:12},(_,i)=><option key={i+1} value={i+1}>{i+1}° semestre</option>)}</select></div>
            <button className="perfil-guardar"><Save size={16}/> Guardar cambios</button>
          </form> : <div className="perfil-academico">
            <article><span><GraduationCap size={16}/> CARRERA</span><strong>{carreras.find(c=>c.id?.toLowerCase()===datos.carreraId?.toLowerCase())?.nombre ?? "Sin especificar"}</strong></article>
            <article><span><BookOpen size={16}/> SEMESTRE</span><strong>{datos.semestre ? `${datos.semestre}° semestre` : "Sin especificar"}</strong></article>
            <article><span><Clock3 size={16}/> EN MIUNI DESDE</span><strong>{fecha(datos.fechaRegistro)}</strong></article>
          </div>}
          <HorarioSeccion />
          <KardexSeccion />
          <div className="perfil-secciones">
            <section id="mis-favoritos" className="perfil-seccion"><header><h2><Bookmark size={18}/> Mis favoritos</h2><span>{favoritos.length}</span></header>{favoritos.length ? <div className="perfil-lista">{favoritos.map(f=><Link className="perfil-item" key={f.lugarId} to={`/lugares/${f.lugarId}`}><span className="perfil-item-icon"><Star size={17}/></span><span className="perfil-item-text"><strong>{nombreLugar(f.lugarId)}</strong><small>Guardado {fecha(f.fechaAgregado)}</small></span><ChevronRight size={17}/></Link>)}</div> : <p className="perfil-vacio">Aún no guardas lugares. Marca tus favoritos desde el mapa.</p>}</section>
            <section id="mis-resenas" className="perfil-seccion"><header><h2><MessageSquare size={18}/> Mis reseñas</h2><span>{resenas.length}</span></header>{resenas.length ? <div className="perfil-lista">{resenas.map(r=><article className="perfil-resena" key={r.id}>{resenaEdit?.id===r.id ? <form onSubmit={guardarResena}><strong>{nombreLugar(r.lugarId)}</strong><div className="perfil-campo"><label>Calificación</label><select required value={resenaEdit.calificacion} onChange={e=>setResenaEdit({...resenaEdit,calificacion:Number(e.target.value)})}>{[5,4,3,2,1].map(n=><option key={n} value={n}>{n} estrellas</option>)}</select></div><div className="perfil-campo"><label>Comentario</label><textarea maxLength={2000} value={resenaEdit.comentario ?? ""} onChange={e=>setResenaEdit({...resenaEdit,comentario:e.target.value})}/></div><div className="perfil-resena-acciones"><button className="perfil-guardar">Guardar</button><button className="perfil-cancelar" type="button" onClick={()=>setResenaEdit(null)}>Cancelar</button></div></form> : <><div className="perfil-resena-top"><div><Link to={`/lugares/${r.lugarId}`}><strong>{nombreLugar(r.lugarId)}</strong></Link><div className="perfil-estrellas" aria-label={`${r.calificacion} de 5 estrellas`}>{"★".repeat(r.calificacion)}{"☆".repeat(5-r.calificacion)}</div></div><div className="perfil-resena-acciones"><button type="button" aria-label="Editar reseña" title="Editar reseña" onClick={()=>setResenaEdit({...r})}><Pencil size={16}/></button><button type="button" aria-label="Eliminar reseña" title="Eliminar reseña" onClick={()=>borrarResena(r.id)}><Trash2 size={16}/></button></div></div>{r.comentario && <p>{r.comentario}</p>}<small>{fecha(r.fechaCreacion)}</small></>}</article>)}</div> : <p className="perfil-vacio">Cuando escribas una reseña, la encontrarás aquí.</p>}</section>
            <section id="mi-historial" className="perfil-seccion"><header><h2><Clock3 size={18}/> Historial del asistente</h2><span>{sesiones.length}</span></header>{sesiones.length ? <div className="perfil-lista">{sesiones.map(s=><Link className="perfil-item" key={s.sesionId} to={`/asistente?sesion=${s.sesionId}`}><span className="perfil-item-icon"><Compass size={17}/></span><span className="perfil-item-text"><strong>{s.preview ?? "Conversación con el asistente"}</strong><small>{fecha(s.ultimaActividad)}</small></span><ChevronRight size={17}/></Link>)}</div> : <p className="perfil-vacio">Tus conversaciones con el asistente aparecerán aquí.</p>}</section>
          </div>
        </>}
      </main>
    </div>
  </div>;
}
