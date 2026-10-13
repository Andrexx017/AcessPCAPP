# Guía de uso de AppParque — Personal operativo (Enfermería)

**Qué es este documento:** una guía completa de cómo usar AppParque, la aplicación con la que el personal de enfermería del Parque del Café registra visitantes, evalúa su accesibilidad y genera la guía de atracciones seguras para cada uno. Cubre el flujo operativo completo: iniciar sesión, registrar o buscar un visitante, aplicar el test guiado, revisar el dictamen, generar el PDF para el visitante y consultar el historial. No cubre las pantallas de administración (gestión de usuarios, preguntas o catálogo de atracciones), que son exclusivas del rol Administrador.

**Para quién es:** personal de enfermería / operativo que atiende visitantes directamente. Si tienes rol de Administrador, todo esto también aplica a ti, además de las pantallas de administración que no se describen aquí.

---

## 1. Iniciar sesión

Al abrir AppParque ves la pantalla "Iniciar sesión", con el panel de marca del Parque del Café a un lado (en pantallas grandes) y el formulario al otro (en celular, el panel de marca queda arriba y el formulario debajo).

- **Usuario**: tu nombre de usuario interno (formato `nombre.apellido`).
- **Contraseña**: puedes mostrar/ocultar el texto con el ícono de ojo junto al campo.
- **Recordar contraseña**: si lo marcas, la próxima vez que abras la app el usuario y la contraseña ya aparecen escritos (se guardan cifrados en el almacenamiento seguro del dispositivo, nunca en texto plano). Si lo desmarcas y vuelves a iniciar sesión, se borran las credenciales guardadas.
- Botón **Iniciar sesión**: valida que ambos campos tengan algo escrito; si falta uno, muestra "Por favor ingrese usuario y contraseña." Si el usuario o la contraseña son incorrectos, muestra el error que devuelve el servidor.
- Hay un interruptor de tema (sol/luna) en la esquina para cambiar entre modo claro y modo oscuro — funciona en toda la app, no solo aquí.

Al iniciar sesión correctamente, el sistema te lleva automáticamente al **Menú Principal** (personal operativo) o al **panel de administración** si tu usuario tiene rol Admin.

---

## 2. Menú principal

Es la pantalla de inicio para el personal operativo. Muestra un saludo con tu nombre y dos accesos principales:

- **Realizar test** → inicia el flujo de atención de un visitante: primero busca o registra al visitante y luego aplica el test guiado (ver secciones 3 y 4).
- **Historial** → abre el historial de evaluaciones ya realizadas (ver sección 7).

En la parte superior hay un botón de menú (ícono de hamburguesa) que abre un **panel lateral** con:

- Tu foto de perfil (o tus iniciales si no has subido una) y tu nombre.
- **Perfil** → va a "Mi Perfil" (sección 8).
- **Cerrar sesión**.

También verás el interruptor de tema claro/oscuro, y en la esquina inferior un **botón flotante del Asistente de IA** (ver sección 9), disponible en casi toda la app para resolver dudas sin salir de donde estás.

---

## 3. Registrar o buscar un visitante

Al tocar "Realizar test" entras al flujo de identificación del visitante.

### 3.1 Paso 1 — Identificación

Pantalla para buscar si el visitante ya tiene una ficha registrada de una visita anterior.

- **Tipo de documento**: Cédula de Ciudadanía, Cédula de Extranjería, Pasaporte o Tarjeta de Identidad.
- **Número de documento**.
- Botón **Buscar**: si falta algún campo, avisa "Selecciona el tipo e ingresa el número de documento." Si el visitante existe, te lleva al paso de **confirmación** (3.2). Si no existe, te lleva directo al **registro completo** (3.3), con esos mismos datos de documento ya precargados.
- Botón **"Es mi primera vez, omitir búsqueda"**: salta la búsqueda y va directo al registro completo, útil si ya sabes que el visitante es nuevo.

