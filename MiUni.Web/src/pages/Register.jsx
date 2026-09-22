import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { GraduationCap, Eye, EyeOff } from "lucide-react";
import { registerRequest } from "../services/api";
import "./Auth.css";

export default function Register() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();

    setError("");
    setSuccess("");

    if (
      !email.trim() ||
      !password.trim() ||
      !confirmPassword.trim()
    ) {
      setError("Completa todos los campos.");
      return;
    }

    if (password !== confirmPassword) {
      setError("Las contraseñas no coinciden.");
      return;
    }

    setLoading(true);

    try {
      const data = await registerRequest(
        email.trim(),
        password
      );

      setSuccess(
        data.message || "Usuario registrado correctamente."
      );

      setTimeout(() => {
        navigate("/login");
      }, 1500);
    } catch (err) {
      if (err.response?.status === 400) {
        const apiErrors = err.response.data;

        if (Array.isArray(apiErrors) && apiErrors.length > 0) {
          setError(
            apiErrors
              .map(
                (item) =>
                  item.description ||
                  item.Description ||
                  "Error de registro."
              )
              .join(" ")
          );
        } else {
          setError(
            err.response.data?.message ||
              "No se pudo completar el registro."
          );
        }
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

          <h1 className="auth-title">Crear cuenta</h1>

          <p className="auth-subtitle">
            Regístrate para comenzar a usar MIUNI
          </p>
        </div>

        <form
          className="auth-form"
          onSubmit={handleSubmit}
        >
          <div className="form-group">
            <label htmlFor="correo">
              Correo institucional
            </label>

            <input
              id="correo"
              type="email"
              placeholder="ejemplo@unison.mx"
              autoComplete="email"
              value={email}
              onChange={(e) =>
                setEmail(e.target.value)
              }
            />
          </div>

          <div className="form-group">
            <label htmlFor="contrasena">
              Contraseña
            </label>

            <div className="input-wrapper">
              <input
                id="contrasena"
                type={
                  showPassword
                    ? "text"
                    : "password"
                }
                placeholder="••••••••"
                autoComplete="new-password"
                value={password}
                onChange={(e) =>
                  setPassword(e.target.value)
                }
              />

              <button
                type="button"
                className="toggle-password"
                onClick={() =>
                  setShowPassword(
                    (value) => !value
                  )
                }
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

          <div className="form-group">
            <label htmlFor="confirmar">
              Confirmar contraseña
            </label>

            <div className="input-wrapper">
              <input
                id="confirmar"
                type={
                  showConfirm
                    ? "text"
                    : "password"
                }
                placeholder="••••••••"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={(e) =>
                  setConfirmPassword(
                    e.target.value
                  )
                }
              />

              <button
                type="button"
                className="toggle-password"
                onClick={() =>
                  setShowConfirm(
                    (value) => !value
                  )
                }
                aria-label={
                  showConfirm
                    ? "Ocultar contraseña"
                    : "Mostrar contraseña"
                }
              >
                {showConfirm ? (
                  <EyeOff size={16} />
                ) : (
                  <Eye size={16} />
                )}
              </button>
            </div>
          </div>

          {error && (
            <p className="error-message">
              {error}
            </p>
          )}

          {success && (
            <p className="success-message">
              {success}
            </p>
          )}

          <button
            type="submit"
            className="btn-primary"
            disabled={loading}
          >
            {loading
              ? "Registrando..."
              : "Registrarme"}
          </button>
        </form>

        <p className="switch-link">
          ¿Ya tienes cuenta?{" "}
          <Link to="/login">
            Inicia sesión
          </Link>
        </p>
      </div>
    </div>
  );
}