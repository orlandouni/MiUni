import { Link, useNavigate } from "react-router-dom";
import { useEffect, useState, lazy, Suspense } from "react";
import { ChevronRight, Map, Menu } from "lucide-react";
import { clearSession, getMe, isAuthenticated } from "../services/api";
import CampusSidebar from "../components/CampusSidebar";
import "./Home.css";

const CampusMap = lazy(() => import("../components/CampusMap"));

function MapaCampus() {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [userError, setUserError] = useState("");
  const [userAttempt, setUserAttempt] = useState(0);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  useEffect(() => {
    let active = true;
    if (isAuthenticated()) {
      getMe().then(data => { if (active) setUser(data); }).catch(() => { if (active) setUserError("No pudimos cargar tu cuenta."); });
    }
    return () => { active = false; };
  }, [userAttempt]);

  const cerrar = () => {
    clearSession();
    setUser(null);
    navigate("/login", { replace: true });
  };

  const nombre = user?.nombre || user?.email?.split("@")[0] || "Invitado";
  const initials = nombre.slice(0, 2).toUpperCase();

  return (
    <div className="home-app map-page">
      <CampusSidebar user={user} open={sidebarOpen} onClose={() => setSidebarOpen(false)} onLogout={cerrar} />

      <div className="home-main">
        <header className="home-topbar">
          <button className="home-menu" onClick={() => setSidebarOpen(true)} aria-label="Abrir menú"><Menu size={19} /></button>
          <div className="home-top-brand"><span className="home-top-icon"><Map size={16} /></span><strong>Mapa del campus</strong></div>
          <Link to={user ? "/perfil" : "/login"} className="home-account" aria-label={user ? "Ir a mi perfil" : "Iniciar sesión"} title={user ? "Mi perfil" : "Iniciar sesión"}><span>{initials}</span><ChevronRight size={15} /></Link>
        </header>

        <main className="map-page-content">
          <section className="map-page-heading">
            <div>
              <span className="home-section-label">Mapa interactivo</span>
              <h1>Explora la Universidad de Sonora</h1>
            </div>
            <p>Busca lugares, filtra categorías y calcula rutas dentro del campus.</p>
            {userError && <p className="map-account-error" role="alert">{userError} <button type="button" onClick={() => { setUserError(""); setUserAttempt(value => value + 1); }}>Reintentar</button></p>}
          </section>
          <section className="map-page-map" aria-label="Mapa del campus">
            <Suspense fallback={<div className="campus-loading" role="status"><Map size={24} /> Preparando el mapa del campus…</div>}><CampusMap /></Suspense>
          </section>
        </main>
      </div>
    </div>
  );
}

export default MapaCampus;
