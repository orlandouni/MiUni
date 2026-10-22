import { Link, useNavigate } from "react-router-dom";
import { useEffect, useState, lazy, Suspense } from "react";
import { clearSession, getMe, isAuthenticated } from "../services/api";
import { ROLES } from "../services/roles";
import MascotaIlustrada from "../components/MascotaIlustrada";
import {
  BookOpen,
  Building2,
  Coffee,
  ChevronRight,
  Compass,
  Flag,
  GraduationCap,
  House,
  LogOut,
  Map,
  Menu,
  MessageSquare,
  Route,
  Search,
  ShieldCheck,
  Sparkles,
  Star,
  Store,
  Toilet,
  UserRound,
  X,
  ArrowUpRight,
  MapPin,
} from "lucide-react";
import "./Home.css";

const CampusMap = lazy(() => import("../components/CampusMap"));

const accesos = [
  { icon: Search, label: "Buscar lugares", text: "Edificios, aulas y servicios", href: "/mapa" },
  { icon: Route, label: "Calcular ruta", text: "Elige origen y destino en el mapa", href: "/mapa" },
  { icon: Compass, label: "Chat MIUNI", text: "Pregunta al asistente del campus", href: "/asistente" },
  { icon: Flag, label: "Reportar", text: "Avisa sobre lugares o reseñas", href: "/resenas-reportes" },
  { icon: Store, label: "Negocios", text: "Consulta y administra negocios", href: "/negocios" },
];

const populares = [
  { icon: BookOpen, label: "Biblioteca" },
  { icon: Coffee, label: "Cafeterías" },
  { icon: Toilet, label: "Baños" },
  { icon: Building2, label: "Servicios escolares" },
  { icon: GraduationCap, label: "Áreas de estudio" },
];

function MascotaMiuni() {
  return (
    <div className="home-mascot" aria-hidden="true">
      <div className="home-mascot-glow" />
      <div className="home-mascot-shadow" />
      <MascotaIlustrada className="home-owl-illustration" />
      <span className="home-mascot-caption"><MapPin size={13} /> Tu guía del campus</span>
    </div>
  );
}

