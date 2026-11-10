import { useEffect, useState } from "react";
import { FileUp, GraduationCap, Pencil, Plus, Save, Trash2 } from "lucide-react";
import { ESTADOS_MATERIA, eliminarMateria, guardarKardex, guardarMateria, leerKardexPdf, obtenerKardex } from "../services/kardex";
import { mensajeError } from "../services/comunidad";

const VACIO = { materia: "", clave: "", periodo: "", semestre: "", calificacion: "", creditos: "", estado: "Aprobada" };

// Kárdex del estudiante: lo usa ChatMuni para responder dudas sobre sus materias y créditos.
export default function KardexSeccion() {
  const [kardex, setKardex] = useState({ materias: [], resumen: null });
  const [form, setForm] = useState(null);
  const [error, setError] = useState("");
  const [vista, setVista] = useState(null);
  const [ocupado, setOcupado] = useState(false);

  async function cargar() {
    try { setKardex(await obtenerKardex()); }
    catch (err) { setError(mensajeError(err)); }
  }
  useEffect(() => { void Promise.resolve().then(cargar); }, []);

  async function guardar(e) {
    e.preventDefault(); setError("");
    try { await guardarMateria(form.id, form); setForm(null); await cargar(); }
    catch (err) { setError(mensajeError(err)); }
  }
  async function borrar(m) {
    if (!window.confirm(`¿Quitar ${m.materia} de tu kárdex?`)) return;
    try { await eliminarMateria(m.id); await cargar(); }
    catch (err) { setError(mensajeError(err)); }
  }
  async function leerPdf(e) {
    const archivo = e.target.files?.[0];
    e.target.value = "";
    if (!archivo) return;
    setOcupado(true); setError(""); setVista(null); setForm(null);
    try { setVista(await leerKardexPdf(archivo)); }
    catch (err) { setError(mensajeError(err)); }
    finally { setOcupado(false); }
  }
  async function confirmarPdf() {
    setOcupado(true); setError("");
    try { setKardex(await guardarKardex(vista)); setVista(null); }
    catch (err) { setError(mensajeError(err)); }
    finally { setOcupado(false); }
  }
  const detalle = m => [m.estado, m.calificacion != null && `Calificación ${m.calificacion}`, `${m.creditos} créditos`, m.semestre && `${m.semestre}° semestre`, m.periodo].filter(Boolean).join(" · ");
  const editar = m => setForm({ ...VACIO, ...Object.fromEntries(Object.entries(m).map(([k, v]) => [k, v ?? ""])) });
  const campo = (k, v) => setForm(f => ({ ...f, [k]: v }));
  const r = kardex.resumen;

  return <section id="mi-kardex" className="perfil-seccion perfil-kardex">
    <header><h2><GraduationCap size={18}/> Mi kárdex</h2><span>{kardex.materias.length}</span></header>
    <p className="perfil-kardex-ayuda">ChatMuni usa tu kárdex y tu horario para responder dudas sobre tus materias, créditos y clases.</p>
    {error && <div className="perfil-alerta" role="alert">{error}</div>}
    {r && kardex.materias.length > 0 && <div className="perfil-kardex-resumen">
      <article><span>CRÉDITOS APROBADOS</span><strong>{r.creditosAprobados}</strong></article>
      <article><span>PROMEDIO</span><strong>{r.promedio ?? "—"}</strong></article>
      <article><span>APROBADAS</span><strong>{r.materiasAprobadas}</strong></article>
      <article><span>CURSANDO</span><strong>{r.materiasCursando}</strong></article>
    </div>}
    {form ? <form className="perfil-kardex-form" onSubmit={guardar}>
      <div className="perfil-campo perfil-kardex-ancho"><label htmlFor="kx-materia">Materia</label><input id="kx-materia" required maxLength={120} value={form.materia} onChange={e => campo("materia", e.target.value)}/></div>
      <div className="perfil-campo"><label htmlFor="kx-estado">Estado</label><select id="kx-estado" value={form.estado} onChange={e => campo("estado", e.target.value)}>{ESTADOS_MATERIA.map(s => <option key={s.valor} value={s.valor}>{s.etiqueta}</option>)}</select></div>
      <div className="perfil-campo"><label htmlFor="kx-calificacion">Calificación (0 a 100)</label><input id="kx-calificacion" type="number" min={0} max={100} step="0.1" value={form.calificacion} onChange={e => campo("calificacion", e.target.value)}/></div>
      <div className="perfil-campo"><label htmlFor="kx-creditos">Créditos</label><input id="kx-creditos" type="number" min={0} max={30} value={form.creditos} onChange={e => campo("creditos", e.target.value)}/></div>
      <div className="perfil-campo"><label htmlFor="kx-semestre">Semestre</label><select id="kx-semestre" value={form.semestre} onChange={e => campo("semestre", e.target.value)}><option value="">Sin especificar</option>{Array.from({ length: 12 }, (_, i) => <option key={i + 1} value={i + 1}>{i + 1}° semestre</option>)}</select></div>
      <div className="perfil-campo"><label htmlFor="kx-periodo">Periodo</label><input id="kx-periodo" maxLength={20} placeholder="2026-2" value={form.periodo} onChange={e => campo("periodo", e.target.value)}/></div>
      <div className="perfil-campo"><label htmlFor="kx-clave">Clave</label><input id="kx-clave" maxLength={20} value={form.clave} onChange={e => campo("clave", e.target.value)}/></div>
      <div className="perfil-resena-acciones perfil-kardex-ancho"><button className="perfil-guardar"><Save size={16}/> Guardar materia</button><button className="perfil-cancelar" type="button" onClick={() => setForm(null)}>Cancelar</button></div>
    </form> : !vista && <div className="perfil-kardex-botones">
      <button className="perfil-cancelar perfil-kardex-agregar" type="button" onClick={() => setForm({ ...VACIO })}><Plus size={16}/> Agregar materia</button>
      <label className="perfil-cancelar perfil-kardex-agregar perfil-kardex-archivo" aria-busy={ocupado}><FileUp size={16}/> {ocupado ? "Leyendo PDF…" : "Subir kárdex en PDF"}<input type="file" accept="application/pdf,.pdf" onChange={leerPdf} disabled={ocupado}/></label>
    </div>}
    {!form && !vista && <p className="perfil-kardex-ayuda">Descarga el &quot;Kárdex electrónico&quot; del portal de alumnos de la UNISON y súbelo: te mostramos tus materias antes de guardarlas.</p>}
    {vista && <div className="perfil-kardex-vista">
      <p className="perfil-kardex-ayuda">Encontramos {vista.length} materias. Revisa que estén bien; al guardar se reemplaza el kárdex que ya tenías.</p>
      <div className="perfil-lista">{vista.map((m, i) => <article className="perfil-resena" key={`${m.clave}-${m.periodo}-${i}`}><strong>{m.materia}</strong><small className="perfil-kardex-detalle">{detalle(m)}</small></article>)}</div>
      <div className="perfil-resena-acciones perfil-kardex-confirmar"><button className="perfil-guardar" type="button" onClick={confirmarPdf} disabled={ocupado}><Save size={16}/> {ocupado ? "Guardando…" : "Guardar este kárdex"}</button><button className="perfil-cancelar" type="button" onClick={() => setVista(null)} disabled={ocupado}>Cancelar</button></div>
    </div>}
    {vista ? null : kardex.materias.length ? <div className="perfil-lista">{kardex.materias.map(m => <article className="perfil-resena" key={m.id}>
      <div className="perfil-resena-top">
        <div><strong>{m.materia}</strong><small className="perfil-kardex-detalle">{detalle(m)}</small></div>
        <div className="perfil-resena-acciones"><button type="button" aria-label={`Editar ${m.materia}`} title="Editar materia" onClick={() => editar(m)}><Pencil size={16}/></button><button type="button" aria-label={`Quitar ${m.materia}`} title="Quitar materia" onClick={() => borrar(m)}><Trash2 size={16}/></button></div>
      </div>
    </article>)}</div> : !form && <p className="perfil-vacio">Agrega las materias que has cursado o sube tu kárdex en PDF para que ChatMuni conozca tu avance.</p>}
  </section>;
}
