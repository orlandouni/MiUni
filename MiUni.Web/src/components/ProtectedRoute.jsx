import { useEffect, useState } from "react";
import { Link, Navigate, useLocation } from "react-router-dom";
import { getMe, isAuthenticated } from "../services/api";

export default function ProtectedRoute({ role, children }) {
  const location = useLocation();
  const [state, setState] = useState({ loading: true });
  useEffect(() => {
    let active = true;
    getMe().then(user => { if (active) setState({ user }); })
      .catch(error => { if (active) setState({ error }); });
    return () => { active = false; };
  }, [location.pathname]);
  if (!isAuthenticated() || state.error?.response?.status === 401)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (state.loading) return <p role="status">Verificando acceso…</p>;
  if (state.error) return <p role="alert">No pudimos verificar tu sesión. Recarga para intentar de nuevo.</p>;
  if (role && !state.user.roles?.includes(role))
    return <main><h1>Acceso restringido</h1><p>Tu cuenta no tiene permiso para entrar aquí.</p><Link to="/">Volver al inicio</Link></main>;
  return children;
}
