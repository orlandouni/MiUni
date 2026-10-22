import { Link } from "react-router-dom";
import AuthExplorer from "../components/AuthExplorer";
import LoginIllustration from "../components/LoginIllustration";
import "./Auth.css";

export default function CampusPanel({ children, animateIllustration = false }) {
  return (
    <main className="miuni-auth">
      <div className="ma-layout">
        <section className="ma-left">
          <header className="ma-brand">
            <Link to="/" aria-label="MIUNI, volver al mapa">
              <span className="ma-brand-mark">M</span><span className="ma-brand-name">MIUNI<small>Universidad de Sonora</small></span>
            </Link>

            <p>
              Conecta<br />
              Explora<br />
              Avanza
            </p>
          </header>

          <div className="ma-center">{children}</div>

          <footer className="ma-footer">
            <img
              src="/auth/unison-silhouette.png"
              alt="Silueta del edificio principal de la Universidad de Sonora"
            />

            <div className="ma-motto">
              <span />
              <p>Tu universidad, siempre contigo</p>
              <span />
            </div>
          </footer>
        </section>

        <section className="ma-right" aria-label="Conoce MIUNI">
          {animateIllustration ? <LoginIllustration /> : <AuthExplorer />}
        </section>
      </div>
    </main>
  );
}
