# Parque del Café — Propuesta de Proyecto
## Sistema de Evaluación y Gestión de Accesibilidad + Chatbot Inteligente (WhatsApp & IA)

**Presentado por:** Andres Garcia — Ingeniero Electrónico

---

## Índice

1. Introducción
2. Antecedentes
3. Definición del problema
4. Solución del problema
5. Alcance
6. Tecnologías
7. Costos y cronograma

---

## 1. Introducción

El presente proyecto propone el desarrollo de un sistema conversacional inteligente que permita al personal del parque acceder de forma rápida, confiable y estructurada a información relacionada con accesibilidad y condiciones médicas de los visitantes, mejorando la toma de decisiones en tiempo real.

Además, se propone el desarrollo de una aplicación multiplataforma que permita evaluar, clasificar y guiar a los visitantes del parque según sus condiciones físicas o médicas, garantizando una experiencia segura, organizada e inclusiva en el recorrido y las atracciones.

---

## 2. Antecedentes

Actualmente, los procesos de atención a visitantes con condiciones especiales dependen en gran medida del criterio individual del personal. Esto puede generar:

- Inconsistencias en la toma de decisiones
- Retrasos en la atención
- Riesgos operativos

No existe una herramienta centralizada que permita consultar información validada de manera inmediata.

La evaluación de accesibilidad en parques suele ser manual, lo que genera:

- Decisiones no estandarizadas
- Falta de trazabilidad
- Riesgos en atracciones
- Experiencias inconsistentes para visitantes

---

## 3. Definición del problema

El personal operativo enfrenta dudas frecuentes frente a casos médicos o condiciones especiales, sin contar con un sistema de apoyo que:

- Brinde respuestas inmediatas
- Justifique decisiones
- Reduzca el margen de error

No existe un sistema digital que permita:

- Evaluar de forma estructurada a los visitantes
- Clasificar riesgos automáticamente
- Generar recomendaciones seguras
- Registrar información para control y seguimiento

---

## 4. Solución propuesta

### Chatbot (WhatsApp + IA)

Desarrollo de un chatbot integrado a WhatsApp, utilizando n8n y potenciado por IA (Gemini Flash 2.5 o Groq), capaz de:

- Permitir un control de acceso para el personal autorizado con validación de ID
- Permitir al personal de primeros auxilios, operaciones, atención al visitante y en general al personal interno resolver dudas importantes sobre las restricciones físicas y de salud de las atracciones
- Obtener una evaluación muy precisa y rápida frente a casos que lo ameriten
- Generar un respaldo inteligente de las posibles atracciones a usar para dicho visitante, con recomendaciones generosas de cómo actuar frente a un caso determinado
- Usarlo como fuente de conocimiento para responder las dudas del personal en general del parque frente a las restricciones físicas y de salud de las atracciones

### Aplicativo

Por parte del aplicativo:

- Permite al personal de primeros auxilios realizar un test guiado al visitante
- Evalúa condiciones físicas, médicas o de accesibilidad
- Genera automáticamente una preselección de atracciones de uso seguro
- Permite validación manual del personal
- Genera:
  - Guía en PDF personalizada
  - Manilla identificadora con accesos permitidos de las atracciones
  - Posibilidad de consulta de la asesoría dada al visitante
  - Registro histórico de asesorías

---

## 5. Alcance

**Por parte del asistente chatbot se incluye:**

- Diseño del flujo conversacional
- Integración con WhatsApp
- Configuración de n8n
- Implementación de IA
- Carga de base de conocimiento
- Pruebas y validación

**Por parte de la aplicación incluye:**

- Desarrollo app Android y PC
- Módulo de test
- Motor de reglas
- Base de datos
- Generación de PDF
- Pruebas

---

## 6. Tecnologías

- Evolution API
- Base de datos: PostgreSQL
- Automatización en n8n
- Contenedor aplicado para la integración del sistema: Docker
- Frontend: .NET MAUI (multiplataforma)
- Backend: API REST: .NET
- Base de datos: Firebase Realtime DB
- Generación de PDFs: Librería de Syncfusion

### Funcionalidades a futuro

- Aplicar scraping o generar una API sobre el sitio web actual y pasarlo como otra base de conocimiento en tiempo real con el fin de mejorar la precisión de la información en general
- IA más robusta para enfrentar mejor los casos
- Usar el flujo junto a alguna tecnología que permita manejar los canales y asignar casos muy específicos a personal médico profesional en tiempo real
- Sincronizar bot con aplicativos internos o externos para mejorar los sistemas en conjunto
- Mejorar la experiencia del usuario mediante rutas sugeridas y notificaciones en tiempo real
- Análisis y extracción de información para identificar las condiciones más comunes, atracciones restringidas y el flujo de los visitantes
- Posible sistema certificable de seguridad frente a casos especiales

---

## 7. Costos y recursos

Todos los valores son en COP, a excepción de la licencia y el VPS de despliegue.

### Parte 1 — App .NET MAUI

