import { Link } from "react-router-dom";
import { useEffect, useState } from "react";
import { clearSession, getMe, isAuthenticated } from "../services/api";
import { ROLES } from "../services/roles";
import CampusMap from "../components/CampusMap";

function Home() {
  const [user, setUser] = useState(null);
  useEffect(() => {
    let active = true;
    if (isAuthenticated()) getMe().then(data => { if (active) setUser(data); }).catch(() => {});
    return () => { active = false; };
  }, []);
  return (
    <div>
      <h1>MiUni</h1>
      <p>Bienvenido a MiUni</p>

      <nav
        aria-label="Navegación principal"
        style={{
          display: "flex",
          justifyContent: "center",
          flexWrap: "wrap",
          gap: 18,
          padding: "18px 12px",
        }}
      >
        <Link to="/favoritos">Mis favoritos</Link>
        <Link to="/resenas">Mis reseñas</Link>
        {user ? <>
          <Link to="/negocios">Mis negocios / Registrar negocio</Link>
          {user.roles?.includes(ROLES.admin) && <Link to="/admin">Administración</Link>}
          <button onClick={() => { clearSession(); setUser(null); }}>Cerrar sesión</button>
        </> : <Link to="/login">Iniciar sesión</Link>}
      </nav>

      <CampusMap />
    </div>
  );
}

export default Home;
