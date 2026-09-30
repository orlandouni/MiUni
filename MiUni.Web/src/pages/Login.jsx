import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  ArrowRight,
  Eye,
  EyeOff,
  GraduationCap,
  LockKeyhole,
  Mail,
} from "lucide-react";

import { loginRequest, setSession } from "../services/api";
import CampusPanel from "./CampusPanel";

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
    <CampusPanel>
      <section className="ma-card" aria-labelledby="login-title">
        <div className="ma-heading">
          <div className="ma-emblem">
            <GraduationCap aria-hidden="true" />
          </div>

          <h1 id="login-title">Bienvenido a MIUNI</h1>
          <p>Ingresa tus credenciales para continuar</p>
        </div>

        <form
          className="ma-form"
          onSubmit={handleSubmit}
          aria-busy={loading}
        >
          <div className="ma-field">
            <label htmlFor="correo">Correo institucional</label>

            <div className="ma-input-wrap">
              <Mail className="ma-input-icon" size={20} aria-hidden="true" />

              <input
                id="correo"
                name="email"
                type="email"
                placeholder="ejemplo@unison.mx"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={loading}
                required
              />
            </div>
          </div>

          <div className="ma-field">
            <label htmlFor="contrasena">Contraseña</label>

            <div className="ma-input-wrap">
              <LockKeyhole
                className="ma-input-icon"
                size={20}
                aria-hidden="true"
              />

              <input
                id="contrasena"
                name="password"
                className="ma-password"
                type={showPassword ? "text" : "password"}
                placeholder="••••••••"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={loading}
                required
              />

              <button
                type="button"
                className="ma-eye"
                onClick={() => setShowPassword((value) => !value)}
                aria-label={
                  showPassword ? "Ocultar contraseña" : "Mostrar contraseña"
                }
                aria-pressed={showPassword}
                disabled={loading}
              >
                {showPassword ? <EyeOff size={19} /> : <Eye size={19} />}
              </button>
            </div>
          </div>

          {error && (
            <p className="ma-error" role="alert">
              {error}
            </p>
          )}

          <button
            type="submit"
            className="ma-submit"
            disabled={loading}
          >
            <span>{loading ? "Ingresando…" : "Iniciar sesión"}</span>
            <ArrowRight size={20} aria-hidden="true" />
          </button>
        </form>

        <div className="ma-divider" aria-hidden="true">
          <span />
          <span>o</span>
          <span />
        </div>

        <p className="ma-alternative">
          ¿No tienes cuenta?{" "}
          <Link to="/registro">Regístrate aquí</Link>
        </p>
      </section>
    </CampusPanel>
  );
}