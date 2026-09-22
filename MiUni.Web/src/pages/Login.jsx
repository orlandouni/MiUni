import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { GraduationCap, Eye, EyeOff } from "lucide-react";
import { loginRequest, setSession } from "../services/api";
import "./Auth.css";

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
        setError("Datos inválidos. Revisa el formulario.");
      } else if (err.request) {
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
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-header">
          <div className="auth-icon">
            <GraduationCap size={28} />
          </div>

          <h1 className="auth-title">Bienvenido a MIUNI</h1>

          <p className="auth-subtitle">
            Ingresa tus credenciales para continuar
          </p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="correo">Correo institucional</label>

            <input
              id="correo"
              type="email"
              placeholder="ejemplo@unison.mx"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <div className="form-group">
            <label htmlFor="contrasena">Contraseña</label>

            <div className="input-wrapper">
              <input
                id="contrasena"
                type={showPassword ? "text" : "password"}
                placeholder="••••••••"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />

              <button
                type="button"
                className="toggle-password"
                onClick={() => setShowPassword((value) => !value)}
                aria-label={
                  showPassword
                    ? "Ocultar contraseña"
                    : "Mostrar contraseña"
                }
              >
                {showPassword ? (
                  <EyeOff size={16} />
                ) : (
                  <Eye size={16} />
                )}
              </button>
            </div>
          </div>

          {error && <p className="error-message">{error}</p>}

          <button
            type="submit"
            className="btn-primary"
            disabled={loading}
          >
            {loading ? "Ingresando..." : "Iniciar sesión"}
          </button>
        </form>

        <p className="switch-link">
          ¿No tienes cuenta?{" "}
          <Link to="/registro">Regístrate aquí</Link>
        </p>
      </div>
    </div>
  );
}