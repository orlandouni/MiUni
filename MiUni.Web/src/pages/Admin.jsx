import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { AlertTriangle, ArrowLeft, Building2, GraduationCap, LayoutDashboard, MapPin, RefreshCw, ShieldCheck, Star, Tags, Users } from "lucide-react";
import { getMe } from "../services/api";
import { adminApi } from "../services/admin";
import { useCarga } from "./admin/ui";
import Resumen from "./admin/Resumen";
import Reportes from "./admin/Reportes";
import Negocios from "./admin/Negocios";
import Lugares from "./admin/Lugares";
import Resenas from "./admin/Resenas";
import Usuarios from "./admin/Usuarios";
import { Carreras, Categorias } from "./admin/Catalogos";
import "./Admin.css";

const SECCIONES = [
  { id: "resumen", texto: "Resumen", icono: LayoutDashboard, titulo: "Centro de control", sub: "Métricas generales de MiUni." },
  { id: "reportes", texto: "Reportes", icono: AlertTriangle, contador: "reportesPendientes", titulo: "Moderación", sub: "Reportes enviados por la comunidad." },
  { id: "negocios", texto: "Negocios", icono: Building2, contador: "solicitudesPendientes", titulo: "Negocios", sub: "Solicitudes de publicación de propietarios." },
  { id: "resenas", texto: "Reseñas", icono: Star, titulo: "Reseñas", sub: "Opiniones publicadas sobre los lugares." },
  { id: "lugares", texto: "Lugares", icono: MapPin, titulo: "Lugares", sub: "Contenido del mapa del campus." },
  { id: "categorias", texto: "Categorías", icono: Tags, titulo: "Categorías", sub: "Clasificación de los lugares." },
  { id: "usuarios", texto: "Usuarios", icono: Users, titulo: "Usuarios", sub: "Cuentas, roles y acceso." },
  { id: "carreras", texto: "Carreras", icono: GraduationCap, titulo: "Carreras", sub: "Catálogo académico." },
];

export default function Admin() {
  const [params, setParams] = useSearchParams();
  const actual = SECCIONES.find(s => s.id === params.get("s")) || SECCIONES[0];
  const panel = useCarga(adminApi.panel);
  const [yo, setYo] = useState(null);
  const [menuAbierto, setMenuAbierto] = useState(false);

  useEffect(() => {
    let activo = true;
    getMe().then(u => { if (activo) setYo(u); }).catch(() => {});
    return () => { activo = false; };
  }, []);

  const irA = id => { setParams(id === "resumen" ? {} : { s: id }); setMenuAbierto(false); window.scrollTo({ top: 0 }); };
  const props = { onCambio: panel.recargar };
  const Icono = actual.icono;

  return <main className="adm">
    <aside className={`adm-sidebar${menuAbierto ? " abierto" : ""}`}>
      <div className="adm-brand"><ShieldCheck size={20} /><span>MiUni<small>Administración</small></span></div>
      <nav aria-label="Secciones de administración">
        {SECCIONES.map(s => {
          const I = s.icono;
          const n = s.contador ? panel.datos?.[s.contador] : 0;
          return <button type="button" key={s.id} className={actual.id === s.id ? "activo" : ""} aria-current={actual.id === s.id ? "page" : undefined} onClick={() => irA(s.id)}>
            <I size={17} /> {s.texto}{n > 0 && <span className="adm-count">{n}</span>}
          </button>;
        })}
      </nav>
      <div className="adm-sidebar-foot">
        {yo && <p><strong>{yo.nombre}</strong><small>{yo.email}</small></p>}
        <Link to="/" className="adm-back"><ArrowLeft size={15} /> Volver a MiUni</Link>
      </div>
    </aside>
    {menuAbierto && <div className="adm-scrim" onClick={() => setMenuAbierto(false)} />}

    <div className="adm-main">
      <header className="adm-topbar">
        <button type="button" className="adm-menu-btn" onClick={() => setMenuAbierto(true)} aria-label="Abrir menú"><Icono size={18} /> {actual.texto}</button>
        <div>
          <p className="adm-eyebrow">Administración</p>
          <h1>{actual.titulo}</h1>
          <p className="adm-sub">{actual.sub}</p>
        </div>
        {actual.id === "resumen" && <button type="button" className="adm-btn" onClick={panel.recargar} disabled={panel.cargando}><RefreshCw size={15} /> Actualizar</button>}
      </header>

      <div className="adm-body">
        {actual.id === "resumen" && <Resumen panel={panel.datos} cargando={panel.cargando} error={panel.error} irA={irA} reintentar={panel.recargar} />}
        {actual.id === "reportes" && <Reportes {...props} />}
        {actual.id === "negocios" && <Negocios {...props} />}
        {actual.id === "resenas" && <Resenas {...props} />}
        {actual.id === "lugares" && <Lugares {...props} />}
        {actual.id === "categorias" && <Categorias {...props} />}
        {actual.id === "usuarios" && <Usuarios {...props} yo={yo?.id} />}
        {actual.id === "carreras" && <Carreras />}
      </div>
    </div>
  </main>;
}
