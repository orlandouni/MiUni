import { BrowserRouter, Routes, Route } from "react-router-dom";
import {lazy, Suspense} from "react";

import Home from "../pages/Home";
import ProtectedRoute from "../components/ProtectedRoute";
import PageMotion from "../components/PageMotion";
const MapaCampus = lazy(() => import("../pages/MapaCampus"));
const Login = lazy(() => import("../pages/Login"));
const Register = lazy(() => import("../pages/Register"));
const NotFound = lazy(() => import("../pages/NotFound"));
const Negocios = lazy(() => import("../pages/Negocios"));
const Admin = lazy(() => import("../pages/Admin"));
const Chat = lazy(() => import("../pages/Chat"));
const Favoritos = lazy(() => import("../pages/Comunidad").then(module => ({default: module.Favoritos})));
const MisResenas = lazy(() => import("../pages/Comunidad").then(module => ({default: module.MisResenas})));
const FichaLugar = lazy(() => import("../pages/Comunidad").then(module => ({default: module.FichaLugar})));
const Reportes = lazy(() => import("../pages/Comunidad").then(module => ({default: module.Reportes})));

function AppRoutes() {
  return (
    <BrowserRouter>
      <PageMotion>
      <Suspense fallback={<div className="route-loading" role="status"><span>M</span> Preparando MiUni…</div>}>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/mapa" element={<MapaCampus />} />
        <Route path="/asistente" element={<ProtectedRoute><Chat /></ProtectedRoute>} />
        <Route path="/login" element={<Login />} />
        <Route path="/registro" element={<Register />} />

        <Route path="/favoritos" element={<ProtectedRoute><Favoritos /></ProtectedRoute>} />
        <Route path="/resenas" element={<ProtectedRoute><MisResenas /></ProtectedRoute>} />
        <Route path="/negocios" element={<ProtectedRoute><Negocios /></ProtectedRoute>} />
        <Route path="/admin" element={<ProtectedRoute role="Administrador"><Admin /></ProtectedRoute>} />
        <Route path="/lugares/:lugarId" element={<FichaLugar />} />

        <Route path="/resenas-reportes" element={<ProtectedRoute><Reportes /></ProtectedRoute>} />

        <Route path="*" element={<NotFound />} />
      </Routes>
      </Suspense>
      </PageMotion>
    </BrowserRouter>
  );
}

export default AppRoutes;
