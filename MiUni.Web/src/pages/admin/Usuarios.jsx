import { useState } from "react";
import { Ban, Search, ShieldCheck, Unlock } from "lucide-react";
import { adminApi, limpiarParams } from "../../services/admin";
import { negocioError } from "../../services/negocioErrors";
import { ROLES } from "../../services/roles";
import { Aviso, Cargando, Confirmacion, Encabezado, ErrorCarga, Insignia, Modal, Paginacion, Vacio, fecha, useAccion, useCarga } from "./ui";

const TAMANO = 20;
const TONO_ROL = { [ROLES.admin]: "primario", [ROLES.propietario]: "alerta", [ROLES.usuario]: "neutro" };

export default function Usuarios({ yo, onCambio }) {
  const [filtro, setFiltro] = useState({ q: "", rol: "", bloqueado: "" });
  const [busqueda, setBusqueda] = useState("");
  const [pagina, setPagina] = useState(1);
  const params = limpiarParams({ ...filtro, pagina, tamanoPagina: TAMANO });
  const { datos, cargando, error, recargar, setDatos } = useCarga(() => adminApi.usuarios(params), JSON.stringify(params));
  const { ocupado, aviso, setAviso, ejecutar } = useAccion();
  const [editandoRoles, setEditandoRoles] = useState(null);
  const [confirmar, setConfirmar] = useState(null);

  const cambiar = cambio => { setFiltro(f => ({ ...f, ...cambio })); setPagina(1); };
  const reemplazar = usuario => setDatos(d => ({ ...d, items: d.items.map(u => u.id === usuario.id ? usuario : u) }));

  const alternarBloqueo = u => {
    const accion = async () => {
      if (await ejecutar(u.id, () => adminApi.bloquearUsuario(u.id, !u.bloqueado), u.bloqueado ? `${u.nombre} puede volver a entrar.` : `Cuenta de ${u.nombre} suspendida.`)) {
        reemplazar({ ...u, bloqueado: !u.bloqueado }); onCambio();
      }
    };
    if (u.bloqueado) return accion();
    setConfirmar({
      titulo: "Suspender cuenta", boton: "Suspender",
      texto: `${u.nombre} no podrá iniciar sesión y su sesión actual se cerrará. Su contenido no se elimina.`, accion,
    });
  };

  return <section className="adm-card">
    <Encabezado titulo="Usuarios" descripcion="Gestiona roles y acceso de las cuentas registradas." />
    <Aviso aviso={aviso || (error && datos && { tipo: "error", texto: error })} onCerrar={() => setAviso(null)} />
    <form className="adm-filters" onSubmit={e => { e.preventDefault(); cambiar({ q: busqueda }); }}>
      <label className="adm-search"><Search size={15} /><input type="search" placeholder="Nombre o correo" value={busqueda}
        onChange={e => { setBusqueda(e.target.value); if (!e.target.value) cambiar({ q: "" }); }} /></label>
      <select aria-label="Rol" value={filtro.rol} onChange={e => cambiar({ rol: e.target.value })}>
        <option value="">Todos los roles</option>
        {Object.values(ROLES).map(r => <option key={r} value={r}>{r}</option>)}
      </select>
      <select aria-label="Acceso" value={filtro.bloqueado} onChange={e => cambiar({ bloqueado: e.target.value })}>
        <option value="">Cualquier acceso</option><option value="false">Activas</option><option value="true">Suspendidas</option>
      </select>
    </form>

    {cargando && !datos ? <Cargando texto="Cargando usuarios…" /> : error && !datos ? <ErrorCarga error={error} onReintentar={recargar} /> : datos?.items.length === 0 ?
      <Vacio titulo="Sin usuarios" texto="Nadie coincide con estos filtros." /> :
      <div className={`adm-table-wrap${cargando ? " recargando" : ""}`}><table className="adm-table">
        <thead><tr><th>Usuario</th><th>Roles</th><th>Carrera</th><th>Actividad</th><th>Registro</th><th aria-label="Acciones" /></tr></thead>
        <tbody>{datos?.items.map(u => <tr key={u.id} className={u.bloqueado ? "inactivo" : ""}>
          <td><strong>{u.nombre}{u.id === yo && " (tú)"}</strong><small>{u.email}</small>{u.bloqueado && <Insignia tono="peligro">Suspendida</Insignia>}</td>
          <td><div className="adm-badges">{u.roles.map(r => <Insignia key={r} tono={TONO_ROL[r]}>{r}</Insignia>)}</div></td>
          <td>{u.nombreCarrera || "—"}{u.semestre ? <small>{u.semestre}.º semestre</small> : null}</td>
          <td><small>{u.resenas} reseñas · {u.reportes} reportes · {u.negocios} negocios</small></td>
          <td>{fecha(u.fechaRegistro)}</td>
          <td className="adm-row-actions">
            <button type="button" className="adm-icon-btn" title="Roles" aria-label={`Editar roles de ${u.nombre}`} onClick={() => setEditandoRoles(u)}><ShieldCheck size={15} /></button>
            <button type="button" className={`adm-icon-btn${u.bloqueado ? "" : " peligro"}`} disabled={u.id === yo || ocupado === u.id}
              title={u.bloqueado ? "Reactivar" : "Suspender"} aria-label={`${u.bloqueado ? "Reactivar" : "Suspender"} a ${u.nombre}`} onClick={() => alternarBloqueo(u)}>
              {u.bloqueado ? <Unlock size={15} /> : <Ban size={15} />}
            </button>
          </td>
        </tr>)}</tbody>
      </table></div>}
    {datos && <Paginacion pagina={pagina} total={datos.total} tamano={TAMANO} onCambio={setPagina} />}

    {editandoRoles && <FormRoles usuario={editandoRoles} esYo={editandoRoles.id === yo} onCerrar={() => setEditandoRoles(null)}
      onGuardado={u => { reemplazar(u); setEditandoRoles(null); setAviso({ tipo: "ok", texto: `Roles de ${u.nombre} actualizados.` }); recargar(); }} />}
    <Confirmacion confirmar={confirmar} onCerrar={() => setConfirmar(null)} ocupado={!!ocupado} />
  </section>;
}