> Si el visitante ya había visitado antes, el sistema recupera su ficha y su última evaluación — no hace falta repetir el formulario ni el test desde cero.

### 3.2 Visitante ya registrado — "Bienvenido de nuevo"

Se muestra solo cuando el documento buscado ya existe. Aparece el nombre y documento del visitante, y:

- Si **no tiene evaluaciones previas**: solo puedes continuar al test (botón "Continuar al test").
- Si **sí tiene una evaluación previa**: se muestra cuándo fue (por ejemplo "hace 2 semanas"), qué condiciones de salud quedaron registradas, y cuántas atracciones le quedaron permitidas de un total. Se te pregunta: **"¿Tus condiciones de salud siguen siendo las mismas desde esa visita?"**
  - **"Sí, siguen igual"** → reutiliza la evaluación anterior sin repetir el test y te lleva directo a la pantalla de **Validación de atracciones** (sección 6), donde revisas/ajustas la preselección y generas el PDF.
  - **"No, algo cambió" / "Repetir test completo"** → inicia el test guiado desde cero (sección 4), reutilizando la edad y estatura que ya tenía registradas.

> **Importante:** si pasaron más de 6 meses desde la última evaluación, la app muestra una advertencia recomendando repetir el test completo en vez de reutilizar la anterior, por seguridad del visitante.

### 3.3 Registro completo — visitante nuevo

Formulario único con todos los datos necesarios (todos los campos son obligatorios por seguridad del visitante):

- **Documento**: tipo y número (precargados si vinieron del paso anterior).
- **Datos personales**: nombre completo, edad, estatura en cm.
- **Acompañante y contacto de emergencia**: nombre del acompañante, parentesco (padre/madre, cónyuge/pareja, hijo/hija, hermano/hermana, otro familiar, amigo/cuidador) y teléfono. Este acompañante **es** el contacto de emergencia — no hay un tercer contacto aparte.
- **En caso de emergencia**: tipo de sangre, EPS, y alergias conocidas (si no tiene, se puede escribir "Ninguna conocida").
- Dos casillas de autorización obligatorias: tratamiento de datos personales (Ley 1581 de 2012) y aceptación de las políticas del parque.

Botón **"Comenzar evaluación"**: valida que todos los campos estén completos y las dos casillas marcadas (si falta algo, te indica exactamente qué falta). Al guardar correctamente, el visitante queda registrado y entras directo al **test guiado de accesibilidad** (sección 4).

---

## 4. Evaluación de accesibilidad (el test guiado)

Es el corazón de la app: un test que recorre el cuerpo del visitante por zonas para entender qué puede y qué no puede hacer, qué dispositivos de apoyo usa (prótesis, silla de ruedas, etc.) y qué condiciones de salud tiene — toda esta información es la que luego se cruza contra los requisitos de cada atracción.

La app se adapta automáticamente al tamaño de pantalla: en **celular** el test se hace en pantallas completas, una cosa a la vez; en **escritorio o tablet** se ve el mapa y las preguntas de la zona abierta al mismo tiempo, lado a lado. Es el mismo test en ambos casos, solo cambia cómo se presenta.

### 4.1 El mapa de capacidades

Pantalla central del test: una silueta del cuerpo dividida en zonas que puedes tocar, más una lista aparte para datos que no son parte del cuerpo (medidas, condiciones generales, acompañante). Cada zona se pinta de un color según su estado:

- **Gris (sin revisar)**: todavía no se tocó.
- **Amarillo (a medias)**: se empezó a responder pero falta alguna pregunta.
- **Azul (completa)**: todas sus preguntas fueron respondidas.
- **Verde (sin novedad)**: se marcó como "sin novedad" (ver 4.2).

