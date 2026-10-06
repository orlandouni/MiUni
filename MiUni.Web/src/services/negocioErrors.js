export function negocioError(error) {
  if (error.response?.status === 401) return "Tu sesión venció. Vuelve a iniciar sesión.";
  if (error.response?.status === 403) return "Tu cuenta no tiene permiso para esta acción.";
  const data = error.response?.data;
  return data?.message || (data?.errors && Object.values(data.errors).flat().join(" ")) || "No pudimos completar la acción. Intenta nuevamente.";
}
