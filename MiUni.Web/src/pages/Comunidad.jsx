import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  ArrowLeft,
  BookOpen,
  Building2,
  CalendarDays,
  ExternalLink,
  FileUp,
  Plus,
  ChevronDown,
  ChevronUp,
  ChevronRight,
  Eye,
  MessageSquare,
  Menu,
  Pencil,
  Flag,
  Star,
  Trash2,
} from "lucide-react";

import api, { clearSession, getMe, getToken } from "../services/api";
import CampusSidebar from "../components/CampusSidebar";
import {
  obtenerLugares,
  obtenerResenas,
  crearResena,
  editarResena,
  eliminarResena,
  obtenerFavoritos,
  obtenerReportes,
  guardarFavorito,
  quitarFavorito,
  mensajeError,
} from "../services/comunidad";

import {
  obtenerHorario,
  leerHorarioPdf,
  guardarHorario,
  agregarClase,
  eliminarClase,
  vincularBuhoRater,
  importarPerfilesBuhoRater,
} from "../services/horario";
import { DIAS, agruparPorMateria, resumenSesiones, esEnlacePerfilBuhoRater } from "../services/horarioUtils";

import InfoNegocioPublica from "../components/InfoNegocioPublica";
import "./Comunidad.css";

// Vuelve a consultar cuando cambia la función o se pulsa actualizar.
// Ignora respuestas de consultas que ya quedaron atrás.
function useConsulta(consultar) {
  const [version, setVersion] = useState(0);
  const [estado, setEstado] = useState({
    consultar: null,
    version: -1,
    datos: null,
    error: "",
    status: null,
  });

  useEffect(() => {
    let vigente = true;

    Promise.resolve()
      .then(consultar)
      .then((datos) => {
        if (vigente) {
          setEstado({
            consultar,
            version,
            datos,
            error: "",
            status: null,
          });
        }
      })
      .catch((error) => {
        if (vigente) {
          setEstado({
            consultar,
            version,
            datos: null,
            error: mensajeError(error),
            status: error.response?.status ?? null,
          });
        }
      });

    return () => {
      vigente = false;
    };
  }, [consultar, version]);

  // Si cambió la consulta o se pidió actualizar, esperamos su respuesta.
  const cargando =
    estado.consultar !== consultar || estado.version !== version;

  return {
    datos: cargando ? null : estado.datos,
    cargando,
    error: cargando ? "" : estado.error,
    actualizar: () => setVersion((actual) => actual + 1),
  };
}

async function consultarSesion() {
  return getToken() ? getMe() : null;
}

function useSesion() {
  return useConsulta(consultarSesion);
}

function mismaId(a, b) {
  return Boolean(a && b && a.toLowerCase() === b.toLowerCase());
}

function fechaLegible(fecha) {
  if (!fecha) return "";

  return new Intl.DateTimeFormat("es-MX", {
    dateStyle: "medium",
  }).format(new Date(fecha));
}

function EstadoConsulta({ consulta }) {
  if (consulta.cargando) {
    return <p className="com-estado" role="status">Cargando…</p>;
  }

  if (consulta.error) {
    return (
      <div className="com-error" role="alert">
        <p>{consulta.error}</p>

        <div className="com-acciones">
          <button
            type="button"
            className="com-boton com-secundario"
            onClick={consulta.actualizar}
          >
            Reintentar
          </button>

          {consulta.status === 401 && <Link to="/login">Iniciar sesión</Link>}
        </div>
      </div>
    );
  }

  return null;
}

function FormularioReporte({ pregunta, motivo, setMotivo, enviando, error, onEnviar, onCancelar }) {
  return <form className="com-formulario com-reporte-form" onSubmit={onEnviar} aria-busy={enviando}>
    <label>{pregunta}
      <textarea required minLength={3} maxLength={2000} rows={3} value={motivo}
        onChange={event => setMotivo(event.target.value)} placeholder="Describe el problema" />
    </label>
    {error && <p className="com-error" role="alert">{error}</p>}
    <div className="com-acciones">
      <button className="com-boton" type="submit" disabled={enviando}>
        {enviando ? "Enviando…" : "Enviar reporte"}
      </button>
      <button className="com-boton com-secundario" type="button" disabled={enviando} onClick={onCancelar}>Cancelar</button>
    </div>
  </form>;
}