| # | Componente | Detalle técnico / Proveedor | Costo inicial (COP) | Presupuesto | Notas |
|---|---|---|---|---|---|
| 1 | Desarrollo App .NET MAUI | Login, test guiado, historial, PDF | 6.400.000 | 0 | Pago único al desarrollador |
| 2 | Licencia Syncfusion PDF | Syncfusion Comercial — 1 Dev | 0 | ~303.486/mes | ~$1.000 USD/año ≈ $3.641.837 COP/año |
| 3 | Firebase (Auth + Realtime DB) | Plan Spark gratuito | 0 | 0 | Gratuito hasta ciertos límites de uso |
| 4 | Servidor backend .NET | Puede compartir VPS del bot | 0 | Incluido en bot | Si se contrata VPS del bot, no hay costo extra |
| 5 | Repositorio de código | GitHub o recomendado | 0 | 0 | GitHub Free disponible |
| 6 | Dispositivo Android pruebas | Suministrado por el parque | 0 | 0 | Hardware del parque |
| | **TOTAL APP** | | **$6.400.000** | **$3.641.837 COP/año** | |

### Parte 2 — Chatbot WhatsApp + IA

| # | Componente | Detalle técnico / Proveedor | Costo inicial (COP) | Presupuesto mensual | Notas |
|---|---|---|---|---|---|
| 1 | Desarrollo Bot WhatsApp IA | Flujo n8n + prompt + BD + buffer | 2.700.000 | 0 | Pago único al desarrollador |
| 2 | Infraestructura VPS | VPS Hostinger (2 vCPU, 4GB RAM) | 0 | 85.000 | ~$12 USD/mes · Docker + n8n + PostgreSQL |
| 3 | Consumo IA — OpenAI Whisper | Transcripción de audios WhatsApp | 0 | 20.000 | Variable según uso de audio. Puede ser $0 si no hay audios |
| 4 | Gemini Flash 2.5 | API Google IA (consultas de texto) | 0 | 0 | Plan gratuito generoso para empezar |
| 5 | Evolution API — WhatsApp | Open source, sin costo de licencia | 0 | 0 | Desplegado en Docker propio |
| 6 | PostgreSQL | Open source, sin licencia | 0 | 0 | Contenerizado en Docker del VPS |
| | **TOTAL BOT** | | **$2.700.000** | **$105.000** | |

### Resumen financiero total

| # | Concepto | Valor | Tipo | Notas |
|---|---|---|---|---|
| 1 | Inversión inicial App .NET MAUI | $6.400.000 COP | | |
| 2 | Inversión inicial Bot WhatsApp | $2.700.000 COP | | |
| 3 | **INVERSIÓN TOTAL** | **$9.100.000 COP** | Pago único desarrollo | Ambos proyectos |
| 4 | Costo operativo mensual | ~$408.486 COP/mes | Solo infraestructura | App tiene $0/mes en Firebase |
| 5 | Licencia anual Syncfusion | ~$3.641.837 COP/año | Renovación anual | Obligatorio para PDF multiplataforma |
| 6 | Capacitaciones opcionales | $22.000 COP/hora | Precio/hora | A solicitud del parque |

---

## Cronograma

| Fase | Actividad | Duración |
|---|---|---|
| 0 | Levantamiento y documentación base | 1-4 semanas |
| 1 | Diseño conversacional Bot WhatsApp | 1 semana |
| 2 | Infraestructura y configuración Bot | 2 semanas |
| 3 | Integración IA y pruebas Bot | 2 semanas |
| 4 | Diseño y arquitectura App .NET MAUI | 3 semanas |
| 5 | Desarrollo frontend App | 4 semanas |
| 6 | Desarrollo backend y API REST App | 4 semanas |
| 7 | Pruebas integrales y ajustes App | 2 semanas |
| 8 | Capacitación, documentación y entrega | 1 semana |

---

## Beneficios

### Beneficios operativos

- Respuestas inmediatas al personal
- Reducción de dependencia de supervisores
- Disminución de tiempos de atención
- Apoyo constante en decisiones complejas
- Digitalización completa del proceso
- Evaluaciones rápidas y estructuradas
- Generación automática de resultados
- Flujo claro para el personal

### Beneficios en seguridad

- Menor margen de error humano
- Respuestas basadas en información estructurada
- Mejor manejo de casos médicos o especiales
- Clasificación objetiva de riesgos
- Reducción de decisiones improvisadas
- Mayor control en acceso a atracciones

### Beneficios estratégicos

- Centralización del conocimiento
- Estandarización de criterios
- Registro de dudas frecuentes (insumo para mejora continua)
- Atención más organizada
- Sensación de cuidado y profesionalismo
- Guía clara para recorrer el parque
- Inclusión real de personas con condiciones especiales
- Base de datos de visitantes
- Trazabilidad de decisiones
- Información para mejora del parque

### Beneficios económicos

- Reduce reprocesos
- Disminuye riesgos legales
- Bajo costo de implementación vs. alto impacto
- Reducción de incidentes
- Optimización del personal
- Mayor satisfacción → más retorno de clientes

### Plus

- Detecta cualquier idioma, y se contextualiza con el caso sea por audio o por texto.

---

## Contacto

- **Teléfono:** 3137874463
- **Correo:** andresfelipebanol11@gmail.com
- **Ubicación:** La Tebaida, Quindío
