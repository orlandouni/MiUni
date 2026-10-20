export const ROLES = { usuario: "Estudiante", propietario: "Propietario", admin: "Administrador" };

export function landingFor(roles = []) {
  if (roles.includes(ROLES.admin)) return "/admin";
  if (roles.includes(ROLES.propietario)) return "/negocios";
  return "/";
}

export function canManageBusiness(roles = []) {
  return roles.includes(ROLES.admin) || roles.includes(ROLES.propietario);
}
