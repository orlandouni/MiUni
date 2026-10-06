import "../pages/Auth.css";

export default function AuthExplorer() {
  return (
    <div className="ma-explorer">
      <header className="ma-art-header">
        <span className="ma-art-wordmark">MIUNI</span>
        <span className="ma-art-line" />
        <span className="ma-art-caption">CAMPUS DIGITAL</span>
      </header>

      <figure className="ma-illustration">
        <img
          className="ma-flat-illustration"
          src="/auth/miuni-campus-playful-3d.png"
          alt="Personajes geométricos 3D explorando el campus con mapa, libros, café y bicicleta"
        />
      </figure>

      <span className="ma-art-footer">MAPA · RUTAS · COMUNIDAD</span>
    </div>
  );
}
