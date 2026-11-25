# Pruebas unitarias de MiUni con MSTest

Proyecto de pruebas unitarias (MSTest, C#) para la API de MiUni. No necesita PostgreSQL,
el servicio RAG ni internet: las pruebas que usan datos corren sobre una base en memoria.

## Cómo ejecutarlas (Windows, PowerShell o CMD, desde la carpeta raíz del repo)

```powershell
# 0. (Opcional) Salida en inglés con Passed / Failed por prueba
$env:DOTNET_CLI_UI_LANGUAGE="en"; $env:VSLANG="1033"

# 1. Todas las pruebas que deben pasar (resultado: PASSED)
dotnet test MiUni.Api.UnitTests --tl:off --filter "TestCategory!=DefectoConocido" --logger "console;verbosity=detailed"

# 2. Todas, incluyendo la que exhibe un defecto real (resultado: 1 FAILED)
dotnet test MiUni.Api.UnitTests --tl:off --logger "console;verbosity=detailed"

# 3. Un solo módulo (cambia el nombre de la clase)
dotnet test MiUni.Api.UnitTests --tl:off --filter "FullyQualifiedName~AutenticacionTests" --logger "console;verbosity=detailed"
```

Las clases de cada módulo son `AutenticacionTests`, `KardexTests`, `MapaTests`,
`HorarioProfesoresTests` y `ChatMuniTests`. En Visual Studio también aparecen en el
**Explorador de pruebas** (Prueba → Explorador de pruebas), agrupables por *Rasgo* (categoría).

Cada prueba tiene una categoría (`TestCategory`): **Caso normal**, **Caso de error**,
**Caso límite** o **Regla de negocio**, y una descripción con el resultado esperado.
Todas siguen el patrón Arrange / Act / Assert.

## Módulos y pruebas

5 módulos con 5 pruebas cada uno (25 en total): 24 pasan y 1 falla a propósito (ver abajo).

### 1. Autenticación y gestión de usuarios (`AutenticacionTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Registro_DatosCompletos_PasaLaValidacion | Normal | Sin errores de validación |
| Registro_CorreoMalFormado_DaError | Error | "El correo no tiene un formato válido." |
| Registro_ContrasenaEnElMinimo_SieteFallaYOchoPasa | Límite | 7 caracteres se rechaza, 8 se acepta |
| Registro_EstudianteConCorreoPersonal_EsRechazado | Negocio | 400: exige correo @unison.mx |
| Registro_ComoAdministrador_EsRechazado | Negocio | 400: solo Estudiante o Propietario |

### 2. Kárdex académico (`KardexTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Resumen_KardexTipico_CalculaCreditosPromedioYConteos | Normal | 18 créditos, promedio 70, 2/1/1 materias |
| Chat_AlumnoSinKardex_LeIndicaSubirlo | Error | "No tienes materias guardadas en tu kárdex" |
| Resumen_KardexVacio_SinPromedioYEnCeros | Límite | Promedio nulo, todo en cero |
| Chat_ConsultaDeCreditos_SoloUsaElKardexPropio | Negocio | No aparecen materias de otro alumno |
| Captura_CalificacionDe100Punto4_DeberiaRechazarse | Límite (**defecto**) | **FALLA**: 100.4 se acepta (ver abajo) |

### 3. Mapa del campus y rutas (`MapaTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Lugar_DentroDeSuHorario_EstaAbierto | Normal | Abierto a las 10:30 (horario 07–15) |
| Ruta_PuntoSinLatitud_DaError | Error | Error en Latitud |
| Lugar_HoraExactaDeAperturaYCierre | Límite | 07:00 abierto, 15:00 cerrado |
| Ruta_RadioDeConexion_SoloEntre1Y1000Metros | Límite | 1 y 1000 m válidos; 0 y 1000.5 m inválidos |
| Lugar_HorarioQueCruzaMedianoche_SigueAbiertoAlDiaSiguiente | Negocio | 22:00–02:00: abierto a la 01:00, cerrado a las 02:00 |

### 4. Horario y profesores (`HorarioProfesoresTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Horario_ClasesDeHoy_ListaSoloLasDelDia | Normal | Solo las clases del miércoles |
| Horario_AlumnoSinHorario_LeIndicaCapturarlo | Error | "No tienes un horario guardado." |
| Horario_ClaseActual_EnElCambioDeHora | Límite | A las 10:00 cuenta la clase de 10–11, no la de 9–10 |
| BuhoRater_EnlaceConDominioFalso_SeRechaza | Error | Enlace rechazado |
| BuhoRater_MismoProfesorEscritoDistinto_TieneLaMismaClave | Negocio | Misma clave sin importar orden ni acentos |

### 5. ChatMuni (`ChatMuniTests.cs`)
| Prueba | Tipo | Resultado esperado |
|---|---|---|
| Historial_TurnosCompletos_SeConservanEnOrden | Normal | user, assistant, user, assistant |
| Historial_PreguntaSinRespuesta_SeDescarta | Error | Solo el turno completo |
| Historial_MuchosTurnos_SeRecortaAlMaximoDeMensajes | Límite | 12 mensajes, los más recientes |
| Pregunta_PersonalOInstitucional_SeDirigeAlModuloCorrecto | Negocio | Horario y kárdex en la API; lo institucional al RAG |
| FechaHoraCampus_ConvierteUtcAHermosillo | Normal | "miércoles 2026-11-25 10:00 (hora de Hermosillo)" |

## Defecto encontrado

`GuardarKardexMateriaRequest.Calificacion` usa `[Range(0, 100)]` con límites enteros, así que
.NET convierte el decimal a entero antes de comparar: **100.4 se redondea a 100 y se acepta**.
La prueba `Captura_CalificacionDe100Punto4_DeberiaRechazarse` falla a propósito para
mostrarlo (categoría `DefectoConocido`). Arreglo sugerido en `MiUni.Api/DTOs/KardexDto.cs`:

```csharp
[Range(typeof(decimal), "0", "100", ErrorMessage = "La calificación debe estar entre 0 y 100.")]
```

Con ese cambio la prueba pasa a PASSED.