function Pantalla({ titulo, subtitulo, children, conSidebar = false, usuario = null }) {
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const nombre = usuario?.nombre || usuario?.email?.split("@")[0] || "Mi perfil";
  const iniciales = nombre.slice(0, 2).toUpperCase();
  const cerrarSesion = () => {
    clearSession();
    navigate("/login", { replace: true });
  };

  if (conSidebar) {
    return (
      <div className="home-app comunidad-app">
        <a href="#com-main-content" className="skip-link">Saltar al contenido</a>
        <CampusSidebar user={usuario} open={sidebarOpen} onClose={() => setSidebarOpen(false)} onLogout={cerrarSesion} />
        <div className="home-main">
          <header className="home-topbar">
            <button className="home-menu" onClick={() => setSidebarOpen(true)} aria-label="Abrir menú"><Menu size={19} /></button>
            <div className="home-top-brand"><span className="home-top-icon"><BookOpen size={16} /></span><strong>MIUNI</strong></div>
            <span className="home-top-context">Mi espacio · {titulo}</span>
            <Link to="/perfil" className="home-account" aria-label="Ir a mi perfil" title="Mi perfil"><span>{iniciales}</span><ChevronRight size={15} /></Link>
          </header>
          <main className="home-content comunidad-content" id="com-main-content">
            <section className="comunidad">
              <div className="com-contenedor">
                <header className="com-encabezado">
                  <div><h1>{titulo}</h1>{subtitulo && <p>{subtitulo}</p>}</div>
                </header>
                {children}
              </div>
            </section>
          </main>
        </div>
      </div>
    );
  }

  return (
    <main className="comunidad">
      <div className="com-contenedor">
        <header className="com-encabezado">
          <Link to="/" className="com-icono" aria-label="Volver al mapa">
            <ArrowLeft size={21} />
          </Link>

          <div>
            <h1>{titulo}</h1>
            {subtitulo && <p>{subtitulo}</p>}
          </div>
        </header>

        <nav className="com-navegacion" aria-label="Mi actividad">
          <Link to="/">Mapa</Link>
          <Link to="/perfil">Mi perfil</Link>
          <Link to="/horario">Mi horario</Link>
          <Link to="/favoritos">Mis favoritos</Link>
          <Link to="/resenas">Mis reseñas</Link>
          <Link to="/resenas-reportes">Mis reportes</Link>
        </nav>

        {children}
      </div>
    </main>
  );
}

function SolicitarSesion() {
  return (
    <div className="com-vacio">
      <MessageSquare size={28} />
      <p>Inicia sesión para consultar y guardar tu actividad.</p>
      <Link className="com-boton" to="/login">
        Iniciar sesión
      </Link>
    </div>
  );
}

function Estrellas({ valor }) {
  return (
    <span className="com-estrellas" aria-label={`${valor} de 5 estrellas`}>
      {[1, 2, 3, 4, 5].map((numero) => (
        <Star
          key={numero}
          size={16}
          aria-hidden="true"
          fill={numero <= valor ? "currentColor" : "none"}
          className={numero <= valor ? "" : "com-estrella-vacia"}
        />
      ))}
    </span>
  );
}

