# Documento de Requerimientos — App de Evaluación y Gestión de Accesibilidad

**Módulo:** Aplicación .NET MAUI + API REST (parte "app" de la propuesta general — ver [Propuesta-Proyecto-Accesibilidad.md](./Propuesta-Proyecto-Accesibilidad.md)). El chatbot de WhatsApp/n8n es un sistema aparte y **no** está cubierto por este documento.

---

## 1. Propósito y alcance

Digitalizar el proceso mediante el cual el personal de primeros auxilios del Parque del Café evalúa las condiciones físicas y médicas de un visitante, y a partir de esa evaluación determina qué atracciones puede usar de forma segura — reemplazando el criterio manual actual por un test guiado, un motor de reglas y un registro histórico consultable.

**Incluye:** login con roles, registro/búsqueda de visitantes, test guiado configurable, motor de reglas de restricciones, preselección automática de atracciones, validación manual por el personal, generación de PDF, historial de evaluaciones, administración de catálogos (preguntas, atracciones, grupos, condiciones) y de usuarios internos.

**No incluye:** el bot de WhatsApp (n8n + IA), la manilla/pulsera con QR física, scraping del sitio web — todo eso es parte de la propuesta general pero no de este módulo.

---

## 2. Actores

| Actor | Descripción |
|---|---|
| **Enfermero / Personal operativo** | Usuario interno que atiende visitantes: registra datos, aplica el test, revisa/ajusta la preselección de atracciones, genera el PDF y consulta el historial de sus propias evaluaciones. |
| **Administrador** | Todo lo anterior, más la administración de catálogos (preguntas y opciones del test, atracciones y sus restricciones, grupos y condiciones) y de usuarios internos. Ve el historial completo (no solo el propio). |
| **Visitante** | Sujeto de la evaluación. No inicia sesión ni interactúa directamente con la app — sus datos son capturados por el personal. |

---

## 3. Requerimientos funcionales

| ID | Requerimiento | Actor(es) |
|---|---|---|
| RF-01 | Iniciar sesión con usuario/contraseña; el sistema determina permisos según el rol (Admin/Enfermero). | Enfermero, Admin |
| RF-02 | Buscar un visitante existente por tipo + número de documento, o registrar uno nuevo si no existe. | Enfermero |
| RF-03 | Aplicar un test guiado de accesibilidad: preguntas de única o múltiple selección, en un orden configurable. | Enfermero |
| RF-04 | El sistema mapea las respuestas del test a un conjunto de **grupos** (limitaciones de movilidad) y **condiciones** (médicas/sensoriales) mediante un motor de reglas. | Sistema |
| RF-05 | El sistema preselecciona automáticamente las atracciones seguras para el visitante, cruzando sus grupos/condiciones y estatura contra las restricciones de cada atracción. | Sistema |
| RF-06 | El personal puede revisar y ajustar manualmente (incluir/excluir) la preselección antes de confirmarla. | Enfermero |
| RF-07 | Generar un PDF con la guía de atracciones permitidas para el visitante. | Enfermero |
| RF-08 | Consultar el historial de evaluaciones propias (Enfermero) o de todo el personal (Admin), con filtro por nombre/documento del visitante. | Enfermero, Admin |
| RF-09 | Administrar el catálogo de preguntas y opciones del test (crear, editar, activar/desactivar, reordenar) sin publicar una nueva versión de la app. | Admin |
| RF-10 | Administrar el catálogo de atracciones y sus restricciones por grupo/condición y rango de estatura. | Admin |
| RF-11 | Administrar el catálogo de grupos y condiciones (los "códigos" que usa el motor de reglas). | Admin |
| RF-12 | Administrar usuarios internos: crear, editar, desactivar, asignar rol. | Admin |

---

## 4. Requerimientos no funcionales

- **Multiplataforma:** la app cliente es .NET MAUI (Android y Windows/PC), consumiendo un backend propio vía HTTP/JSON — no accede directamente a la base de datos.
- **Persistencia:** PostgreSQL como base de datos relacional única para este módulo (reemplaza Firebase Realtime DB del prototipo original).
- **Backend:** API REST en .NET (proyecto `AppParque.Api`) con Entity Framework Core como ORM.
- **Seguridad:** contraseñas de `Usuario` almacenadas con hash (BCrypt), nunca en texto plano. Acceso a endpoints de administración restringido por rol.
- **Configurabilidad:** preguntas, atracciones y catálogo de reglas viven en base de datos, editables sin recompilar la app.
- **Trazabilidad:** toda evaluación queda asociada a qué enfermero la realizó y cuándo, de forma permanente (no se sobrescriben registros históricos).
- **Portabilidad de despliegue:** la base de datos corre en un contenedor Docker (`docker-compose.yml`), reproducible en cualquier máquina de desarrollo.

---

## 5. Reglas de negocio (motor de reglas)

