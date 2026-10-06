import { Link } from "react-router-dom";
import { useEffect, useState } from "react";
import { clearSession, getMe, isAuthenticated } from "../services/api";
import { ROLES } from "../services/roles";
import CampusMap from "../components/CampusMap";
import { BookOpen, Building2, ChevronRight, Compass, Flag, House, LogOut, Map, Menu, MessageSquare, ShieldCheck, Star, UserRound, X } from "lucide-react";
import "./Home.css";

function Home() {
  const [user, setUser] = useState(null);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  useEffect(() => {
    let active = true;
    if (isAuthenticated()) getMe().then(data => { if (active) setUser(data); }).catch(() => {});
    return () => { active = false; };
  }, []);
  const cerrar = () => { clearSession(); setUser(null); };
  const nombre = user?.nombre || user?.email?.split("@")[0] || "Invitado";
  const initials = nombre.slice(0, 2).toUpperCase();
  return <div className="home-app">
    <aside className={`home-sidebar ${sidebarOpen ? "abierto" : ""}`}>
      <div className="home-logo"><div className="home-logo-mark">M</div><div><strong>MIUNI</strong><span>Tu campus, a la mano</span></div><button className="home-close" onClick={() => setSidebarOpen(false)} aria-label="Cerrar menú"><X size={18} /></button></div>
      <div className="home-nav-group"><span className="home-nav-label">Navegación</span><Link to="/" className="home-nav-item activo"><Map size={17} /> Mapa del campus</Link><Link to="/" className="home-nav-item"><Compass size={17} /> Asistente MIUNI</Link></div>
      <div className="home-nav-group"><span className="home-nav-label">Mi espacio</span><Link to="/favoritos" className="home-nav-item"><Star size={17} /> Mis favoritos</Link><Link to="/resenas" className="home-nav-item"><MessageSquare size={17} /> Mis reseñas y reportes</Link></div>
      {user && <div className="home-nav-group"><span className="home-nav-label">Gestión</span><Link to="/negocios" className="home-nav-item"><Building2 size={17} /> Mis negocios</Link>{user.roles?.includes(ROLES.admin) && <Link to="/admin" className="home-nav-item"><ShieldCheck size={17} /> Administración</Link>}</div>}
      <div className="home-sidebar-bottom">{user ? <button className="home-logout" onClick={cerrar}><LogOut size={16} /> Cerrar sesión</button> : <Link className="home-login" to="/login"><UserRound size={16} /> Iniciar sesión</Link>}</div>
    </aside>
    {sidebarOpen && <button className="home-overlay" aria-label="Cerrar menú" onClick={() => setSidebarOpen(false)} />}
    <div className="home-main"><header className="home-topbar"><button className="home-menu" onClick={() => setSidebarOpen(true)} aria-label="Abrir menú"><Menu size={19} /></button><div className="home-top-brand"><span className="home-top-icon"><BookOpen size={16} /></span><strong>MIUNI</strong></div><div className="home-account"><span>{initials}</span><ChevronRight size={15} /></div></header><main className="home-map-area"><CampusMap /></main></div>
  </div>;
}

export default Home;
