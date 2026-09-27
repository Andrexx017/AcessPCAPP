# Plan de Desarrollo, Infraestructura y Seguridad — App de Evaluación de Accesibilidad

**Complementa:** [Requisitos-App-Evaluacion-Accesibilidad.md](./Requisitos-App-Evaluacion-Accesibilidad.md) (qué hace el sistema) y [Propuesta-Proyecto-Accesibilidad.md](./Propuesta-Proyecto-Accesibilidad.md) (alcance/costos pactados con el cliente). Este documento traduce los requerimientos no funcionales en decisiones concretas de infraestructura, seguridad y secuencia de desarrollo, y reemplaza/resuelve varios puntos de la sección "Pendientes" del documento de requisitos.

---

## 1. Estado actual (2026-09-26)

| Componente | Estado |
|---|---|
| `AppParque` (MAUI) | Reorganizado a Screaming Architecture (Features/Services/Shared). Sigue consumiendo `FireBaseService` — **aún no migrado** a `AppParque.Api`. |
| `AppParque.Api` | Proyecto creado, `AppDbContext` con todas las entidades del modelo de datos, migración `InitialCreate` generada. **Sin controllers, sin auth, sin endpoints todavía** — solo esqueleto (`Program.cs` registra el DbContext y OpenAPI, nada más). |
| Base de datos | PostgreSQL en Docker (`docker-compose.yml`, solo el servicio `postgres`). Migración aplicada localmente. |
| PDF | `PdfGeneratorService.cs` con **PdfSharpCore** (no Syncfusion). |
| Infraestructura de producción | No existe todavía — todo corre local. |

**Decisión confirmada en esta sesión:** se mantiene **PdfSharpCore** como librería de PDF definitiva (no Syncfusion). Esto es una desviación respecto a la tecnología cotizada en la propuesta original al cliente — vale la pena informarlo, porque le ahorra al parque la licencia anual de Syncfusion (~$3.641.837 COP/año) sin costo funcional aparente.

---

## 2. Infraestructura

### 2.1 Entornos

- **Local (desarrollo):** `docker-compose.yml` levanta Postgres; `AppParque.Api` corre con `dotnet run` contra ese contenedor; MAUI apunta a `https://localhost:<puerto>` o a la IP del host en emulador Android.
- **Producción:** un único VPS (ver 2.2), todo contenerizado.
- No se justifica un entorno de "staging" separado dado el tamaño del equipo (un desarrollador) y del cliente (un parque). Las pruebas de integración antes de desplegar se hacen local contra Postgres en Docker.

### 2.2 Hosting de producción

**Decisión: compartir el VPS presupuestado para el bot de WhatsApp** (Hostinger, 2 vCPU / 4GB RAM, ~$12 USD/mes), tal como se cotizó en la propuesta general — costo adicional $0 para esta parte.

Esto es razonable para la carga esperada (personal interno de un solo parque, probablemente <20 usuarios concurrentes), pero trae un riesgo real: si el bot (n8n + Evolution API + IA) consume CPU/RAM de forma intensiva, puede degradar la disponibilidad de la app de accesibilidad, que es la que tiene implicaciones de seguridad de visitantes. Mitigación:

- Definir **límites de recursos por contenedor** en `docker-compose.yml` (`deploy.resources.limits` en modo standalone, o `mem_limit`/`cpus` de Docker Compose v2) para que ni el bot ni la app puedan acaparar todo el VPS.
- Monitoreo básico (ver 2.4) para detectar contención antes de que sea un incidente.
- Si el parque crece o el bot se vuelve intensivo, migrar `AppParque.Api` + Postgres a un VPS propio es un cambio de infraestructura, no de código (ya es stateless, ver sección 4).

### 2.3 Composición de contenedores (a extender)

`docker-compose.yml` hoy solo tiene `postgres`. Para producción se necesita agregar:

```yaml
services:
  postgres: # ya existe
    ...
  api:
    build: ./AppParque.Api
    restart: unless-stopped
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=appparque;Username=appparque_api;Password=${DB_API_PASSWORD}"
      Jwt__Key: "${JWT_SIGNING_KEY}"
    depends_on:
      - postgres
    expose:
      - "8080"
  reverse-proxy:
    image: caddy:2
    restart: unless-stopped
    ports:
      - "443:443"
      - "80:80"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
      - caddy-data:/data
```

- **Caddy** en vez de nginx+certbot manual: renueva TLS automáticamente con Let's Encrypt, un solo archivo de config. Dado que ya hay un VPS con el bot, revisar si el bot ya usa nginx/Caddy y reutilizar el mismo reverse proxy para ambos servicios (subdominios: `api.<dominio>` para la app, lo que ya use el bot para su webhook).
- Las credenciales (`DB_API_PASSWORD`, `JWT_SIGNING_KEY`) van en un `.env` **no versionado** (agregar a `.gitignore` si no está), nunca en `appsettings.json` commiteado. `appsettings.Development.json` puede tener defaults de desarrollo local; producción se inyecta por variables de entorno.

