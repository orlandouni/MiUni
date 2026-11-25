# Pruebas unitarias de MiUni con MSTest

Proyecto de pruebas unitarias (MSTest, C#) para la API de MiUni. No necesita PostgreSQL,
el servicio RAG ni internet: las pruebas que usan datos corren sobre una base en memoria.

## Cómo ejecutarlas (Windows, PowerShell o CMD, desde la carpeta raíz del repo)

```powershell
# 1. Todas las pruebas que deben pasar (resultado: PASSED)
dotnet test MiUni.Api.UnitTests --filter "TestCategory!=DefectoConocido" --logger "console;verbosity=detailed"

# 2. Todas, incluyendo la que exhibe un defecto real (resultado: 1 FAILED)
dotnet test MiUni.Api.UnitTests --logger "console;verbosity=detailed"

# 3. Un solo módulo (cambia el nombre de la clase)
dotnet test MiUni.Api.UnitTests --filter "FullyQualifiedName~AutenticacionTests" --logger "console;verbosity=detailed"
```

Las clases de cada módulo son `AutenticacionTests`, `KardexTests`, `MapaTests`,
`HorarioProfesoresTests` y `ChatMuniTests`. En Visual Studio también aparecen en el
**Explorador de pruebas** (Prueba → Explorador de pruebas), agrupables por *Rasgo* (categoría).

Cada prueba tiene una categoría (`TestCategory`): **Caso normal**, **Caso de error**,
**Caso límite** o **Regla de negocio**, y una descripción con el resultado esperado.
Todas siguen el patrón Arrange / Act / Assert.

## Módulos y pruebas

### 1. Autenticación y gestión de usuarios (`AutenticacionTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Registro_DatosCompletos_PasaLaValidacion | Normal | Sin errores de validación |
| Registro_CorreoMalFormado_DaError | Error | "El correo no tiene un formato válido." |
| Login_SinContrasena_DaError | Error | "La contraseña es obligatoria." |
| Registro_LongitudMinimaDeContrasena (7 / 8 caracteres) | Límite | 7 se rechaza, 8 se acepta |
| Perfil_RangoDeSemestre (0 / 1 / 12 / 13) | Límite | Solo 1 a 12 son válidos |
| Registro_EstudianteConCorreoPersonal_EsRechazado | Negocio | 400: exige correo @unison.mx |
| Registro_ComoAdministrador_EsRechazado | Negocio | 400: solo Estudiante o Propietario |
| Roles_GestionDeNegocios_IncluyePropietarioYAdministrador | Negocio | Propietario y Administrador, no Estudiante |

### 2. Kárdex académico (`KardexTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Resumen_KardexTipico_CalculaCreditosPromedioYConteos | Normal | 18 créditos, promedio 70, 2/1/1 materias |
| Resumen_KardexVacio_SinPromedioYEnCeros | Límite | Promedio nulo, todo en cero |
| Resumen_PromedioConDecimales_SeRedondeaADos | Límite | 95.17 |
| Resumen_BajasYMateriasEnCurso_NoAfectanElPromedio | Negocio | Promedio 100, 10 créditos |
| Captura_RangoDeCalificacion (-1 / 0 / 100 / 101) | Límite | Solo 0 a 100 son válidas |
| Captura_CalificacionConDecimalesArribaDe100_DeberiaRechazarse | Límite (**defecto**) | **FALLA**: 100.4 se acepta (ver abajo) |
| Captura_SinNombreYConDemasiadosCreditos_DaDosErrores | Error | Dos mensajes de validación |
| Chat_AlumnoSinKardex_LeIndicaSubirlo | Error | "No tienes materias guardadas en tu kárdex" |
| Chat_ConsultaDeCreditos_SoloUsaElKardexPropio | Negocio | No aparecen materias de otro alumno |
| Chat_PreguntasQueNoSonDelKardexPropio_NoSeContestanAqui | Negocio | Trámites y otras preguntas van al RAG |

### 3. Mapa del campus y rutas (`MapaTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Lugar_DentroDeSuHorario_EstaAbierto | Normal | Abierto a las 10:30 (horario 07–15) |
| Lugar_HorasFrontera (06:59 / 07:00 / 14:59 / 15:00) | Límite | Abre a las 07:00 en punto, cierra a las 15:00 en punto |
| Lugar_HorarioQueCruzaMedianoche_SigueAbiertoAlDiaSiguiente | Negocio | 22:00–02:00: abierto a la 01:00, cerrado a las 02:00 |
| Lugar_SinHorarioParaEseDia_EstaCerrado | Error | Cerrado |
| Ruta_CoordenadasDelCampus_SonValidas | Normal | Sin errores |
| Ruta_ValoresFrontera (lat ±90, lon ±180, radio 1–1000 m) | Límite | Fuera de rango se rechaza |
| Ruta_PuntoSinLatitud_DaError | Error | Error en Latitud |
| Chat_PreguntaDondeEsta_EncuentraElLugar | Normal | "Encontré Biblioteca Central." |
| Chat_PreguntaPorHorarioDeUnLugar_NoLaContestaElMapa | Negocio | Sin respuesta (la da el RAG) |
| Chat_LugarInexistente_NoDevuelveRespuesta | Error | Sin respuesta, no inventa lugares |
| Chat_ListaDeLugares_MuestraMaximoDiez | Límite | "mostrando 10 de 15" |

### 4. Horario y profesores (`HorarioProfesoresTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Horario_ClasesDeHoy_ListaSoloLasDelDia | Normal | Solo las clases del miércoles |
| Horario_AlumnoSinHorario_LeIndicaCapturarlo | Error | "No tienes un horario guardado." |
| Horario_ClaseActual_EnElCambioDeHora | Límite | A las 10:00 cuenta la clase de 10–11, no la de 9–10 |
| Horario_PreguntaPorFestivos_AclaraQueNoSeGuardan | Negocio | Aclara que no guarda festivos |
| BuhoRater_EnlaceDePerfilValido_SeAcepta | Normal | Extrae el id del perfil |
| BuhoRater_EnlaceInvalido_SeRechaza (4 enlaces) | Error | Dominio falso, otra página, id inválido o FTP: rechazados |
| BuhoRater_IdEnCerosYNombreVacio_SeRechazan | Límite | Rechazados / nulos |
| BuhoRater_MismoProfesorEscritoDistinto_TieneLaMismaClave | Negocio | Misma clave sin importar orden ni acentos |

### 5. ChatMuni (`ChatMuniTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Historial_TurnosCompletos_SeConservanEnOrden | Normal | user, assistant, user, assistant |
| Historial_PreguntaSinRespuesta_SeDescarta | Error | Solo el turno completo |
| Historial_MuchosTurnos_SeRecortaAlMaximoDeMensajes | Límite | 12 mensajes, los más recientes |
| Historial_TurnoDemasiadoLargo_CortaPorTamano | Límite | Corta al pasar de 4000 bytes |
| Historial_LimiteMenorADos_SeAjustaAUnTurno | Negocio | Siempre al menos un turno |
| Pregunta_SeClasificaComoPersonalONo (5 preguntas) | Negocio | Horario / kárdex / RAG según la pregunta |
| FechaHoraCampus_ConvierteUtcAHermosillo | Normal | "miércoles 2026-11-25 10:00 (hora de Hermosillo)" |

## Defecto encontrado

`GuardarKardexMateriaRequest.Calificacion` usa `[Range(0, 100)]` con límites enteros, así que
.NET convierte el decimal a entero antes de comparar: **100.4 se redondea a 100 y se acepta**.
La prueba `Captura_CalificacionConDecimalesArribaDe100_DeberiaRechazarse` falla a propósito para
mostrarlo (categoría `DefectoConocido`). Arreglo sugerido en `MiUni.Api/DTOs/KardexDto.cs`:

```csharp
[Range(typeof(decimal), "0", "100", ErrorMessage = "La calificación debe estar entre 0 y 100.")]
```

Con ese cambio la prueba pasa a PASSED.
