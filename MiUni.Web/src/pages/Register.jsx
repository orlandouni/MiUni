import { useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowRight,
  Eye,
  EyeOff,
  Check,
  GraduationCap,
  LockKeyhole,
  Mail,
  User,
} from "lucide-react";

import { registerRequest } from "../services/api";
import { authErrorMessage } from "../services/authErrors";
import CampusPanel from "./CampusPanel";

export default function Register() {
  const [nombre, setNombre] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    if (loading || success) return;
    setError("");
    if (!nombre.trim() || !email.trim() || !password || !confirmPassword) {
      setError("Completa todos los campos.");
      return;
    }
    if (!email.trim().toLowerCase().endsWith("@unison.mx")) {
      setError("Usa tu correo institucional terminado en @unison.mx.");
      return;
    }
    if (password.length < 8) {
      setError("La contraseña debe tener al menos 8 caracteres.");
      return;
    }
    if (password !== confirmPassword) {
      setError("Las contraseñas no coinciden.");
      return;
    }
    setLoading(true);
    try {
      await registerRequest(email.trim(), password, nombre.trim());
      setSuccess(true);
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

    return (
    <CampusPanel>
      <section
        className="ma-card ma-register"
        aria-labelledby="register-title"
      >
        <div className="ma-heading">
          <div className="ma-emblem">
            <GraduationCap aria-hidden="true" />
          </div>

          <h1 id="register-title">Crear cuenta</h1>
          <p>Regístrate para comenzar a usar MIUNI</p>
        </div>

        {success ? (
          <div className="ma-success" role="status">
            <Check size={34} aria-hidden="true" />
            <h2>Cuenta creada correctamente</h2>
            <p>Ya puedes iniciar sesión con tus credenciales.</p>

            <Link className="ma-submit" to="/login">
              <span>Iniciar sesión</span>
              <ArrowRight size={20} aria-hidden="true" />
            </Link>
          </div>
        ) : (
          <>
            <form
              className="ma-form"
              onSubmit={handleSubmit}
              aria-busy={loading}
            >
              <div className="ma-field">
                <label htmlFor="nombre">Nombre completo</label>

                <div className="ma-input-wrap">
                  <User
                    className="ma-input-icon"
                    size={19}
                    aria-hidden="true"
                  />

                  <input
                    id="nombre"
                    name="nombre"
                    autoComplete="name"
                    placeholder="Ej. Ana López Torres"
                    value={nombre}
                    onChange={(e) => setNombre(e.target.value)}
                    disabled={loading}
                    required
                  />
                </div>
              </div>

              <div className="ma-field">
                <label htmlFor="correo">Correo institucional</label>

                <div className="ma-input-wrap">
                  <Mail
                    className="ma-input-icon"
                    size={19}
                    aria-hidden="true"
                  />

                  <input
                    id="correo"
                    name="email"
                    type="email"
                    autoComplete="email"
                    placeholder="ejemplo@unison.mx"
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
                    size={19}
                    aria-hidden="true"
                  />

                  <input
                    id="contrasena"
                    name="password"
                    className="ma-password"
                    type={showPassword ? "text" : "password"}
                    autoComplete="new-password"
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    minLength={8}
                    aria-describedby="password-help"
                    disabled={loading}
                    required
                  />

                  <button
                    type="button"
                    className="ma-eye"
                    onClick={() => setShowPassword((value) => !value)}
                    aria-label={
                      showPassword
                        ? "Ocultar contraseña"
                        : "Mostrar contraseña"
                    }
                    aria-pressed={showPassword}
                    disabled={loading}
                  >
                    {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  </button>
                </div>

                <p id="password-help" className="ma-help">
                  Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo.
                </p>
              </div>

              <div className="ma-field">
                <label htmlFor="confirmar">Confirmar contraseña</label>

                <div className="ma-input-wrap">
                  <LockKeyhole
                    className="ma-input-icon"
                    size={19}
                    aria-hidden="true"
                  />

                  <input
                    id="confirmar"
                    name="confirmPassword"
                    className="ma-password"
                    type={showConfirm ? "text" : "password"}
                    autoComplete="new-password"
                    placeholder="••••••••"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    disabled={loading}
                    required
                  />

                  <button
                    type="button"
                    className="ma-eye"
                    onClick={() => setShowConfirm((value) => !value)}
                    aria-label={
                      showConfirm
                        ? "Ocultar confirmación de contraseña"
                        : "Mostrar confirmación de contraseña"
                    }
                    aria-pressed={showConfirm}
                    disabled={loading}
                  >
                    {showConfirm ? <EyeOff size={18} /> : <Eye size={18} />}
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
                <span>{loading ? "Creando cuenta…" : "Registrarme"}</span>
                <ArrowRight size={20} aria-hidden="true" />
              </button>
            </form>

            <p className="ma-alternative">
              ¿Ya tienes cuenta?{" "}
              <Link to="/login">Inicia sesión</Link>
            </p>
          </>
        )}
      </section>
    </CampusPanel>
  );
}