### 2.4 Backups y monitoreo

- **Backup de Postgres:** `pg_dump` diario vía cron en el VPS, retención de ~30 días, copiado fuera del VPS (ej. a un bucket S3-compatible barato o incluso a Google Drive/rclone) — los datos incluyen historial médico de visitantes, perderlos no es aceptable.
- **Monitoreo mínimo viable:** healthcheck HTTP simple en `AppParque.Api` (`/health`) + `docker stats` o Netdata/Uptime Kuma (gratis, liviano) para ver consumo de recursos y alertar si el VPS se satura. No se justifica un stack de observabilidad pesado (Prometheus/Grafana) para este tamaño de sistema todavía.

### 2.5 CI/CD

- GitHub Actions:
  - Build + test de `AppParque.Api` en cada push/PR.
  - Build de `AppParque` (al menos target Android) en cada push/PR para detectar roturas de compilación temprano.
  - Deploy: dado el volumen bajo de releases, un workflow manual (`workflow_dispatch`) que haga SSH al VPS y corra `docker compose pull && docker compose up -d` es suficiente para v1 — no se justifica un pipeline de deploy continuo sofisticado todavía.

---

## 3. Seguridad

### 3.1 Autenticación y autorización

**Decisión: JWT con refresh tokens.**

- **Access token:** corto (15–30 min), firmado (HMAC-SHA256 con `Jwt__Key` de al menos 256 bits, o RS256 si se prevé validar el token desde otro servicio), con claims `sub` (UsuarioId), `role` (`Admin`/`Enfermero`), `exp`.
- **Refresh token:** vida más larga (7–14 días, alineado a que el personal no relogea todos los días), **almacenado hasheado** en una tabla nueva (`RefreshToken`: UsuarioId, TokenHash, ExpiraEn, Revocado, CreadoEn) — nunca en texto plano, igual que las contraseñas.
- **Rotación:** cada uso de un refresh token emite uno nuevo e invalida el anterior (rotación con detección de reuso: si un refresh token ya usado se vuelve a presentar, se revocan todos los tokens de ese usuario — señal de robo de token).
- **Logout real:** endpoint que revoca el refresh token del usuario (marca `Revocado = true`), algo que un esquema de "JWT simple sin refresh" no puede ofrecer.
- **Autorización por rol:** políticas de ASP.NET Core (`[Authorize(Roles = "Admin")]`) en todos los endpoints de administración de catálogos y usuarios (RF-09 a RF-12); Enfermero solo ve su propio historial (filtrar por `UsuarioId` del token, no confiar en un parámetro de query).
- **MAUI:** tokens guardados con `SecureStorage` (Keystore/Keychain nativo), nunca en `Preferences` plano.

### 3.2 Contraseñas y datos en tránsito/reposo

- BCrypt para `Usuario.PasswordHash` (ya decidido en el doc de requisitos) — confirmar factor de costo ≥ 12.
- HTTPS obligatorio extremo a extremo (MAUI ↔ API); `UseHttpsRedirection` + HSTS en producción.
- Usuario de base de datos de la API con **privilegios mínimos** (solo DML sobre el schema de la app, no superusuario de Postgres) — hoy `docker-compose.yml` usa el usuario `postgres` (superusuario) tanto para la app como para administración; crear un rol dedicado `appparque_api` antes de producción.
- Rate limiting en el endpoint de login (ASP.NET Core `Microsoft.AspNetCore.RateLimiting`) para mitigar fuerza bruta sobre credenciales del personal.

### 3.3 Datos sensibles (importante: implicación legal, no solo técnica)

Los datos de `Visitante`, `Condicion` y las respuestas de `Evaluacion` son **datos de salud**, que en Colombia la Ley 1581 de 2012 y el Decreto 1377 de 2013 clasifican como **"datos sensibles"** (Habeas Data) — requieren autorización explícita del titular (o su acudiente si es menor de edad) para su tratamiento, y tienen reglas más estrictas que un dato personal común. Esto no está resuelto en el requisito actual (RF-02 solo dice "registrar visitante") y conviene cerrarlo antes de ir a producción:

- Agregar a `RegistroVisitante` un campo de **autorización de tratamiento de datos** (checkbox + texto de consentimiento, con fecha), y persistirlo — no es opcional, es requisito legal para procesar datos de salud en Colombia.
- Definir **política de retención**: ¿cuánto tiempo se conserva el historial de un visitante que no vuelve al parque? (la Ley 1581 exige que los datos no se conserven indefinidamente sin finalidad).
- Restringir logs: nunca loguear respuestas del test, condiciones médicas ni datos de contacto del visitante en texto plano (Serilog con enmascaramiento o simplemente no incluir esos campos en los logs).
- Esto es una recomendación de producto, no solo de infraestructura — vale la pena que el parque (como responsable del tratamiento) lo confirme con su propio criterio legal; no es algo que se resuelva solo con código.

