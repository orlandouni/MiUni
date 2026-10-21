import './MascotaIlustrada.css';

export default function MascotaIlustrada({className = ''}) {
  return <div className={`miuni-mascota ${className}`} aria-hidden="true">
    <img src="/mascot/miuni-buho-ilustrado.png" alt="" width="1199" height="1312" decoding="async" draggable="false" />
  </div>;
}
