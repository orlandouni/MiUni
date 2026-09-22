import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowUpRight, ArrowRight, Eye, EyeOff, Check } from "lucide-react";
import { registerRequest } from "../services/api";
import { authErrorMessage } from "../services/authErrors";
import "@fontsource/barlow-condensed/600.css";
import "@fontsource/barlow-condensed/700.css";
import "./Register.css";
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
    <main className="registration">
      <CampusPanel />

      <section className="registration-panel" aria-labelledby="registration-title">
        <div className="registration-topline"><span>COMUNIDAD UNIVERSITARIA</span><ArrowUpRight size={24} aria-hidden="true" /></div>
        <div className="registration-content">
          <p className="registration-marker"><span>01</span> PUNTO DE PARTIDA</p>
          <h1 id="registration-title">Crear cuenta</h1>
          <p className="registration-lead">El primer paso para conocer tu campus.<br />Regístrate con tu correo institucional.</p>
          {success ? <div className="registration-success" role="status"><Check size={32} /><h2>Ya tienes un lugar.</h2><p>Tu cuenta está lista. Inicia sesión para comenzar a explorar.</p><Link className="registration-submit" to="/login">Iniciar sesión <ArrowRight size={20} /></Link></div> :
            <form className="registration-form" onSubmit={handleSubmit} aria-busy={loading}>
              <div className="registration-field"><label htmlFor="nombre">Nombre completo</label><input id="nombre" name="nombre" autoComplete="name" placeholder="¿Cómo te llamas?" value={nombre} onChange={(e) => setNombre(e.target.value)} required /></div>
              <div className="registration-field"><label htmlFor="correo">Correo institucional</label><input id="correo" name="email" type="email" autoComplete="email" placeholder="tu.nombre@unison.mx" value={email} onChange={(e) => setEmail(e.target.value)} required /></div>
              <div className="registration-field"><label htmlFor="contrasena">Contraseña</label><div className="registration-password"><input id="contrasena" name="password" type={showPassword ? "text" : "password"} autoComplete="new-password" placeholder="Crea tu contraseña" value={password} onChange={(e) => setPassword(e.target.value)} minLength={8} aria-describedby="password-help" required /><button type="button" onClick={() => setShowPassword(!showPassword)} aria-label={showPassword ? "Ocultar contraseña" : "Mostrar contraseña"} aria-pressed={showPassword}>{showPassword ? <EyeOff size={19} /> : <Eye size={19} />}</button></div><p id="password-help">Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo.</p></div>
              <div className="registration-field"><label htmlFor="confirmar">Confirmar contraseña</label><div className="registration-password"><input id="confirmar" name="confirmPassword" type={showConfirm ? "text" : "password"} autoComplete="new-password" placeholder="Escríbela una vez más" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required /><button type="button" onClick={() => setShowConfirm(!showConfirm)} aria-label={showConfirm ? "Ocultar confirmación de contraseña" : "Mostrar confirmación de contraseña"} aria-pressed={showConfirm}>{showConfirm ? <EyeOff size={19} /> : <Eye size={19} />}</button></div></div>
              {error && <p className="registration-error" role="alert">{error}</p>}
              <button type="submit" className="registration-submit" disabled={loading}>{loading ? "Creando tu cuenta…" : "Crear mi cuenta"}<ArrowRight size={21} aria-hidden="true" /></button>
            </form>}
          <p className="registration-login">¿Ya eres parte? <Link to="/login">Inicia sesión <ArrowUpRight size={15} /></Link></p>
        </div>
        <footer className="registration-footer"><span>MENOS VUELTAS. MÁS CAMPUS.</span><span>MIUNI ↗</span></footer>
      </section>
    </main>
  );
}
