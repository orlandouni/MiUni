import { Link } from "react-router-dom";
import { MapPin, ArrowUpRight } from "lucide-react";
function CampusMap() {
  return (
    <svg className="campus-map" viewBox="0 0 600 400" role="img" aria-label="Plano ilustrativo del campus; no es un mapa de navegación real">
      <defs><pattern id="map-grid" width="24" height="24" patternUnits="userSpaceOnUse"><path d="M24 0H0V24" fill="none" stroke="currentColor" strokeOpacity=".12" /></pattern></defs>
      <rect width="600" height="400" fill="url(#map-grid)" />
      <g fill="none" stroke="currentColor" strokeOpacity=".35" strokeWidth="2">
        <path d="M-20 318H160V200H360V80H620M-20 342H184V224H384V104H620" />
        <path d="M48 55H150V144H48ZM210 55H312V144H210ZM430 147H552V224H430ZM236 273H338V368H236ZM405 273H540V368H405Z" />
        <path d="M68 76H129V125H68ZM451 167H531V204H451" />
        <circle cx="95" cy="235" r="31"/><circle cx="95" cy="235" r="20"/>
      </g>
      <path d="M52 330H172V212H372V92H470" fill="none" stroke="var(--signal)" strokeWidth="7" strokeLinejoin="round" />
      <circle cx="52" cy="330" r="10" fill="var(--navy)" stroke="var(--signal)" strokeWidth="5" /><circle cx="470" cy="92" r="11" fill="var(--signal)" />
      <g fill="currentColor" fontFamily="sans-serif" fontSize="12" letterSpacing="2"><text x="49" y="42">AULAS</text><text x="237" y="257">SERVICIOS</text><text x="413" y="256">BIBLIOTECA</text></g>
      <g transform="translate(318 132)"><rect width="202" height="43" fill="var(--signal)"/><text x="16" y="27" fill="var(--navy)" fontFamily="sans-serif" fontSize="13" fontWeight="700">TU SIGUIENTE DESTINO ↗</text></g>
      <text x="27" y="380" fill="currentColor" fontFamily="sans-serif" fontSize="11" letterSpacing="2">● ESTÁS AQUÍ</text>
      <path d="M555 42V13L548 25M555 13L562 25" fill="none" stroke="currentColor" strokeWidth="2"/><text x="550" y="59" fill="currentColor" fontSize="12">N</text>
    </svg>
  );
}


export default function CampusPanel() {
  return (
      <section className="campus-panel" aria-label="Bienvenido a MiUni">
        <Link to="/" className="campus-brand" aria-label="MiUni, inicio"><MapPin size={30} strokeWidth={2.5} />MiUni<span>GUÍA DE CAMPUS</span></Link>
        <div className="campus-intro"><p className="wayfinding-label">TU CAMPUS. TU RUTA.</p><h2>Encuentra<br /> tu lugar<span>.</span></h2><p>Ubica tu próxima clase.<br />Resuelve tus dudas. Muévete con confianza.</p></div>
        <CampusMap />
        <div className="campus-services"><span><MapPin size={17} /> Navegación de campus</span><span><ArrowUpRight size={19} /> Chatbot institucional</span></div>
        <p className="map-caption">MIUNI / PLANO ILUSTRATIVO</p>
      </section>
  );
}

