import { Link, useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import { clearSession, getMe, isAuthenticated } from "../services/api";
import { ROLES } from "../services/roles";
import CampusMap from "../components/CampusMap";
import {
  BookOpen,
  Building2,
  ChevronRight,
  Compass,
  Flag,
  GraduationCap,
  LogOut,
  Map,
  MapPinned,
  Menu,
  MessageSquare,
  Route,
  Search,
  ShieldCheck,
  Sparkles,
  Star,
  Store,
  UserRound,
  X,
} from "lucide-react";
import "./Home.css";

const accesos = [
  { icon: Search, label: "Buscar lugares", text: "Edificios, aulas y servicios", href: "#mapa" },
  { icon: Route, label: "Calcular ruta", text: "Elige origen y destino en el mapa", href: "#mapa" },
  { icon: Flag, label: "Reportar", text: "Avisa sobre lugares o reseñas", href: "/resenas-reportes" },
  { icon: Store, label: "Negocios", text: "Consulta y administra negocios", href: "/negocios" },
];

const populares = [
  "Biblioteca",
  "Cafeterías",
  "Baños",
  "Servicios escolares",
  "Áreas de estudio",
];

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
          <Link to="/" className="home-nav-item activo"><Map size={17} /> Mapa del campus</Link>
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

        <main className="home-content">
          <section className="home-hero" aria-labelledby="home-title">
            <div className="home-hero-copy">
              <span className="home-kicker"><Sparkles size={14} /> Universidad de Sonora</span>
              <h1 id="home-title">Hola, {nombre}. ¿A dónde quieres ir hoy?</h1>
              <p>Encuentra lugares del campus, calcula rutas y entra rápido a las herramientas que más usas.</p>
              <a className="home-search-link" href="#mapa">
                <Search size={18} />
                Buscar en el mapa del campus
                <ChevronRight size={17} />
              </a>
            </div>

            <div className="home-campus-signal" aria-hidden="true">
              <div className="home-campus-card">
                <GraduationCap size={28} />
                <strong>MIUNI</strong>
                <span>Mapa, rutas y comunidad</span>
              </div>
              <div className="home-route-line" />
              <span className="home-pin home-pin-a"><MapPinned size={17} /></span>
              <span className="home-pin home-pin-b"><Building2 size={17} /></span>
              <span className="home-pin home-pin-c"><BookOpen size={17} /></span>
            </div>
          </section>

          <section className="home-quick-grid" aria-label="Accesos rápidos">
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

          <section className="home-popular" aria-label="Lugares populares">
            <div>
              <span className="home-section-label">Lugares populares</span>
              <h2>Empieza por lo más buscado</h2>
            </div>
            <div className="home-chip-list">
              {populares.map((lugar) => <a href="#mapa" key={lugar}>{lugar}</a>)}
            </div>
          </section>

          <section className="home-map-panel" id="mapa" aria-label="Mapa del campus">
            <div className="home-map-heading">
              <div>
                <span className="home-section-label">Mapa interactivo</span>
                <h2>Explora el campus</h2>
              </div>
              <p>Busca un punto, filtra por categoría y selecciona origen o destino para calcular una ruta.</p>
            </div>
            <div className="home-map-area"><CampusMap /></div>
          </section>
        </main>
      </div>
    </div>
  );
}

export default Home;
