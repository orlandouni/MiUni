import { useMemo, useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { adminApi } from "../../services/admin";
import { negocioError } from "../../services/negocioErrors";
import { Aviso, Cargando, Confirmacion, Encabezado, ErrorCarga, Modal, Vacio, useAccion, useCarga } from "./ui";

// Tabla + formulario genérico para catálogos simples (categorías, carreras).
function Catalogo({ titulo, descripcion, singular, cargar, crear, editar, eliminar, campos, columnas, onCambio }) {
  const { datos, cargando, error, recargar } = useCarga(cargar);
  const { ocupado, aviso, setAviso, ejecutar } = useAccion();
  const [editando, setEditando] = useState(null);
  const [confirmar, setConfirmar] = useState(null);

  const tras = ok => { if (ok) { recargar(); onCambio?.(); } };
  const borrar = item => setConfirmar({
    titulo: `Eliminar ${singular}`, boton: "Eliminar",
    texto: `"${item.nombre}" se eliminará de forma permanente.`,
    accion: async () => tras(await ejecutar(item.id, () => eliminar(item.id), `${item.nombre} eliminado.`)),
  });

  return <section className="adm-card">
    <Encabezado titulo={titulo} descripcion={descripcion}>
      <button type="button" className="adm-btn primario" onClick={() => setEditando(Object.fromEntries(campos.map(c => [c.campo, ""])))}><Plus size={15} /> Nueva {singular}</button>
    </Encabezado>
    <Aviso aviso={aviso || (error && datos && { tipo: "error", texto: error })} onCerrar={() => setAviso(null)} />
    {cargando && !datos ? <Cargando /> : error && !datos ? <ErrorCarga error={error} onReintentar={recargar} /> : datos?.length === 0 ? <Vacio titulo={`Sin ${titulo.toLowerCase()}`} /> :
      <div className="adm-table-wrap"><table className="adm-table">
        <thead><tr>{columnas.map(c => <th key={c.titulo}>{c.titulo}</th>)}<th aria-label="Acciones" /></tr></thead>
        <tbody>{datos?.map(item => <tr key={item.id}>
          {columnas.map(c => <td key={c.titulo}>{c.valor(item)}</td>)}
          <td className="adm-row-actions">
            <button type="button" className="adm-icon-btn" aria-label={`Editar ${item.nombre}`} title="Editar" onClick={() => setEditando(item)}><Pencil size={15} /></button>
            <button type="button" className="adm-icon-btn peligro" aria-label={`Eliminar ${item.nombre}`} title="Eliminar" disabled={ocupado === item.id} onClick={() => borrar(item)}><Trash2 size={15} /></button>
          </td>
        </tr>)}</tbody>
      </table></div>}

    {editando && <FormCatalogo item={editando} singular={singular} campos={campos} onCerrar={() => setEditando(null)}
      guardar={valores => editando.id ? editar(editando.id, valores) : crear(valores)}
      onGuardado={() => { setAviso({ tipo: "ok", texto: editando.id ? "Cambios guardados." : `${singular[0].toUpperCase()}${singular.slice(1)} creada.` }); setEditando(null); tras(true); }} />}
    <Confirmacion confirmar={confirmar} onCerrar={() => setConfirmar(null)} ocupado={!!ocupado} />
  </section>;
}

function FormCatalogo({ item, singular, campos, guardar, onCerrar, onGuardado }) {
  const [valores, setValores] = useState(() => Object.fromEntries(campos.map(c => [c.campo, item[c.campo] ?? ""])));
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");
  async function enviar(e) {
    e.preventDefault();
    setGuardando(true); setError("");
    try {
      await guardar(Object.fromEntries(Object.entries(valores).map(([k, v]) => [k, v.trim() || null])));
      onGuardado();
    } catch (err) { setError(negocioError(err)); }
    finally { setGuardando(false); }
  }
  return <Modal titulo={item.id ? `Editar ${item.nombre}` : `Nueva ${singular}`} onCerrar={onCerrar} ancho={480}>
    <form className="adm-form" onSubmit={enviar}>
      {error && <p className="adm-error" role="alert">{error}</p>}
      {campos.map(c => <label key={c.campo}>{c.etiqueta}
        <input required={c.requerido} maxLength={c.max} placeholder={c.ayuda} value={valores[c.campo]} onChange={e => setValores(v => ({ ...v, [c.campo]: e.target.value }))} />
      </label>)}
      <div className="adm-modal-actions">
        <button type="button" className="adm-btn" onClick={onCerrar}>Cancelar</button>
        <button type="submit" className="adm-btn primario" disabled={guardando}>{guardando ? "Guardando…" : "Guardar"}</button>
      </div>
    </form>
  </Modal>;
}

export function Categorias({ onCambio }) {
  // Conteo de lugares por categoría a partir del listado administrativo.
  const lugares = useCarga(adminApi.lugares);
  const conteo = useMemo(() => (lugares.datos || []).reduce((m, l) => ({ ...m, [l.categoriaId]: (m[l.categoriaId] || 0) + 1 }), {}), [lugares.datos]);
  return <Catalogo titulo="Categorías" descripcion="Clasificación de los lugares del mapa. Solo se pueden eliminar si no tienen lugares." singular="categoría"
    cargar={adminApi.categorias} crear={adminApi.crearCategoria} editar={adminApi.editarCategoria} eliminar={adminApi.eliminarCategoria}
    onCambio={() => { lugares.recargar(); onCambio(); }}
    campos={[{ campo: "nombre", etiqueta: "Nombre", requerido: true, max: 100 }, { campo: "icono", etiqueta: "Ícono", max: 500, ayuda: "Nombre o URL del ícono (opcional)" }]}
    columnas={[
      { titulo: "Nombre", valor: c => <strong>{c.nombre}</strong> },
      { titulo: "Ícono", valor: c => c.icono || "—" },
      { titulo: "Lugares", valor: c => conteo[c.id] ?? 0 },
    ]} />;
}

export function Carreras() {
  return <Catalogo titulo="Carreras" descripcion="Programas que los estudiantes pueden asociar a su perfil." singular="carrera"
    cargar={adminApi.carreras} crear={adminApi.crearCarrera} editar={adminApi.editarCarrera} eliminar={adminApi.eliminarCarrera}
    campos={[{ campo: "nombre", etiqueta: "Nombre", requerido: true, max: 150 }, { campo: "facultad", etiqueta: "Facultad / división", max: 150 }]}
    columnas={[
      { titulo: "Nombre", valor: c => <strong>{c.nombre}</strong> },
      { titulo: "Facultad", valor: c => c.facultad || "—" },
      { titulo: "Estudiantes", valor: c => c.usuarios },
    ]} />;
}
