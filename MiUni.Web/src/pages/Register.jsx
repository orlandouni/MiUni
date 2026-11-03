import { useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowRight,
  Eye,
  EyeOff,
  GraduationCap,
  LockKeyhole,
  Mail,
  MailCheck,
  Store,
  User,
} from "lucide-react";

import { registerRequest } from "../services/api";
import { ROLES } from "../services/roles";
import { authErrorMessage } from "../services/authErrors";
import CampusPanel from "./CampusPanel";
import ReenviarVerificacion from "../components/ReenviarVerificacion";

const TIPOS_CUENTA = [
  {
    valor: ROLES.usuario,
    titulo: "Estudiante",
    texto: "Explora el campus con tu correo @unison.mx",
    icono: GraduationCap,
  },
  {
    valor: ROLES.propietario,
    titulo: "Propietario",
    texto: "Publica y administra tu negocio",
    icono: Store,
  },
];

export default function Register() {
  const [tipoCuenta, setTipoCuenta] = useState(ROLES.usuario);
  const esPropietario = tipoCuenta === ROLES.propietario;
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
    if (!esPropietario && !email.trim().toLowerCase().endsWith("@unison.mx")) {
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
      await registerRequest(email.trim(), password, nombre.trim(), tipoCuenta);
      setSuccess(true);
    } catch (err) {
      setError(authErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

    return (
    <CampusPanel animateIllustration>
      <section
        className="ma-card ma-register"
        aria-labelledby="register-title"
      >
        <div className="ma-heading">
          <div className="ma-emblem">
            {esPropietario ? <Store aria-hidden="true" /> : <GraduationCap aria-hidden="true" />}
          </div>

          <h1 id="register-title">Crear cuenta</h1>
          <p>
            {esPropietario
              ? "Registra tu negocio y llega a la comunidad universitaria"
              : "Regístrate para comenzar a usar MIUNI"}
          </p>
        </div>

        {success ? (
          <div className="ma-success" role="status">
            <MailCheck size={34} aria-hidden="true" />
            <h2>Revisa tu correo</h2>
            <p>
              Enviamos un enlace de verificación a <strong>{email.trim()}</strong>.
              Ábrelo para activar tu cuenta{esPropietario ? " y después registra tu negocio" : ""}.
            </p>
            <p className="ma-help">El enlace vence en 24 horas. Si no lo ves, revisa la carpeta de spam.</p>

            <ReenviarVerificacion email={email.trim()} />

            <Link className="ma-submit" to="/login">
              <span>Ya lo verifiqué, iniciar sesión</span>
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
              <fieldset className="ma-account-type" disabled={loading}>
                <legend>Tipo de cuenta</legend>
                {TIPOS_CUENTA.map(({ valor, titulo, texto, icono: Icono }) => (
                  <label
                    key={valor}
                    className={tipoCuenta === valor ? "activo" : ""}
                  >
                    <input
                      type="radio"
                      name="tipoCuenta"
                      value={valor}
                      checked={tipoCuenta === valor}
                      onChange={() => { setTipoCuenta(valor); setError(""); }}
                    />
                    <Icono size={20} aria-hidden="true" />
                    <span>
                      <strong>{titulo}</strong>
                      <small>{texto}</small>
                    </span>
                  </label>
                ))}
              </fieldset>

              <div className="ma-field">
                <label htmlFor="nombre">
                  {esPropietario ? "Nombre del negocio" : "Nombre completo"}
                </label>

                <div className="ma-input-wrap">
                  {esPropietario ? (
                    <Store className="ma-input-icon" size={19} aria-hidden="true" />
                  ) : (
                    <User className="ma-input-icon" size={19} aria-hidden="true" />
                  )}

                  <input
                    id="nombre"
                    name="nombre"
                    autoComplete={esPropietario ? "organization" : "name"}
                    placeholder={esPropietario ? "Ej. Cafetería El Búho" : "Ej. Ana López Torres"}
                    value={nombre}
                    onChange={(e) => setNombre(e.target.value)}
                    disabled={loading}
                    required
                  />
                </div>
              </div>

              <div className="ma-field">
                <label htmlFor="correo">
                  {esPropietario ? "Correo del negocio" : "Correo institucional"}
                </label>

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
                    placeholder={esPropietario ? "negocio@correo.com" : "ejemplo@unison.mx"}
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    disabled={loading}
                    required
                  />
                </div>
              </div>

              <div className="ma-field-row">
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
                <p id="password-help" className="ma-help">
                  Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo.
                </p>
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