function Home() {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  useEffect(() => {
    let active = true;
    if (isAuthenticated()) getMe().then(data => { if (active) setUser(data); }).catch(() => {});
    return () => { active = false; };
  }, []);
  const cerrar = () => {
    clearSession();
    setUser(null);
    navigate("/login", { replace: true });
  };
  const nombre = user?.nombre || user?.email?.split("@")[0] || "Invitado";
  const initials = nombre.slice(0, 2).toUpperCase();
  return (
    <div className="home-app">
      <a href="#home-main-content" className="skip-link">Saltar al contenido</a>
      <aside className={`home-sidebar ${sidebarOpen ? "abierto" : ""}`}>
        <div className="home-logo">
          <div className="home-logo-mark">M</div>
          <div>
            <strong>MIUNI</strong>
            <span>Tu campus, a la mano</span>
          </div>
          <button className="home-close" onClick={() => setSidebarOpen(false)} aria-label="Cerrar menú"><X size={18} /></button>
        </div>
        <div className="home-nav-group">
          <span className="home-nav-label">Navegación</span>
          <Link to="/" className="home-nav-item activo"><House size={17} /> Inicio</Link>
          <Link to="/mapa" className="home-nav-item"><Map size={17} /> Mapa del campus</Link>
          <Link to="/asistente" className="home-nav-item"><Compass size={17} /> Asistente MIUNI</Link>
        </div>
        <div className="home-nav-group">
          <span className="home-nav-label">Mi espacio</span>
          <Link to="/favoritos" className="home-nav-item"><Star size={17} /> Mis favoritos</Link>
          <Link to="/resenas" className="home-nav-item"><MessageSquare size={17} /> Mis reseñas y reportes</Link>
        </div>
        {user && (
          <div className="home-nav-group">
            <span className="home-nav-label">Gestión</span>
            <Link to="/negocios" className="home-nav-item"><Building2 size={17} /> Mis negocios</Link>
            {user.roles?.includes(ROLES.admin) && <Link to="/admin" className="home-nav-item"><ShieldCheck size={17} /> Administración</Link>}
          </div>
        )}
        <div className="home-sidebar-bottom">
          {user ? (
            <button className="home-logout" onClick={cerrar}><LogOut size={16} /> Cerrar sesión</button>
          ) : (
            <Link className="home-login" to="/login"><UserRound size={16} /> Iniciar sesión</Link>
          )}
        </div>
      </aside>

      {sidebarOpen && <button className="home-overlay" aria-label="Cerrar menú" onClick={() => setSidebarOpen(false)} />}

      <div className="home-main">
        <header className="home-topbar">
          <button className="home-menu" onClick={() => setSidebarOpen(true)} aria-label="Abrir menú"><Menu size={19} /></button>
          <div className="home-top-brand"><span className="home-top-icon"><BookOpen size={16} /></span><strong>MIUNI</strong></div>
          <div className="home-account"><span>{initials}</span><ChevronRight size={15} /></div>
        </header>

        <main className="home-content" id="home-main-content">
          <div className="home-welcome"><p>Hola, <strong>{nombre}</strong>. Qué bueno verte por aquí.</p><span>Hermosillo · Sonora</span></div>
          <section className="home-hero" aria-labelledby="home-title">
            <div className="home-hero-copy">
              <span className="home-kicker"><Sparkles size={14} /> Universidad de Sonora</span>
              <h1 id="home-title">Tu campus,<br /><em>a tu ritmo.</em></h1>
              <p>De tu próxima clase a tu café favorito. Encuentra tu lugar, traza tu camino y resuelve tus dudas.</p>
              <div className="home-hero-actions">
                <Link className="ui-button yellow" to="/mapa"><Map size={17} /> Explorar el campus <ArrowUpRight size={16} /></Link>
                <Link className="home-hero-chat" to="/asistente">Preguntar a MiUni <ChevronRight size={16} /></Link>
              </div>
            </div>

            <MascotaMiuni />
          </section>

          <section data-reveal="visible" className="home-quick-grid" aria-label="Accesos rápidos">
            {accesos.map(({ icon: Icon, label, text, href }) => (
              href.startsWith("/") ? (
                <Link className="home-action" to={href} key={label}>
                  <span><Icon size={20} /></span>
                  <strong>{label}</strong>
                  <small>{text}</small>
                </Link>
              ) : (
                <a className="home-action" href={href} key={label}>
                  <span><Icon size={20} /></span>
                  <strong>{label}</strong>
                  <small>{text}</small>
                </a>
              )
            ))}
          </section>

          <section data-reveal="visible" className="home-popular" aria-label="Lugares populares">
            <div>
              <span className="home-section-label">Lugares populares</span>
              <h2>Empieza por lo más buscado</h2>
            </div>
            <div className="home-chip-list">
              {populares.map(({ icon: Icon, label }) => (
                <Link to="/mapa" key={label}>
                  <Icon size={15} />
                  {label}
                </Link>
              ))}
            </div>
          </section>

          <section data-reveal="visible" className="home-map-panel" id="mapa" aria-label="Mapa del campus">
            <div className="home-map-heading">
              <div>
                <span className="home-section-label">Mapa interactivo</span>
                <h2>Explora el campus</h2>
              </div>
              <p>Busca un punto, filtra por categoría y selecciona origen o destino para calcular una ruta.</p>
            </div>
            <div className="home-map-area"><Suspense fallback={<div className="campus-loading" role="status"><Map size={24} /> Preparando el mapa del campus…</div>}><CampusMap /></Suspense></div>
          </section>
          <section className="home-assistant-strip" data-reveal="visible">
            <div><span className="home-section-label">Menos vueltas, más respuestas</span><h2>¿Por dónde empezamos?</h2><p>Pregunta por horarios, lugares y servicios. MiUni te acompaña.</p></div>
            <Link className="ui-button secondary" to="/asistente"><MessageSquare size={17} /> Abrir el asistente <ArrowUpRight size={16} /></Link>
          </section>
        </main>
      </div>
    </div>
  );
}

export default Home;
