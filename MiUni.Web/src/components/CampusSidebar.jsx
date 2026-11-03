import { Link, NavLink } from "react-router-dom";
import { Building2, Compass, House, LogOut, Map, MessageSquare, ShieldCheck, Star, UserRound, X } from "lucide-react";
import { ROLES } from "../services/roles";

export default function CampusSidebar({ user, open = false, onClose = () => {}, onLogout }) {
  const navClass = ({ isActive }) => `home-nav-item${isActive ? " activo" : ""}`;
  const closeOnMobile = () => { if (window.matchMedia("(max-width: 700px)").matches) onClose(); };

  return <>
    <aside className={`home-sidebar ${open ? "abierto" : ""}`}>
      <div className="home-logo">
        <div className="home-logo-mark">M</div>
        <div><strong>MIUNI</strong><span>Tu campus, a la mano</span></div>
        <button className="home-close" onClick={onClose} aria-label="Cerrar menú"><X size={18} /></button>
      </div>
      <div className="home-nav-group">
        <span className="home-nav-label">Navegación</span>
        <NavLink to="/" end className={navClass} onClick={closeOnMobile}><House size={17} /> Inicio</NavLink>
        <NavLink to="/mapa" className={navClass} onClick={closeOnMobile}><Map size={17} /> Mapa del campus</NavLink>
        <NavLink to="/asistente" className={navClass} onClick={closeOnMobile}><Compass size={17} /> Asistente MIUNI</NavLink>
      </div>
      <div className="home-nav-group">
        <span className="home-nav-label">Mi espacio</span>
        <NavLink to="/perfil" className={navClass} onClick={closeOnMobile}><UserRound size={17} /> Mi perfil</NavLink>
        <NavLink to="/favoritos" className={navClass} onClick={closeOnMobile}><Star size={17} /> Mis favoritos</NavLink>
        <NavLink to="/resenas" className={navClass} onClick={closeOnMobile}><MessageSquare size={17} /> Mis reseñas y reportes</NavLink>
      </div>
      {user && <div className="home-nav-group">
        <span className="home-nav-label">Gestión</span>
        <NavLink to="/negocios" className={navClass} onClick={closeOnMobile}><Building2 size={17} /> Mis negocios</NavLink>
        {user.roles?.includes(ROLES.admin) && <NavLink to="/admin" className={navClass} onClick={closeOnMobile}><ShieldCheck size={17} /> Administración</NavLink>}
      </div>}
      <div className="home-sidebar-bottom">
        {user ? <button className="home-logout" onClick={onLogout}><LogOut size={16} /> Cerrar sesión</button> : <Link className="home-login" to="/login" onClick={closeOnMobile}><UserRound size={16} /> Iniciar sesión</Link>}
      </div>
    </aside>
    {open && <button className="home-overlay" aria-label="Cerrar menú" onClick={onClose} />}
  </>;
}