En la parte de arriba ves el nombre del visitante y cuántas zonas llevas ("X de 10"). Abajo, un mensaje dinámico te dice si ya puedes ver el dictamen o cuántas zonas faltan por resolver, con un botón que dice **"Empezar"** (te lleva a la primera zona pendiente) o **"Ver dictamen"** (cuando ya respondiste todo).

Las 10 zonas son: **Cabeza** (visión, audición, condición cognitiva, comprensión de instrucciones), **Brazo y mano izquierda**, **Brazo y mano derecha**, **Tronco y postura**, **Pierna izquierda**, **Pierna derecha**, **Marcha y embarque**, **Medidas** (estatura, peso, edad — no se puede marcar "sin novedad" aquí, hay que llenarla), **Condiciones generales** (embarazo, cirugías recientes, convulsiones, mareo/vértigo, claustrofobia, etc.) y **Acompañante y rol**.

### 4.2 Entrar a una zona

Al tocar una zona en el mapa, primero se pregunta **"¿Hay algo que revisar aquí?"** con dos opciones:

- **"Sin novedad"**: marca de una vez todas las preguntas de esa zona con sus valores normales/por defecto, y vuelve al mapa. Es el camino rápido cuando el visitante no tiene ninguna condición en esa zona.
- **"Evaluar"**: entra a responder las preguntas de esa zona una por una (o todas juntas, en modo escritorio).

> **Muy importante:** "Sin novedad" **no es lo mismo** que dejar una pregunta sin responder. "Sin novedad" registra explícitamente que todo está normal. Una pregunta que se deja en blanco (con el botón "No sabe o no responde") queda marcada como **"por verificar"** — el sistema nunca asume que algo sin dato está bien ni mal, simplemente pide verificarlo en el sitio antes de confirmar esa atracción.

### 4.3 Responder las preguntas de una zona

Cada pregunta se presenta con su texto, una explicación si hace falta, y las opciones para responder (botones de selección única, o un campo numérico con su unidad — cm, kg, años). Algunas preguntas solo aparecen si la respuesta anterior lo amerita (por ejemplo, "¿la prótesis es removible?" solo aparece si ya dijiste que el visitante usa prótesis).

En modo celular puedes navegar con **"Atrás"** y **"Siguiente"**, o tocar **"No sabe o no responde"** para dejar esa pregunta pendiente sin perder el resto del avance. En modo escritorio ves todas las preguntas de la zona abierta a la vez, con un botón para marcar toda la zona como "sin novedad" o pasar directo a la siguiente zona.

Al terminar todas las zonas (o si intentas pasar al dictamen con preguntas pendientes, la app te avisa exactamente cuáles faltan y te lleva ahí).

### 4.4 Resumen antes del dictamen

Pantalla de repaso final antes de pedir el resultado. Muestra, zona por zona, cada pregunta respondida y su respuesta; lo que se haya marcado distinto a "lo normal" queda resaltado como para que lo revises con más atención (esto es solo una señal visual, no significa por sí solo que una atracción quede restringida).

- Puedes escribir algo adicional en el cuadro **"¿Algo más que no te preguntamos?"** — esta nota no entra al motor de reglas, pero sí queda guardada en el historial y se puede enviar al asistente de IA para una segunda opinión.
- Botón **"Preguntar a la IA"**: envía todas las respuestas (y tu nota, si escribiste una) al asistente, que responde en la misma pantalla con su propio análisis de qué atracciones podrían quedar restringidas o condicionadas y por qué. Es solo una opinión de apoyo — no reemplaza el resultado oficial del motor de reglas.
- Botón **"Editar"**: vuelve al mapa/zonas para corregir alguna respuesta.
- Botón **"Continuar al dictamen"**: calcula el resultado oficial y pasa a la siguiente pantalla (sección 5). En este punto el resumen completo (incluida cualquier nota que hayas agregado) queda guardado en el historial.

---

## 5. El dictamen (resultado del test)

Aquí se muestra, atracción por atracción, el resultado de cruzar las respuestas del visitante contra los requisitos de cada atracción del parque (29 en total).