function FormularioResena({ inicial, alGuardar, alCancelar }) {
  const [calificacion, setCalificacion] = useState(
    inicial?.calificacion ?? 5
  );
  const [comentario, setComentario] = useState(
    inicial?.comentario ?? ""
  );
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");

  async function enviar(evento) {
    evento.preventDefault();
    if (guardando) return;

    setGuardando(true);
    setError("");

    try {
      await alGuardar(calificacion, comentario);
      setComentario("");
      setCalificacion(5);
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <form className="com-formulario" onSubmit={enviar} aria-busy={guardando}>
      <fieldset disabled={guardando}>
        <legend>¿Qué te pareció este lugar?</legend>

        <div className="com-selector">
          {[1, 2, 3, 4, 5].map((numero) => (
            <button
              key={numero}
              type="button"
              aria-label={`${numero} estrellas`}
              aria-pressed={calificacion === numero}
              onClick={() => setCalificacion(numero)}
            >
              <Star
                size={29}
                fill={numero <= calificacion ? "currentColor" : "none"}
              />
            </button>
          ))}
        </div>

        <label>
          Comentario <span>(opcional)</span>
          <textarea
            rows={4}
            value={comentario}
            onChange={(evento) => setComentario(evento.target.value)}
            placeholder="Escribe tu opinión aquí…"
          />
        </label>

        {error && <p className="com-error" role="alert">{error}</p>}

        <div className="com-acciones">
          <button className="com-boton" type="submit">
            {guardando
              ? "Guardando…"
              : inicial
                ? "Guardar cambios"
                : "Publicar reseña"}
          </button>

          {alCancelar && (
            <button
              className="com-boton com-secundario"
              type="button"
              onClick={alCancelar}
            >
              Cancelar
            </button>
          )}
        </div>
      </fieldset>
    </form>
  );
}

function TarjetaResena({ resena, titulo, propia, puedeReportar = false, alCambiar }) {
  const [editando, setEditando] = useState(false);
  const [eliminando, setEliminando] = useState(false);
  const [error, setError] = useState("");
  const [reportando, setReportando] = useState(false);
  const [motivo, setMotivo] = useState("");
  const [enviandoReporte, setEnviandoReporte] = useState(false);
  const [reporteEnviado, setReporteEnviado] = useState(false);

  async function reportar(evento) {
    evento.preventDefault();
    if (motivo.trim().length < 3) {
      setError("Describe el motivo con al menos 3 caracteres.");
      return;
    }
    setEnviandoReporte(true);
    setError("");
    try {
      await api.post("/api/ReporteUsuario", { resenaId: resena.id, motivo: motivo.trim() });
      setReporteEnviado(true);
      setReportando(false);
    } catch (e) { setError(mensajeError(e)); }
    finally { setEnviandoReporte(false); }
  }

  async function borrar() {
    if (!window.confirm("¿Quieres eliminar esta reseña?")) return;

    setEliminando(true);
    setError("");

    try {
      await eliminarResena(resena.id);
      alCambiar();
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setEliminando(false);
    }
  }

  async function guardar(calificacion, comentario) {
    await editarResena(resena.id, calificacion, comentario);
    setEditando(false);
    alCambiar();
  }

  return (
    <article className="com-tarjeta">
      <div className="com-fila">
        <div className="com-crecer">
          <h3>{titulo}</h3>
          <Estrellas valor={resena.calificacion} />
        </div>

        {propia && !editando && (
          <div className="com-acciones">
            <button
              className="com-icono"
              type="button"
              aria-label="Editar reseña"
              disabled={eliminando}
              onClick={() => {
                setError("");
                setEditando(true);
              }}
            >
              <Pencil size={17} />
            </button>

            <button
              className="com-icono com-peligro"
              type="button"
              aria-label="Eliminar reseña"
              disabled={eliminando}
              onClick={borrar}
            >
              <Trash2 size={17} />
            </button>
          </div>
        )}
        {puedeReportar && !propia && !reporteEnviado && <button className="com-boton com-secundario com-reportar" type="button" onClick={() => { setReportando(value => !value); setError(""); }}><Flag size={16} /> Reportar reseña</button>}
      </div>

      {editando ? (
        <FormularioResena
          inicial={resena}
          alGuardar={guardar}
          alCancelar={() => setEditando(false)}
        />
      ) : (
        <>
          {resena.comentario && (
            <p className="com-comentario">{resena.comentario}</p>
          )}
          <p className="com-fecha">{fechaLegible(resena.fechaCreacion)}</p>
        </>
      )}

      {reportando && <FormularioReporte pregunta="¿Por qué reportas esta reseña?" motivo={motivo} setMotivo={setMotivo} enviando={enviandoReporte} error={error} onEnviar={reportar} onCancelar={() => setReportando(false)} />}
      {reporteEnviado && <p className="com-exito" role="status">Gracias. El equipo revisará tu reporte.</p>}

      {error && !reportando && <p className="com-error" role="alert">{error}</p>}
    </article>
  );
}

function TarjetaFavorito({ favorito, lugar, alQuitar }) {
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState("");

  async function quitar() {
    setOcupado(true);
    setError("");

    try {
      await quitarFavorito(favorito.lugarId);
      alQuitar(favorito.lugarId);
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setOcupado(false);
    }
  }

  return (
    <article className="com-tarjeta">
      <div className="com-fila">
        <span className="com-categoria-icono">
          <Building2 size={24} />
        </span>

        <div className="com-crecer">
          <h2>{lugar?.nombre ?? "Lugar no disponible"}</h2>
          {lugar && (
            <span className="com-etiqueta">{lugar.categoria.nombre}</span>
          )}
          <p className="com-descripcion">
            {lugar?.descripcion ?? "Puedes quitarlo de tus favoritos."}
          </p>
        </div>

        <div className="com-favorito-acciones">
          <span className="com-guardado">
            <Star size={13} fill="currentColor" /> Guardado
          </span>

          <div className="com-acciones">
            <button
              type="button"
              className="com-icono com-peligro"
              aria-label="Quitar de favoritos"
              disabled={ocupado}
              onClick={quitar}
            >
              <Trash2 size={18} />
            </button>

            {lugar && (
              <Link
                to={`/lugares/${lugar.id}`}
                className="com-icono"
                aria-label={`Ver ${lugar.nombre}`}
              >
                <Eye size={18} />
              </Link>
            )}
          </div>
        </div>
      </div>

      {error && <p className="com-error" role="alert">{error}</p>}
    </article>
  );
}

function ListaFavoritos() {
  const consultar = useCallback(async () => {
    const [favoritos, lugares] = await Promise.all([
      obtenerFavoritos(),
      obtenerLugares().catch(() => []),
    ]);
    return { favoritos, lugares };
  }, []);

  const consulta = useConsulta(consultar);

  return (
    <>
      <EstadoConsulta consulta={consulta} />

      {consulta.datos && !consulta.error && (
        <>
          <p className="com-contador">
            {consulta.datos.favoritos.length} lugares guardados
          </p>

          <div className="com-lista">
            {consulta.datos.favoritos.map((favorito) => (
              <TarjetaFavorito
                key={favorito.lugarId}
                favorito={favorito}
                lugar={consulta.datos.lugares.find(
                  (lugar) => mismaId(lugar.id, favorito.lugarId)
                )}
                alQuitar={consulta.actualizar}
              />
            ))}
          </div>

          {consulta.datos.favoritos.length === 0 && (
            <p className="com-vacio">
              Todavía no tienes favoritos. Abre un lugar del mapa y guárdalo.
            </p>
          )}
        </>
      )}
    </>
  );
}

export function Favoritos() {
  const sesion = useSesion();

  return (
    <Pantalla titulo="Mis favoritos" subtitulo="Tus lugares guardados del campus" conSidebar usuario={sesion.datos}>
      <EstadoConsulta consulta={sesion} />
      {!sesion.cargando && !sesion.error && (
        sesion.datos ? <ListaFavoritos /> : <SolicitarSesion />
      )}
    </Pantalla>
  );
}

function ListaMisResenas({ usuario }) {
  const consultar = useCallback(async () => {
    const [resenas, lugares] = await Promise.all([
      obtenerResenas(),
      obtenerLugares().catch(() => []),
    ]);

    return {
      resenas: resenas.filter((resena) =>
        mismaId(resena.usuarioId, usuario.id)
      ),
      lugares,
    };
  }, [usuario.id]);

  const consulta = useConsulta(consultar);

  return (
    <>
      <EstadoConsulta consulta={consulta} />

      {consulta.datos && (
        <>
          <p className="com-contador">
            {consulta.datos.resenas.length} reseñas
          </p>

          <div className="com-lista">
            {consulta.datos.resenas.map((resena) => (
              <TarjetaResena
                key={resena.id}
                resena={resena}
                titulo={
                  consulta.datos.lugares.find(
                    (lugar) => mismaId(lugar.id, resena.lugarId)
                  )?.nombre ?? "Lugar no disponible"
                }
                propia
                alCambiar={consulta.actualizar}
              />
            ))}
          </div>

          {consulta.datos.resenas.length === 0 && (
            <p className="com-vacio">
              Todavía no has publicado reseñas.
            </p>
          )}
        </>
      )}
    </>
  );
}

export function MisResenas() {
  const sesion = useSesion();

  return (
    <Pantalla titulo="Mis reseñas" subtitulo="Tu experiencia en el campus" conSidebar usuario={sesion.datos}>
      <EstadoConsulta consulta={sesion} />
      {!sesion.cargando && !sesion.error && (
        sesion.datos
          ? <ListaMisResenas usuario={sesion.datos} />
          : <SolicitarSesion />
      )}
    </Pantalla>
  );
}

function BotonFavorito({ lugarId }) {
  const consultar = useCallback(() => obtenerFavoritos(), []);
  const consulta = useConsulta(consultar);
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState("");

  const guardado = consulta.datos?.some((favorito) =>
    mismaId(favorito.lugarId, lugarId)
  );

  async function cambiar() {
    setOcupado(true);
    setError("");

    try {
      if (guardado) {
        await quitarFavorito(lugarId);
      } else {
        await guardarFavorito(lugarId);
      }
      consulta.actualizar();
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setOcupado(false);
    }
  }

  return (
    <div className="com-favorito-control">
      <EstadoConsulta consulta={consulta} />

      {consulta.datos && (
        <button
          type="button"
          className="com-boton com-dorado"
          aria-pressed={Boolean(guardado)}
          disabled={ocupado || consulta.cargando}
          onClick={cambiar}
        >
          <Star size={18} fill={guardado ? "currentColor" : "none"} />
          {ocupado
            ? "Guardando…"
            : guardado
              ? "Quitar de favoritos"
              : "Guardar favorito"}
        </button>
      )}

      {error && <p className="com-error" role="alert">{error}</p>}
    </div>
  );
}

function ContenidoLugar({ lugarId }) {
  const sesion = useSesion();
  const [mostrarResenas, setMostrarResenas] = useState(true);
  const [aviso, setAviso] = useState("");
  const [reportandoLugar, setReportandoLugar] = useState(false);
  const [motivoLugar, setMotivoLugar] = useState("");
  const [reporteLugarEnviado, setReporteLugarEnviado] = useState(false);
  const [errorReporteLugar, setErrorReporteLugar] = useState("");
  const [enviandoReporteLugar, setEnviandoReporteLugar] = useState(false);

  const consultar = useCallback(async () => {
    const [lugares, resenas] = await Promise.all([
      obtenerLugares(),
      obtenerResenas(lugarId),
    ]);

    return {
      lugar: lugares.find((lugar) => mismaId(lugar.id, lugarId)),
      resenas,
    };
  }, [lugarId]);

  const consulta = useConsulta(consultar);
  const lugar = consulta.datos?.lugar;
  const resenas = consulta.datos?.resenas ?? [];

  const promedio = resenas.length
    ? (
        resenas.reduce((total, resena) => total + resena.calificacion, 0)
        / resenas.length
      ).toFixed(1)
    : "—";

  async function publicar(calificacion, comentario) {
    await crearResena(lugarId, calificacion, comentario);
    setAviso("Tu reseña se publicó correctamente.");
    consulta.actualizar();
  }

  async function reportarLugar(evento) {
    evento.preventDefault();
    if (motivoLugar.trim().length < 3) { setErrorReporteLugar("Describe el motivo con al menos 3 caracteres."); return; }
    setEnviandoReporteLugar(true); setErrorReporteLugar("");
    try {
      await api.post("/api/ReporteUsuario", { lugarId, motivo: motivoLugar.trim() });
      setReporteLugarEnviado(true); setReportandoLugar(false);
    } catch (error) { setErrorReporteLugar(mensajeError(error)); }
    finally { setEnviandoReporteLugar(false); }
  }

  return (
    <Pantalla titulo="Ficha del lugar" subtitulo="Reseñas y favoritos">
      <EstadoConsulta consulta={consulta} />

      {aviso && <p className="com-exito" role="status">{aviso}</p>}

      {consulta.datos && !lugar && (
        <p className="com-vacio">
          El lugar no existe o ya no está disponible.
        </p>
      )}

      {lugar && (
        <>
          <section className="com-ficha">
            <div className="com-fila">
              <div className="com-crecer">
                <p className="com-sobretitulo">{lugar.categoria.nombre}</p>
                <h2>{lugar.nombre}</h2>
              </div>

              <span
                className="com-promedio"
                aria-label={
                  resenas.length
                    ? `Promedio: ${promedio} de 5`
                    : "Sin calificaciones"
                }
              >
                {promedio} <Star size={15} fill="currentColor" />
              </span>
            </div>

            <p>{lugar.descripcion}</p>

            <InfoNegocioPublica lugarId={lugarId} />

            {sesion.datos && (
              <><BotonFavorito key={sesion.datos.id} lugarId={lugarId} />
                <div className="com-reporte-lugar">{!reporteLugarEnviado && <button className="com-boton com-secundario" type="button" onClick={() => { setReportandoLugar(value => !value); setErrorReporteLugar(""); }}><Flag size={16} /> Reportar lugar</button>}
                  {reporteLugarEnviado && <p className="com-exito" role="status">Gracias. El equipo revisará tu reporte.</p>}
                  {reportandoLugar && <FormularioReporte pregunta="¿Por qué reportas este lugar?" motivo={motivoLugar} setMotivo={setMotivoLugar} enviando={enviandoReporteLugar} error={errorReporteLugar} onEnviar={reportarLugar} onCancelar={() => setReportandoLugar(false)} />}
                </div>
              </>
            )}
          </section>

          <section className="com-seccion">
            <button
              type="button"
              className="com-acordeon"
              aria-expanded={mostrarResenas}
              aria-controls="lista-resenas-lugar"
              onClick={() => setMostrarResenas((actual) => !actual)}
            >
              <MessageSquare size={21} />
              <span>Reseñas ({resenas.length})</span>
              {mostrarResenas
                ? <ChevronUp size={19} />
                : <ChevronDown size={19} />}
            </button>

            {mostrarResenas && (
              <div id="lista-resenas-lugar" className="com-lista com-interior">
                {resenas.map((resena) => {
                  const propia = mismaId(resena.usuarioId, sesion.datos?.id);

                  return (
                    <TarjetaResena
                      key={resena.id}
                      resena={resena}
                      titulo={propia ? "Tú" : "Usuario de MiUni"}
                      propia={propia}
                      puedeReportar={Boolean(sesion.datos)}
                      alCambiar={consulta.actualizar}
                    />
                  );
                })}

                {resenas.length === 0 && (
                  <p className="com-estado">
                    Aún no hay reseñas para este lugar.
                  </p>
                )}
              </div>
            )}
          </section>

          <section className="com-seccion">
            <h2 className="com-titulo-seccion">
              <Pencil size={20} /> Publicar reseña
            </h2>

            <div className="com-interior">
              <EstadoConsulta consulta={sesion} />

              {!sesion.cargando && !sesion.error && (
                sesion.datos
                  ? <FormularioResena alGuardar={publicar} />
                  : <SolicitarSesion />
              )}
            </div>
          </section>
        </>
      )}
    </Pantalla>
  );
}

export function FichaLugar() {
  const { lugarId } = useParams();

  // Cambiar de lugar reinicia formularios y mensajes del anterior.
  return <ContenidoLugar key={lugarId} lugarId={lugarId} />;
}

function estadoReporte(estado) {
  return estado === "Pendiente" ? "Pendiente" : estado === "Revisado" ? "Revisado" : "Descartado";
}

function ListaReportes() {
  const consulta = useConsulta(obtenerReportes);
  return <>
    <EstadoConsulta consulta={consulta} />
    {consulta.datos && <div className="com-lista">
      {consulta.datos.map(reporte => <article className="com-tarjeta com-reporte-card" key={reporte.id}>
        <div className="com-fila"><div className="com-crecer"><span className="com-etiqueta">{reporte.resenaId ? "Reseña" : "Lugar"}</span><h2>{reporte.nombreLugar || "Lugar reportado"}</h2></div><span className={`com-estado-reporte estado-${reporte.estado?.toLowerCase()}`}>{estadoReporte(reporte.estado)}</span></div>
        <p className="com-reporte-motivo">{reporte.motivo}</p><p className="com-fecha">{fechaLegible(reporte.fechaCreacion)}</p>
      </article>)}
      {consulta.datos.length === 0 && <p className="com-vacio">Todavía no has enviado reportes.</p>}
    </div>}
  </>;
}

export function Reportes() {
  return <Pantalla titulo="Mis reportes" subtitulo="Consulta el estado de tus reportes enviados"><ListaReportes /></Pantalla>;
}

// ---------- Mi horario ----------

function TarjetaMateria({ grupo, alQuitar, puedeVincular = false }) {
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState("");

  async function quitar() {
    setOcupado(true);
    setError("");
    try {
      for (const sesion of grupo.sesiones) await eliminarClase(sesion.id);
      alQuitar();
    } catch (error) {
      setError(mensajeError(error));
      setOcupado(false);
    }
  }

  return (
    <article className="com-tarjeta com-horario-tarjeta">
      <div className="com-fila">
        <span className="com-categoria-icono">
          <CalendarDays size={22} />
        </span>

        <div className="com-crecer">
          <h2>{grupo.materia}</h2>
          <p className="com-descripcion">
            {grupo.profesor ?? "Sin profesor asignado"}
            {grupo.salones.length > 0 && ` · ${grupo.salones.join(", ")}`}
          </p>
          <p className="com-horario-sesiones">{resumenSesiones(grupo.sesiones)}</p>
        </div>

        <div className="com-favorito-acciones">
          {grupo.buhoRaterUrl && (
            <a
              className="com-boton com-secundario com-buho"
              href={grupo.buhoRaterUrl}
              target="_blank"
              rel="noopener noreferrer"
              aria-label={grupo.buhoRaterPerfil
                ? `Ver el perfil de ${grupo.profesor} en BuhoRater (se abre en otra pestaña)`
                : `Buscar a ${grupo.profesor} en BuhoRater (se abre en otra pestaña)`}
            >
              {grupo.buhoRaterPerfil ? "Ver en BuhoRater" : "Buscar en BuhoRater"} <ExternalLink size={14} />
            </a>
          )}
          {alQuitar && (
            <button
              type="button"
              className="com-icono com-peligro"
              aria-label={`Quitar ${grupo.materia} del horario`}
              disabled={ocupado}
              onClick={quitar}
            >
              <Trash2 size={18} />
            </button>
          )}
        </div>
      </div>

      {puedeVincular && alQuitar && grupo.profesor && <VincularBuhoRater grupo={grupo} alGuardar={alQuitar} />}

      {error && <p className="com-error" role="alert">{error}</p>}
    </article>
  );
}

// El perfil se pega una vez y queda para todos los que tengan a ese profe.
function VincularBuhoRater({ grupo, alGuardar }) {
  const [abierto, setAbierto] = useState(false);
  const [enlace, setEnlace] = useState("");
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");

  async function guardar(evento, valor = enlace) {
    evento.preventDefault();
    if (valor && !esEnlacePerfilBuhoRater(valor)) {
      setError("Ese no parece un perfil de BuhoRater. Copia el enlace que empieza con https://www.buhorater.com/perfil?id=");
      return;
    }
    setGuardando(true);
    setError("");
    try {
      await vincularBuhoRater(grupo.profesor, valor);
      setAbierto(false);
      setEnlace("");
      alGuardar();
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setGuardando(false);
    }
  }

  if (!abierto) {
    return (
      <button type="button" className="com-horario-vincular" onClick={() => setAbierto(true)}>
        {grupo.buhoRaterPerfil ? "Admin: cambiar enlace de BuhoRater" : "Admin: cargar su perfil de BuhoRater"}
      </button>
    );
  }

  return (
    <form className="com-horario-enlace" onSubmit={guardar}>
      <fieldset disabled={guardando}>
        <label>
          Enlace del perfil de {grupo.profesor} en BuhoRater
          <input
            type="url"
            inputMode="url"
            required
            autoFocus
            placeholder="https://www.buhorater.com/perfil?id=…"
            value={enlace}
            onChange={(evento) => setEnlace(evento.target.value)}
          />
        </label>
        <p className="com-descripcion">Se guarda para todos los estudiantes que tienen a este profe.</p>
        {error && <p className="com-error" role="alert">{error}</p>}
        <div className="com-acciones">
          <button className="com-boton" type="submit">{guardando ? "Guardando…" : "Guardar enlace"}</button>
          {grupo.buhoRaterPerfil && (
            <button className="com-boton com-secundario" type="button" onClick={(evento) => guardar(evento, "")}>Quitar enlace</button>
          )}
          <button className="com-boton com-secundario" type="button" onClick={() => { setAbierto(false); setError(""); }}>Cancelar</button>
        </div>
      </fieldset>
    </form>
  );
}

function SubirHorarioPdf({ alGuardar }) {
  const [vista, setVista] = useState(null);
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState("");

  async function leer(evento) {
    const archivo = evento.target.files?.[0];
    evento.target.value = "";
    if (!archivo) return;
    setOcupado(true);
    setError("");
    setVista(null);
    try {
      setVista(await leerHorarioPdf(archivo));
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setOcupado(false);
    }
  }

  async function confirmar() {
    setOcupado(true);
    setError("");
    try {
      await guardarHorario(vista);
      setVista(null);
      alGuardar();
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setOcupado(false);
    }
  }

  return (
    <section className="com-seccion com-horario-subir" aria-busy={ocupado}>
      <div className="com-interior">
        <h2 className="com-horario-titulo"><FileUp size={18} /> Sube tu horario en PDF</h2>
        <p className="com-descripcion">
          Descarga el &quot;Horario de reinscripción&quot; del portal de alumnos de la UNISON y súbelo aquí.
          Lo leemos y te mostramos tus clases antes de guardarlas.
        </p>

        <label className="com-boton com-horario-archivo">
          {ocupado && !vista ? "Leyendo PDF…" : "Elegir PDF"}
          <input type="file" accept="application/pdf,.pdf" onChange={leer} disabled={ocupado} />
        </label>

        {error && <p className="com-error" role="alert">{error}</p>}

        {vista && (
          <div className="com-horario-vista">
            <p className="com-contador">
              Encontramos {vista.length} clases en {agruparPorMateria(vista).length} materias. Revisa que estén bien:
            </p>
            <div className="com-lista">
              {agruparPorMateria(vista).map((grupo) => (
                <TarjetaMateria key={grupo.id} grupo={grupo} />
              ))}
            </div>
            <p className="com-descripcion">Al guardar se reemplaza el horario que ya tenías.</p>
            <div className="com-acciones">
              <button type="button" className="com-boton" onClick={confirmar} disabled={ocupado}>
                {ocupado ? "Guardando…" : "Guardar este horario"}
              </button>
              <button type="button" className="com-boton com-secundario" onClick={() => setVista(null)} disabled={ocupado}>
                Cancelar
              </button>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

const claseVacia = { materia: "", profesor: "", dia: 1, horaInicio: "07:00", horaFin: "08:00", salon: "" };

function AgregarClaseManual({ alGuardar }) {
  const [abierto, setAbierto] = useState(false);
  const [clase, setClase] = useState(claseVacia);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");

  const cambiar = (campo) => (evento) => setClase((actual) => ({ ...actual, [campo]: evento.target.value }));

  async function enviar(evento) {
    evento.preventDefault();
    setGuardando(true);
    setError("");
    try {
      await agregarClase(clase);
      setClase((actual) => ({ ...claseVacia, materia: actual.materia, profesor: actual.profesor, salon: actual.salon }));
      alGuardar();
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section className="com-seccion">
      <button type="button" className="com-acordeon" aria-expanded={abierto} onClick={() => setAbierto(!abierto)}>
        <Plus size={18} /> <span>Agregar o corregir una clase a mano</span>
        {abierto ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
      </button>

      {abierto && (
        <form className="com-formulario com-interior com-horario-form" onSubmit={enviar}>
          <fieldset disabled={guardando}>
            <label>Materia<input required maxLength={120} value={clase.materia} onChange={cambiar("materia")} /></label>
            <label>Profesor <span>(opcional)</span><input maxLength={120} value={clase.profesor} onChange={cambiar("profesor")} /></label>
            <div className="com-horario-campos">
              <label>Día
                <select value={clase.dia} onChange={cambiar("dia")}>
                  {DIAS.map((dia, i) => <option key={dia} value={i + 1}>{dia}</option>)}
                </select>
              </label>
              <label>Inicio<input type="time" required value={clase.horaInicio} onChange={cambiar("horaInicio")} /></label>
              <label>Fin<input type="time" required value={clase.horaFin} onChange={cambiar("horaFin")} /></label>
              <label>Salón <span>(opcional)</span><input maxLength={60} value={clase.salon} onChange={cambiar("salon")} /></label>
            </div>
            {error && <p className="com-error" role="alert">{error}</p>}
            <div className="com-acciones">
              <button className="com-boton" type="submit">{guardando ? "Guardando…" : "Agregar clase"}</button>
            </div>
          </fieldset>
        </form>
      )}
    </section>
  );
}

function ImportarPerfilesBuhoRater({ alImportar }) {
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState("");
  const [resultado, setResultado] = useState(null);

  async function importar(evento) {
    const archivo = evento.target.files?.[0];
    evento.target.value = "";
    if (!archivo) return;
    setOcupado(true);
    setError("");
    setResultado(null);
    try {
      setResultado(await importarPerfilesBuhoRater(archivo));
      alImportar();
    } catch (error) {
      setError(mensajeError(error));
    } finally {
      setOcupado(false);
    }
  }

  return (
    <section className="com-seccion com-horario-subir" aria-busy={ocupado}>
      <div className="com-interior">
        <h2 className="com-horario-titulo"><FileUp size={18} /> Admin: cargar perfiles de BuhoRater</h2>
        <p className="com-descripcion">
          Abre en tu navegador la página de BuhoRater donde salen los profes, baja hasta el final, guárdala con
          Ctrl+S (&quot;Página web completa&quot;) y súbela aquí. También acepta un CSV con nombre y enlace.
          Los botones de esos profes llevarán directo a su perfil para todos los estudiantes.
        </p>
        <label className="com-boton com-secundario com-horario-archivo">
          {ocupado ? "Leyendo…" : "Elegir archivo (.html o .csv)"}
          <input type="file" accept=".html,.htm,.csv,.txt,text/html,text/csv" onChange={importar} disabled={ocupado} />
        </label>
        {error && <p className="com-error" role="alert">{error}</p>}
        {resultado && (
          <p className="com-exito" role="status">
            Leímos {resultado.leidos} profes: {resultado.nuevos} nuevos y {resultado.actualizados} actualizados.
            {resultado.sinNombre > 0 && ` ${resultado.sinNombre} enlaces no traían el nombre del profe y se saltaron.`}
          </p>
        )}
      </div>
    </section>
  );
}

function ContenidoHorario({ esAdmin = false }) {
  const consulta = useConsulta(obtenerHorario);
  const grupos = consulta.datos ? agruparPorMateria(consulta.datos) : [];

  return (
    <>
      <SubirHorarioPdf alGuardar={consulta.actualizar} />
      <EstadoConsulta consulta={consulta} />

      {consulta.datos && (
        <>
          <p className="com-contador">{grupos.length} materias en tu horario</p>
          <div className="com-lista">
            {grupos.map((grupo) => (
              <TarjetaMateria key={grupo.id} grupo={grupo} alQuitar={consulta.actualizar} puedeVincular={esAdmin} />
            ))}
          </div>
          {grupos.length === 0 && (
            <p className="com-vacio">Todavía no tienes clases. Sube el PDF de tu horario para empezar.</p>
          )}
          <p className="com-descripcion com-horario-nota">
            Las calificaciones de los profesores están en BuhoRater, un sitio independiente de MiUni.
            El botón abre el perfil del profe en otra pestaña cuando ya está cargado en MiUni; si no, abre una búsqueda.
          </p>
        </>
      )}

      <AgregarClaseManual alGuardar={consulta.actualizar} />
      {esAdmin && <ImportarPerfilesBuhoRater alImportar={consulta.actualizar} />}
    </>
  );
}

export function MiHorario() {
  const sesion = useSesion();

  return (
    <Pantalla titulo="Mi horario" subtitulo="Tus clases y tus profes, con acceso a sus reseñas en BuhoRater" conSidebar usuario={sesion.datos}>
      <EstadoConsulta consulta={sesion} />
      {!sesion.cargando && !sesion.error && (
        sesion.datos ? <ContenidoHorario esAdmin={Boolean(sesion.datos.roles?.includes("Administrador"))} /> : <SolicitarSesion />
      )}
    </Pantalla>
  );
}
