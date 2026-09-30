import { Link } from "react-router-dom";
import CampusMap from "../components/CampusMap";

function Home() {
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
        <Link to="/login">Iniciar sesión</Link>
      </nav>

      <CampusMap />
    </div>
  );
}

export default Home;