import {BookOpen, GraduationCap, Coffee, MapPin, MessageSquare, Route} from 'lucide-react';
import './AuthExplorer.css';

export default function AuthExplorer({animated = false}) {
  return (
    <div className={`auth-explorer ${animated ? 'animated' : ''}`}>
      <span className="ae-eyebrow">Hecho para tu vida universitaria</span>
      <h2>Conecta con tu<br /><em>siguiente paso.</em></h2>
      <p>Un lugar para encontrar tu camino<br />y sentirte parte de tu universidad.</p>
      <div className="ae-map" aria-hidden="true">
        <div className="ae-path" />
        <span className="ae-building first"><BookOpen size={27} /></span>
        <span className="ae-building second"><GraduationCap size={30} /></span>
        <span className="ae-building third"><Coffee size={24} /></span>
        <span className="ae-pin"><MapPin size={30} /></span>
        <img className="ae-mascot" src="/mascot/miuni-buho-ilustrado.png" alt="" decoding="async" />
        <div className="ae-map-note"><Route size={22} /><span>De aquí,<strong>a donde quieras.</strong></span></div>
      </div>
      <div className="ae-features"><span><MapPin size={15} /> Explora el campus</span><span><MessageSquare size={15} /> Resuelve tus dudas</span></div>
    </div>
  );
}
