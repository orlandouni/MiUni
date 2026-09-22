import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowUpRight, ArrowRight, Eye, EyeOff } from "lucide-react";
import { loginRequest, setSession } from "../services/api";
import CampusPanel from "./CampusPanel";
import "@fontsource/barlow-condensed/600.css";
import "@fontsource/barlow-condensed/700.css";
import "./Register.css";

export default function Login() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");

    if (!email.trim() || !password.trim()) {
      setError("Completa correo y contraseña.");
      return;
    }

    setLoading(true);

    try {
      const data = await loginRequest(email.trim(), password);

      setSession(data.token, data.expires);

      navigate("/");
    } catch (err) {
      if (err.response?.status === 401) {
        setError(
          err.response.data?.message ||
            "Correo o contraseña incorrectos."
        );
      } else if (err.response?.status === 400) {
        const apiErrors = err.response.data;
        setError(
          apiErrors?.errors
            ? Object.values(apiErrors.errors).flat().join(" ")
            : apiErrors?.message || "Datos inválidos. Revisa el formulario."
        );
      } else if (!err.response && err.request) {
        setError(
          "No se pudo conectar con el servidor. Intenta de nuevo."
        );
      } else {
        setError("Ocurrió un error inesperado.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="registration">
      <CampusPanel />
      <section className="registration-panel" aria-labelledby="login-title">
        <div className="registration-topline">
          <span>COMUNIDAD UNIVERSITARIA</span>
          <ArrowUpRight size={24} aria-hidden="true" />
        </div>
        <div className="registration-content">
          <p className="registration-marker"><span>→</span> RETOMA TU RUTA</p>
          <h1 id="login-title">Iniciar sesión</h1>
          <p className="registration-lead">
            Tu campus te espera.<br />
            Ingresa con tu correo institucional para continuar.
          </p>
          <form className="registration-form" onSubmit={handleSubmit} aria-busy={loading}>
            <div className="registration-field">
              <label htmlFor="correo">Correo institucional</label>
              <input
                id="correo"
                name="email"
                type="email"
                placeholder="tu.nombre@unison.mx"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="registration-field">
              <label htmlFor="contrasena">Contraseña</label>
              <div className="registration-password">
                <input
                  id="contrasena"
                  name="password"
                  type={showPassword ? "text" : "password"}
                  placeholder="Escribe tu contraseña"
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((value) => !value)}
                  aria-label={showPassword ? "Ocultar contraseña" : "Mostrar contraseña"}
                  aria-pressed={showPassword}
                >
                  {showPassword ? <EyeOff size={19} /> : <Eye size={19} />}
                </button>
              </div>
            </div>
            {error && <p className="registration-error" role="alert">{error}</p>}
            <button type="submit" className="registration-submit" disabled={loading}>
              {loading ? "Ingresando…" : "Iniciar sesión"}
              <ArrowRight size={21} aria-hidden="true" />
            </button>
          </form>
          <p className="registration-login">
            ¿Aún no tienes cuenta? <Link to="/registro">Regístrate aquí <ArrowUpRight size={15} /></Link>
          </p>
        </div>
        <footer className="registration-footer">
          <span>MENOS VUELTAS. MÁS CAMPUS.</span><span>MIUNI ↗</span>
        </footer>
      </section>
    </main>
  );
}
