import { Link, useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import { clearSession, getMe, isAuthenticated } from "../services/api";
import { ROLES } from "../services/roles";
import CampusMap from "../components/CampusMap";
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
} from "lucide-react";
import "./Home.css";

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
      <svg className="home-owlbot" viewBox="0 0 420 340" role="img">
        <defs>
          <linearGradient id="owlBody" x1="120" x2="300" y1="70" y2="285" gradientUnits="userSpaceOnUse">
            <stop stopColor="#ffffff" />
            <stop offset="1" stopColor="#dce8ff" />
          </linearGradient>
          <linearGradient id="owlBlue" x1="118" x2="305" y1="92" y2="290" gradientUnits="userSpaceOnUse">
            <stop stopColor="#0a4fb9" />
            <stop offset="1" stopColor="#052f86" />
          </linearGradient>
          <filter id="owlShadow" x="50" y="30" width="320" height="300" colorInterpolationFilters="sRGB" filterUnits="userSpaceOnUse">
            <feDropShadow dx="0" dy="18" floodColor="#173765" floodOpacity=".18" stdDeviation="14" />
          </filter>
        </defs>

        <path className="home-mascot-circuit circuit-a" d="M68 104h46l22 22" />
        <path className="home-mascot-circuit circuit-b" d="M330 92h-38l-24 24" />
        <path className="home-mascot-circuit circuit-c" d="M338 248h-42l-20-20" />
        <circle className="home-mascot-node" cx="65" cy="104" r="6" />
        <circle className="home-mascot-node" cx="334" cy="92" r="6" />
        <circle className="home-mascot-node gold" cx="342" cy="248" r="6" />

        <g filter="url(#owlShadow)">
          <path className="home-wing left" d="M113 176c-34 19-52 55-40 88 37-2 64-23 76-60z" />
          <path className="home-wing right" d="M307 176c34 19 52 55 40 88-37-2-64-23-76-60z" />
          <path className="home-ear left" d="M142 93 104 60l9 62z" />
          <path className="home-ear right" d="M278 93l38-33-9 62z" />
          <path className="home-body" d="M104 163c0-73 49-118 106-118s106 45 106 118c0 78-42 132-106 132s-106-54-106-132z" />
          <path className="home-face-panel" d="M131 159c0-48 34-82 79-82s79 34 79 82c0 51-31 88-79 88s-79-37-79-88z" />
          <circle className="home-eye-ring" cx="177" cy="151" r="42" />
          <circle className="home-eye-ring" cx="243" cy="151" r="42" />
          <circle className="home-eye" cx="177" cy="151" r="23" />
          <circle className="home-eye" cx="243" cy="151" r="23" />
          <circle className="home-eye-light" cx="168" cy="142" r="7" />
          <circle className="home-eye-light" cx="234" cy="142" r="7" />
          <path className="home-beak" d="M210 169 226 190h-32z" />
          <path className="home-chest" d="M170 219c18 17 62 17 80 0 1 33-14 54-40 54s-41-21-40-54z" />
          <path className="home-chest-line" d="M186 237h48M196 253h28" />
          <path className="home-headband" d="M160 79c28-15 72-15 100 0" />
          <circle className="home-antenna-dot" cx="210" cy="52" r="8" />
          <path className="home-antenna" d="M210 75V58" />
        </g>

        <g className="home-mascot-badges">
          <rect x="72" y="56" width="74" height="34" rx="12" />
          <path d="M92 73h34M92 80h21" />
          <rect x="275" y="267" width="74" height="34" rx="12" />
          <path d="M295 284h34M295 291h21" />
        </g>
      </svg>
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

        <main className="home-content">
          <section className="home-hero" aria-labelledby="home-title">
            <div className="home-hero-copy">
              <span className="home-kicker"><Sparkles size={14} /> Universidad de Sonora</span>
              <h1 id="home-title">Hola, {nombre}. ¿Qué quieres hacer hoy?</h1>
              <p>Encuentra lugares del campus, calcula rutas y entra rápido a las herramientas que más usas.</p>
              <Link className="home-search-link" to="/mapa">
                <Search size={18} />
                Buscar en el mapa del campus
                <ChevronRight size={17} />
              </Link>
            </div>

            <MascotaMiuni />
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
              {populares.map(({ icon: Icon, label }) => (
                <Link to="/mapa" key={label}>
                  <Icon size={15} />
                  {label}
                </Link>
              ))}
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
