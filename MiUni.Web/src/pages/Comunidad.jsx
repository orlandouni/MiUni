import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Building2,
  ChevronDown,
  ChevronUp,
  Eye,
  MessageSquare,
  Pencil,
  Flag,
  Star,
  Trash2,
} from "lucide-react";

import api, { getMe, getToken } from "../services/api";
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

          <Link to="/login">Iniciar sesión</Link>
        </div>
      </div>
    );
  }

  return null;
}

function Pantalla({ titulo, subtitulo, children }) {
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
    setEnviandoReporte(true);
    setError("");
    try {
      await api.post("/api/ReporteUsuario", { resenaId: resena.id, motivo });
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
        {puedeReportar && !propia && <button className="com-icono" type="button" aria-label="Reportar reseña" onClick={() => setReportando(value => !value)}><Flag size={17} /></button>}
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

      {reportando && <form className="com-formulario com-reporte-form" onSubmit={reportar}>
        <label>¿Por qué reportas esta reseña?
          <textarea required minLength={3} maxLength={2000} rows={3} value={motivo} onChange={event => setMotivo(event.target.value)} placeholder="Describe el problema" />
        </label>
        <div className="com-acciones"><button className="com-boton" disabled={enviandoReporte}>{enviandoReporte ? "Enviando…" : "Enviar reporte"}</button><button className="com-boton com-secundario" type="button" onClick={() => setReportando(false)}>Cancelar</button></div>
      </form>}
      {reportando && error && <p className="com-error" role="alert">{error}</p>}
      {reporteEnviado && <p className="com-exito" role="status">Gracias. El equipo revisará tu reporte.</p>}

      {error && <p className="com-error" role="alert">{error}</p>}
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
      obtenerLugares(),
    ]);
    return { favoritos, lugares };
  }, []);

  const consulta = useConsulta(consultar);

  return (
    <>
      <EstadoConsulta consulta={consulta} />

      {consulta.datos && (
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
    <Pantalla titulo="Mis favoritos" subtitulo="Tus lugares guardados del campus">
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
      obtenerLugares(),
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
    <Pantalla titulo="Mis reseñas" subtitulo="Tu experiencia en el campus">
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
    setEnviandoReporteLugar(true); setErrorReporteLugar("");
    try {
      await api.post("/api/ReporteUsuario", { lugarId, motivo: motivoLugar });
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

            {sesion.datos && (
              <><BotonFavorito key={sesion.datos.id} lugarId={lugarId} />
                <div className="com-reporte-lugar"><button className="com-boton com-secundario" type="button" onClick={() => setReportandoLugar(value => !value)}><Flag size={16} /> Reportar lugar</button>
                  {reporteLugarEnviado && <p className="com-exito" role="status">Gracias. El equipo revisará tu reporte.</p>}
                  {reportandoLugar && <form className="com-formulario" onSubmit={reportarLugar}><label>Motivo del reporte<textarea required minLength={3} maxLength={2000} rows={3} value={motivoLugar} onChange={event => setMotivoLugar(event.target.value)} /></label>{errorReporteLugar && <p role="alert" className="com-error">{errorReporteLugar}</p>}<div className="com-acciones"><button className="com-boton" disabled={enviandoReporteLugar}>{enviandoReporteLugar ? "Enviando…" : "Enviar reporte"}</button><button className="com-boton com-secundario" type="button" onClick={() => setReportandoLugar(false)}>Cancelar</button></div></form>}
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