### 5.1 Pantalla de resultados

Arriba ves un resumen con cuatro contadores: **Permitidas**, **Con condición**, **Por verificar** y **Restringidas**. Si al sistema le faltó algún dato para decidir con certeza, aparece un aviso indicando exactamente qué dato falta.

Puedes filtrar la lista con los chips **Todas / Permitidas / Con condición / Por verificar / No** (restringidas). Cada atracción aparece con su nombre, su nivel de intensidad, una etiqueta de color según su resultado, y el detalle de:

- **Motivos**: por qué quedó en ese estado.
- **Mitigaciones**: qué debe cumplirse para poder usarla (por ejemplo "debe ir con acompañante adulto", "debe ubicarse en la silla interna").
- **Advertencias**: información adicional relevante.

Los cuatro resultados posibles son:

| Resultado | Qué significa |
|---|---|
| **Permitida** | El visitante cumple todos los requisitos. |
| **Permitida con condición** | Puede usarla, pero aplicando una mitigación concreta (acompañante, ubicación en el vehículo, etc.). |
| **Por verificar** | Falta un dato para decidir con certeza — hay que confirmarlo en el sitio antes de autorizarla. |
| **Restringida** | No cumple un requisito de seguridad de esa atracción. |

Desde aquí tienes tres acciones independientes:

- **Editar**: vuelve a la pantalla anterior para ajustar respuestas.
- **Generar guía**: pasa a construir el PDF para el visitante (sección 5.2).
- **Confirmar**: guarda este dictamen en el historial (sección 5.3) — puedes confirmar antes o después de generar el PDF, como prefieras.

### 5.2 Generar la guía en PDF

Dos pantallas rápidas:

1. **Preselección**: las atracciones permitidas o con condición empiezan marcadas como "incluidas" en la guía, y las restringidas o por verificar empiezan "excluidas" — pero puedes cambiar cualquiera con su interruptor antes de generar el PDF. Arriba se ve cuántas quedaron incluidas del total.
2. **Vista previa**: muestra exactamente cómo va a quedar el PDF — nombre del visitante, fecha, y cada atracción incluida con su descripción, alturas mínima/máxima y cualquier nota de mitigación. Desde aquí:
   - **Descargar**: genera el PDF y lo guarda; puedes abrirlo directamente desde el aviso de confirmación.
   - **Compartir**: abre el menú nativo para enviar el PDF por el medio que prefieras (correo, WhatsApp, etc.).

El PDF generado incluye el logo del parque, un saludo personalizado con el nombre del visitante y la fecha, la lista de atracciones recomendadas con sus datos y notas, y un mapa del parque.

### 5.3 Confirmar y guardar en el historial

El botón **Confirmar** de la pantalla de resultados (5.1) guarda de forma permanente: el perfil completo del visitante, cualquier nota adicional, el resultado de cada atracción y los cuatro contadores, además de la respuesta de la IA si la pediste. Al confirmar, la app te devuelve directo al Menú Principal, lista para atender al siguiente visitante. Este registro queda disponible después en el **Historial** (sección 7).

---

## 6. Reutilizar una evaluación anterior (ruta corta)

Cuando en el paso de identificación (3.2) eliges **"Sí, siguen igual"**, no repites el test — vas directo a una pantalla de **Validación de atracciones** con la lista de atracciones de la evaluación anterior, cada una con una casilla (marcada si ya estaba preseleccionada). Puedes marcar o desmarcar cualquiera manualmente — el cambio se guarda solo, sin necesidad de un botón "guardar" aparte.

Botón **"Generar PDF"**: con al menos una atracción marcada, genera el PDF de la guía para el visitante (mismo formato que el de la sección 5.2) y te permite abrirlo al terminar.

---

## 7. Historial de evaluaciones

Muestra la lista de evaluaciones ya realizadas. Como personal operativo, ves solo las que tú hiciste (un Administrador ve las de todo el equipo, con un filtro adicional por enfermero).

