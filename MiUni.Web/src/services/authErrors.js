const identityMessages = {
  PasswordRequiresNonAlphanumeric: "Incluye un símbolo en la contraseña.",
  PasswordRequiresDigit: "Incluye un número en la contraseña.",
  PasswordRequiresUpper: "Incluye una mayúscula en la contraseña.",
  PasswordRequiresLower: "Incluye una minúscula en la contraseña.",
  PasswordTooShort: "La contraseña debe tener al menos 8 caracteres.",
  DuplicateUserName: "Ya existe una cuenta con ese correo. Inicia sesión.",
  DuplicateEmail: "Ya existe una cuenta con ese correo. Inicia sesión.",
};

export function authErrorMessage(error) {
  const status = error.response?.status;
  const data = error.response?.data;
  if (!error.response) return "No pudimos conectar con el servidor. Intenta de nuevo en unos momentos.";
  if (status === 404) return "El servicio de registro no está disponible. Intenta de nuevo más tarde.";
  if (status >= 500) return "El servicio no está disponible en este momento. Intenta de nuevo más tarde.";
  if (Array.isArray(data)) {
    return [...new Set(data.map((item) => identityMessages[item.code || item.Code] || item.description || item.Description).filter(Boolean))].join(" ") || "Revisa los datos del formulario.";
  }
  if (data?.errors) return Object.values(data.errors).flat().join(" ");
  return data?.message || "No pudimos crear tu cuenta. Revisa tus datos e inténtalo de nuevo.";
}
