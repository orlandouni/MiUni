// Funciones puras del horario (sin dependencias del navegador, para poder probarlas con node:test).

export const DIAS = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];
export const DIAS_CORTOS = ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"];

// "07:00:00" -> "07:00"
export function hora(texto) {
  return (texto ?? "").slice(0, 5);
}

function clave(texto) {
  return (texto ?? "").trim().toLocaleUpperCase("es-MX");
}

// Junta las clases de una misma materia y profesor en una tarjeta con todos sus días.
export function agruparPorMateria(clases) {
  const grupos = new Map();
  for (const clase of clases) {
    const id = `${clave(clase.materia)}|${clave(clase.profesor)}`;
    if (!grupos.has(id)) {
      grupos.set(id, {
        id,
        materia: clase.materia,
        profesor: clase.profesor ?? null,
        buhoRaterUrl: clase.buhoRaterUrl ?? null,
        buhoRaterPerfil: Boolean(clase.buhoRaterPerfil),
        sesiones: [],
      });
    }
    const grupo = grupos.get(id);
    grupo.sesiones.push(clase);
  }
  const inicio = (g) => Math.min(...g.sesiones.map((s) => s.dia * 1440 + minutos(s.horaInicio)));
  return [...grupos.values()]
    .map((g) => {
      const sesiones = [...g.sesiones].sort((a, b) => a.dia - b.dia || minutos(a.horaInicio) - minutos(b.horaInicio));
      const salones = [...new Set(sesiones.map((s) => s.salon).filter(Boolean))];
      return { ...g, sesiones, salones };
    })
    .sort((a, b) => minutos(a.sesiones[0].horaInicio) - minutos(b.sesiones[0].horaInicio) || inicio(a) - inicio(b));
}

function minutos(texto) {
  const [h, m] = hora(texto).split(":").map(Number);
  return (h || 0) * 60 + (m || 0);
}

// "Lun, Mié 07:00–08:00 · Vie 09:00–10:00"
export function resumenSesiones(sesiones) {
  const porHorario = new Map();
  for (const s of sesiones) {
    const rango = `${hora(s.horaInicio)}–${hora(s.horaFin)}`;
    if (!porHorario.has(rango)) porHorario.set(rango, []);
    porHorario.get(rango).push(DIAS_CORTOS[s.dia - 1] ?? "?");
  }
  return [...porHorario].map(([rango, dias]) => `${dias.join(", ")} ${rango}`).join(" · ");
}

// Convierte lo leído del PDF (o una clase guardada) al cuerpo que espera la API.
export function aSolicitud(clase) {
  return {
    materia: clase.materia,
    profesor: clase.profesor || null,
    dia: Number(clase.dia),
    horaInicio: hora(clase.horaInicio),
    horaFin: hora(clase.horaFin),
    salon: clase.salon || null,
  };
}

// Enlace de perfil de BuhoRater (https://www.buhorater.com/perfil?id=<uuid>) o el id suelto.
// La API vuelve a validarlo; esto solo evita mandar algo que claramente no es un perfil.
export function esEnlacePerfilBuhoRater(texto) {
  const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  const limpio = (texto ?? "").trim();
  if (uuid.test(limpio)) return true;
  try {
    const url = new URL(limpio.includes("://") ? limpio : `https://${limpio}`);
    return ["www.buhorater.com", "buhorater.com"].includes(url.hostname.toLowerCase())
      && url.pathname.replace(/\/+$/, "").toLowerCase() === "/perfil"
      && uuid.test(url.searchParams.get("id") ?? "");
  } catch {
    return false;
  }
}
