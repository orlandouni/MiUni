import {Link} from 'react-router-dom';
import {MapPin, ArrowUpRight} from 'lucide-react';
import './NotFound.css';

function NotFound() {
  return (
    <main className="not-found">
      <span className="ui-eyebrow"><MapPin size={15} /> MiUni · Fuera del camino</span>
      <span className="not-found-number" aria-hidden="true">404</span>
      <h1>Este camino no lleva a clase.</h1>
      <p>No encontramos esta página. Volvamos a un lugar conocido.</p>
      <div><Link className="ui-button" to="/">Volver al inicio <ArrowUpRight size={16} /></Link><Link className="ui-button secondary" to="/mapa">Explorar el mapa</Link></div>
    </main>
  );
}

export default NotFound;
