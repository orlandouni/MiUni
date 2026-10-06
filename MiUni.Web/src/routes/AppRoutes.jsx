import {
  BrowserRouter,
  Navigate,
  Routes,
  Route,
} from "react-router-dom";

import Home from "../pages/Home";
import Login from "../pages/Login";
import Register from "../pages/Register";
import NotFound from "../pages/NotFound";
import ProtectedRoute from "../components/ProtectedRoute";
import Negocios from "../pages/Negocios";
import Admin from "../pages/Admin";
import {
  Favoritos,
  MisResenas,
  FichaLugar,
} from "../pages/Comunidad";

function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/registro" element={<Register />} />

        <Route path="/favoritos" element={<ProtectedRoute><Favoritos /></ProtectedRoute>} />
        <Route path="/resenas" element={<ProtectedRoute><MisResenas /></ProtectedRoute>} />
        <Route path="/negocios" element={<ProtectedRoute><Negocios /></ProtectedRoute>} />
        <Route path="/admin" element={<ProtectedRoute role="Administrador"><Admin /></ProtectedRoute>} />
        <Route path="/lugares/:lugarId" element={<FichaLugar />} />

        <Route
          path="/resenas-reportes"
          element={<Navigate to="/resenas" replace />}
        />

        <Route path="*" element={<NotFound />} />
      </Routes>
    </BrowserRouter>
  );
}

export default AppRoutes;