function FormRoles({ usuario, esYo, onCerrar, onGuardado }) {
  const [roles, setRoles] = useState(() => usuario.roles.filter(r => r !== ROLES.usuario));
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");
  const alternar = rol => setRoles(rs => rs.includes(rol) ? rs.filter(r => r !== rol) : [...rs, rol]);
  async function guardar(e) {
    e.preventDefault();
    setGuardando(true); setError("");
    try { onGuardado(await adminApi.rolesUsuario(usuario.id, roles)); }
    catch (err) { setError(negocioError(err)); }
    finally { setGuardando(false); }
  }
  const opciones = [
    { rol: ROLES.propietario, texto: "Puede editar los negocios que tiene aprobados." },
    { rol: ROLES.admin, texto: "Acceso completo a este panel de administración." },
  ];
  return <Modal titulo={`Roles de ${usuario.nombre}`} onCerrar={onCerrar} ancho={460}>
    <form className="adm-form" onSubmit={guardar}>
      {error && <p className="adm-error" role="alert">{error}</p>}
      <label className="adm-role-option deshabilitado"><input type="checkbox" checked disabled /><span><strong>{ROLES.usuario}</strong><small>Rol base de toda cuenta.</small></span></label>
      {opciones.map(o => {
        const bloqueado = esYo && o.rol === ROLES.admin;
        return <label key={o.rol} className={`adm-role-option${bloqueado ? " deshabilitado" : ""}`}>
          <input type="checkbox" checked={roles.includes(o.rol)} disabled={bloqueado} onChange={() => alternar(o.rol)} />
          <span><strong>{o.rol}</strong><small>{bloqueado ? "No puedes quitarte tu propio rol de administrador." : o.texto}</small></span>
        </label>;
      })}
      <div className="adm-modal-actions">
        <button type="button" className="adm-btn" onClick={onCerrar}>Cancelar</button>
        <button type="submit" className="adm-btn primario" disabled={guardando}>{guardando ? "Guardando…" : "Guardar roles"}</button>
      </div>
    </form>
  </Modal>;
}