El motor de reglas traduce las respuestas del test en **grupos** (limitación de movilidad) y **condiciones** (médicas/sensoriales), y luego cruza esos códigos contra las restricciones de cada atracción para decidir la preselección. Estas son las reglas heredadas del prototipo (`TestViewModel.MapAnswersToRestrictions`), documentadas aquí para su validación y posterior carga como datos semilla — **quedan pendientes de confirmar con el conocimiento del negocio** antes de darlas por definitivas:

**Grupos (movilidad):**
| Código | Se activa cuando... |
|---|---|
| `grupo_1` | El visitante no puede caminar sin apoyo |
| `grupo_1_1` | Camina sin apoyo + sube escaleras sin ayuda + sin capacidad de agarre |
| `grupo_2` / `grupo_3` | Camina sin apoyo (o sube con ayuda) + agarre en una sola mano |
| `grupo_4` | Sube escaleras con ayuda + agarre en ambas manos |
| `grupo_5` | Camina y sube sin ayuda, agarre en una mano, sin prótesis de pierna |
| `grupo_5_1` | Camina y sube sin ayuda, agarre en ambas manos, con prótesis de pierna (debajo de rodilla o ambas) |
| `grupo_5_2` | Camina y sube sin ayuda, con prótesis por encima de rodilla |
| `grupo_10` | Tiene discapacidad certificada por alteración del sistema nervioso o trastorno mental severo |
| `protesis_mano` | Tiene prótesis en una o ambas manos |
| `protesis_pierna_arriba_rodilla` | Tiene prótesis de pierna por encima de la rodilla |

**Condiciones (médicas/sensoriales):**
`discapacidad_visual`, `discapacidad_auditiva`, `discapacidad_cognitiva`, `embarazo`, `problemas_cardiacos`, `problemas_columna`, `mareo_vertigo`, `miedo_espacios_cerrados`.

**Cruce contra atracciones:** cada atracción define, por grupo y por condición, si la **bloquea** (`Permitido = false`: si el visitante activó ese código, la atracción no se preselecciona) o si la **exige** (`Permitido = true`, semántica de inclusión exclusiva: solo se preselecciona si el visitante cumple alguno de los códigos marcados como incluyentes). Además se filtra por `AlturaMinima`/`AlturaMaxima` contra la estatura del visitante.

---

## 6. Modelo de datos

Ver carpeta [`Diagramas/`](./Diagramas) — diagrama entidad-relación completo en [`Diagramas/02-der.png`](./Diagramas/02-der.png), ya implementado como migración de EF Core (`AppParque.Api/Migrations/InitialCreate`) y aplicado sobre PostgreSQL.

Entidades principales: `Usuario`, `Visitante`, `Pregunta`/`OpcionRespuesta`, `Grupo`/`Condicion` (catálogos del motor de reglas), `Atraccion` con sus restricciones, y `Evaluacion` (el evento de test) con sus tablas de respuestas, grupos/condiciones activados y resultado por atracción.

---

## 7. Fuera de alcance (de este módulo)

- Chatbot WhatsApp + IA (n8n, Evolution API, Gemini/Whisper) — sistema aparte, propuesta general.
- Manilla/pulsera física con QR.
- Scraping o API sobre el sitio web del parque.
- Cualquier funcionalidad listada como "Funcionalidades a futuro" en la propuesta general.

---

## 8. Decisiones técnicas ya tomadas

1. Base de datos relacional (PostgreSQL) en vez de Firebase Realtime DB — los datos son inherentemente relacionales (visitantes, atracciones, reglas, historial).
2. Visitante como entidad reutilizable por documento (no un registro nuevo por cada visita).
3. Preguntas/opciones del test viven en base de datos, no hardcodeadas.
4. Grupos y condiciones normalizados en catálogos de BD, no strings mágicos en código.
5. Usuarios internos (autenticación) viven completos en PostgreSQL — no se usa Firebase Auth.
6. Nuevo proyecto `AppParque.Api` (Web API .NET + EF Core) como backend; la app MAUI consume esta API por HTTP, no accede a la BD directamente.
7. Llaves primarias: `int` autoincremental (no GUID).

---

## 9. Pendientes / próximos pasos

- Validar con el conocimiento del negocio (Andres) las reglas documentadas en la sección 5 antes de cargarlas como datos semilla definitivos.
- Cargar datos semilla: preguntas actuales, catálogo de grupos/condiciones, atracciones reales del parque y sus restricciones.
- Diseñar y construir los endpoints REST de `AppParque.Api` (auth, visitantes, evaluaciones, catálogos).
- Reescribir el motor de reglas del lado del backend (hoy vive en el ViewModel de la app).
- Definir estrategia de autenticación de la API (JWT u otro esquema) — no decidido aún.
- Actualizar la app MAUI para consumir `AppParque.Api` en vez de `FireBaseService`.
