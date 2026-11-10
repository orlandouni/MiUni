import { useEffect, useState } from "react";
import { Link, Navigate, useLocation } from "react-router-dom";
import { getMe, isAuthenticated } from "../services/api";

export default function ProtectedRoute({ role, children }) {
  const location = useLocation();
  const [state, setState] = useState({ loading: true, pathname: null, attempt: -1 });
  const [intento, setIntento] = useState(0);
  useEffect(() => {
    let active = true;
    getMe().then(user => { if (active) setState({ user, pathname: location.pathname, attempt: intento }); })
      .catch(error => { if (active) setState({ error, pathname: location.pathname, attempt: intento }); });
    return () => { active = false; };
  }, [location.pathname, intento]);
  if (!isAuthenticated() || state.error?.response?.status === 401)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (state.loading || state.pathname !== location.pathname || state.attempt !== intento) return <main className="route-state" role="status"><span className="route-state-mark">M</span><p>Verificando tu acceso…</p></main>;
  if (state.error) return <main className="route-state" role="alert"><span className="route-state-mark">!</span><h1>No pudimos verificar tu sesión</h1><p>Comprueba tu conexión e inténtalo de nuevo.</p><button type="button" className="ui-button" onClick={() => setIntento(value => value + 1)}>Reintentar</button></main>;
  // `role` admite un rol o una lista de roles permitidos.
  const permitidos = role ? [role].flat() : [];
  if (permitidos.length && !permitidos.some(r => state.user.roles?.includes(r)))
    return <main className="route-state"><span className="route-state-mark">!</span><h1>Acceso restringido</h1><p>Tu cuenta no tiene permiso para entrar aquí.</p><Link className="ui-button secondary" to="/">Volver al inicio</Link></main>;
  return children;
}