### 3.4 Validación e integridad

- FluentValidation o DataAnnotations en todos los endpoints de escritura, especialmente en catálogos de administración (Preguntas, Atracciones, restricciones) — un error ahí afecta la seguridad real de un visitante.
- Los endpoints de `Evaluacion` deben ser **create-only** (no update/delete) para cumplir el requisito de trazabilidad ya definido (RF-08, NFR de "no se sobrescriben registros históricos") — si hay que corregir algo, se crea una nueva evaluación referenciando la anterior, no se edita.

---

## 4. Escalabilidad

El dimensionamiento correcto aquí es **no sobre-diseñar**: es una app interna de un solo parque, con concurrencia baja (personal de primeros auxilios, no visitantes). Las decisiones de escalabilidad son sobre todo "no cerrar puertas", no "construir para escala que no vamos a tener":

- **API stateless:** JWT sin sesión de servidor → si algún día hace falta, se puede correr más de una instancia de `AppParque.Api` detrás del reverse proxy sin cambios de código. No es necesario ahora.
- **Catálogos cacheados:** `Grupo`, `Condicion`, `Pregunta`/`OpcionRespuesta`, `Atraccion` cambian con poca frecuencia (administración manual) → cachear en memoria (`IMemoryCache`) en la API con invalidación al editar desde los endpoints de Admin, para no pegarle a Postgres en cada test guiado.
- **Índices de base de datos:** `Visitante.NumeroDocumento` (búsqueda, RF-02), `Evaluacion.UsuarioId` + `Evaluacion.FechaCreacion` (filtros de historial, RF-08).
- **Generación de PDF:** síncrona en el request está bien para v1 dado el volumen (un PDF por evaluación, uso esporádico). Si en el futuro se vuelve un cuello de botella (picos de visitantes simultáneos), mover a un job en background (Hangfire) — no construirlo así desde ya.
- **Multi-parque a futuro:** el modelo de datos actual (PKs `int`, sin `ParqueId`) asume un solo cliente. Si el negocio evoluciona a vender esto a otros parques, se necesitaría introducir un tenant ID en las tablas principales — está fuera de alcance ahora, pero es la razón por la que vale la pena no hardcodear "Parque del Café" en ningún lado del backend.

---

## 5. Plan de desarrollo (secuencia hacia producción)

Retoma y ordena la sección "Pendientes" de `Requisitos-App-Evaluacion-Accesibilidad.md`:

| Fase | Entregable | Depende de |
|---|---|---|
| A | Validar con Andres las reglas del motor (grupos/condiciones, sección 5 del doc de requisitos) y cargar datos semilla reales (preguntas, atracciones del parque, restricciones) | — |
| B | Endpoints de Auth: login, emisión JWT + refresh token, logout/revocación, hash BCrypt | — |
| C | Rol dedicado de Postgres para la API (privilegios mínimos) + secrets fuera del repo | — |
| D | Endpoints de `Visitante` (buscar por documento / crear) | B |
| E | Motor de reglas server-side (portar `TestViewModel.MapAnswersToRestrictions` a `AppParque.Api`) + endpoints de `Evaluacion` (aplicar test, preselección automática) | A, D |
| F | Endpoints de administración de catálogos (Preguntas, Atracciones, Grupos, Condiciones) con `[Authorize(Roles="Admin")]` | B |
| G | Endpoints de administración de `Usuario` | B |
| H | Endpoints de `Historial` con filtros y paginación | E |
| I | Migrar MAUI: reemplazar `FireBaseService` por `HttpClient` contra `AppParque.Api`, tokens en `SecureStorage` | B–H según pantalla |
| J | Consentimiento de tratamiento de datos en `RegistroVisitante` (sección 3.3) | D |
| K | Infra de producción: extender `docker-compose.yml` (API + reverse proxy), backups, CI/CD manual | C |
| L | Pruebas integrales (incluye revisar OWASP API Top 10 sobre los endpoints reales) y capacitación al personal del parque | I, K |

---

## 6. Pendientes / decisiones abiertas

- Confirmar con el parque la política de retención de datos de visitantes (sección 3.3) — no es una decisión técnica unilateral.
- Definir si el consentimiento de tratamiento de datos se captura en papel (proceso actual) o digitalmente en la app — afecta el diseño de `RegistroVisitante`.
- Definir dominio real para `api.<dominio>` cuando exista (hoy no hay dominio confirmado para el parque en este repo).
- Revisar si el bot de WhatsApp ya reservó un reverse proxy/dominio en el VPS compartido, para reutilizarlo en vez de duplicar configuración de Caddy/nginx.