Cada registro muestra: nombre y documento del visitante, quién hizo la evaluación, la fecha, y los cuatro contadores de resultado (Permitidas / Con condición / Por verificar / Restringidas), además de si tiene una nota adicional o una respuesta de la IA guardada.

Filtros disponibles: buscar por nombre o documento del visitante, rango de fechas (por defecto, los últimos 30 días), y casillas para mostrar u ocultar registros "Con condición", "Por verificar", "Restringida" y "Con nota del personal o de la IA" (todas activas por defecto). El botón **"Limpiar filtros"** los restablece a sus valores por defecto.

Puedes **exportar** la lista filtrada a **CSV** o a **Excel** con el nombre, documento, enfermero, fecha y veredicto de cada registro — útil para reportes o auditorías.

---

## 8. Mi perfil

Desde el menú lateral → Perfil, puedes:

- Cambiar tu foto (tócala o usa el botón "Cambiar foto").
- Editar tu nombre completo y tu correo electrónico.
- Ver tu usuario y tu rol (de solo lectura, no se pueden cambiar desde aquí).
- Guardar los cambios con el botón **"Guardar cambios"** (el nombre completo es obligatorio).

---

## 9. El asistente de IA

Disponible desde el botón flotante (ícono de burbuja) en casi toda la app. En celular se abre a pantalla completa; en escritorio/tablet se abre como un panel pequeño al lado, sin tapar el resto de la pantalla.

Antes de poder escribir, siempre hay que elegir uno de estos temas:

- **🩺 Diagnóstico de accesibilidad**: evalúa el caso de un visitante contra las atracciones, conversando — útil como segunda opinión o para resolver dudas puntuales sin pasar por el test completo.
- **📏 Alturas y restricciones por atracción**: preguntas sobre estatura mínima/máxima, peso, requisitos de acompañante o condiciones físicas/de salud de una atracción específica.
- **🕒 Horarios del parque**: si el parque está abierto ahora, y el horario de cualquier servicio.
- **💡 Ayuda con la aplicación**: cómo usar AppParque — explica cualquier pantalla o flujo, y si aplica, te deja un acceso directo para ir directo allá (por ejemplo a Historial, a Registrar visitante o a Mi Perfil).
- **💬 Otras consultas**: cualquier otra pregunta relacionada con el parque que no entre en los temas anteriores.

Dentro de una conversación puedes adjuntar una imagen (ícono de clip), copiar o compartir cualquier respuesta del asistente, y cuando termines, el asistente te pregunta si tu duda quedó resuelta ("✅ Listo, gracias" cierra el tema; "💬 Tengo otra pregunta" sigue la misma conversación). También puedes tocar **"↺ Cambiar de tema"** en cualquier momento para volver al menú de temas. Si pasan 5 minutos sin actividad, la conversación se cierra sola por inactividad.

---

## 10. Notas importantes para recordar

- **"Sin novedad" ≠ no responder.** "Sin novedad" dice explícitamente que todo está normal en esa zona. No responder deja la zona "por verificar" — el sistema nunca asume nada sobre un dato que falta.
- **Vigencia de 6 meses.** Si la última evaluación de un visitante tiene más de 180 días, la app recomienda repetir el test completo en vez de reutilizar la anterior.
- **El acompañante es el contacto de emergencia.** No existe un tercer contacto separado — el nombre, parentesco y teléfono del acompañante son los datos de contacto en caso de emergencia.
- **La consulta a la IA durante el test o en el dictamen es solo una opinión de apoyo.** El resultado oficial siempre lo calcula el motor de reglas a partir de las respuestas del test, no el asistente.
- Un visitante puede tener varias evaluaciones a lo largo del tiempo (una por cada visita en la que cambiaron sus condiciones); todas quedan guardadas en el historial, nunca se sobrescriben.
