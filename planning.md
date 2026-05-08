# Planning de Desarrollo: Reto Técnico - Northwind Order Management System

## Contexto y Reglas del Proyecto
Aplicación Full-Stack para gestión de pedidos sobre la base de datos **Northwind (SQL Server)**. No requiere autenticación (se omite gestión de usuarios).
Toda la solución (frontend, backend y base de datos) debe levantarse **localmente** con un único comando: `docker-compose up -d` desde la raíz del repositorio. "Desplegar" en este reto = orquestación local con Docker, no despliegue en cloud.

### Stack Tecnológico
- **Backend:** ASP.NET Core (REST API) sobre **.NET 10**, Clean Architecture (4 capas + Tests), Entity Framework Core, C#.
- **Frontend:** Vue 3 + Quasar Framework + **TypeScript** + pnpm + Vite.
- **Base de Datos:** SQL Server 2022 (contenedor) con la base Northwind cargada por script.
- **Integraciones Externas:** Google Maps Platform — *Address Validation API*, *Maps JavaScript API*, *Geocoding API*.
- **Reportes:** **QuestPDF** (PDF) y **ClosedXML** (Excel) — ambos OSS sin restricciones de licencia.
- **Testing:** xUnit + Moq + FluentAssertions + Coverlet (backend, ≥80% cobertura). Vitest + Vue Test Utils (frontend, bonus).
- **Logging:** Serilog (consola estructurada).
- **Orquestación:** Docker + Docker Compose.

### Principios transversales (aplicar en todas las fases)
- **Clean Code:** nombres descriptivos, métodos cortos, separación de responsabilidades, sin código muerto.
- **DTOs siempre cruzan la frontera** Application ↔ Api (nunca exponer entidades EF).
- **Validación en Application** (FluentValidation) + **manejo global de errores** (`IExceptionHandler` → `ProblemDetails` RFC 7807).
- **Inyección de dependencias** por interfaces; nada de `new` para servicios o repositorios.
- **Async/await end-to-end** en operaciones I/O; cancelación con `CancellationToken`.
- **Configuración por variables de entorno**, nunca secretos hardcodeados ni en commits.

---

## Fase 1: Entorno, Google Cloud y Orquestación (Docker)
**Objetivo:** Variables de entorno, base de datos lista y `docker-compose` funcional levantando los 3 servicios.

1. **Google Cloud Platform**
   - Asume que el usuario habilitó: *Address Validation API*, *Maps JavaScript API* y *Geocoding API*.
   - La API Key se inyecta exclusivamente vía variables de entorno (jamás en código ni en repo).
   - Las claves reales las pondrá el desarrollador en `.env` después; escribe el código asumiendo que estarán disponibles en runtime.

2. **Variables de Entorno Centralizadas**
   - En la raíz, crear `.env` (gitignored) y `.env.example` (versionado) con:
     ```env
     # Base de datos
     DB_SA_PASSWORD=TuPasswordSeguro123!
     DB_NAME=Northwind

     # Google Maps (backend usa la primera, frontend la segunda)
     GOOGLE_MAPS_API_KEY=AIzaSy...
     VITE_GOOGLE_MAPS_API_KEY=AIzaSy...

     # Conexión generada (compose la inyecta al backend)
     ASPNETCORE_ENVIRONMENT=Development
     ```
   - Añadir `.env` al `.gitignore`.

3. **Base de Datos — Scripts de inicialización (orden importa)**
   - El script oficial `/database/script-northwindDataBase.sql` **NO crea la base `Northwind`** (declara explícitamente: *"This script does not create a database"*). Hay que crearla y seleccionarla antes.
   - Crear `/database/00-create-database.sql`:
     ```sql
     IF DB_ID('Northwind') IS NULL
       CREATE DATABASE Northwind;
     GO
     ```
   - Crear `/database/02-add-geo-columns.sql`:
     ```sql
     USE Northwind;
     GO
     IF COL_LENGTH('dbo.Orders', 'Latitude') IS NULL
       ALTER TABLE dbo.Orders ADD Latitude DECIMAL(18,15) NULL;
     IF COL_LENGTH('dbo.Orders', 'Longitude') IS NULL
       ALTER TABLE dbo.Orders ADD Longitude DECIMAL(18,15) NULL;
     GO
     ```
   - **Orden de ejecución en `db-init`**:
     1. `00-create-database.sql` (sobre `master`).
     2. `script-northwindDataBase.sql` con `sqlcmd -d Northwind` (la flag `-d` selecciona la base destino, ya que el script no tiene `USE`).
     3. `02-add-geo-columns.sql`.
   - Esto evita migraciones EF sobre el schema legacy y mantiene Northwind original intacto.

   **Notas sobre el schema Northwind que afectan al backend:**
   - La tabla `Order Details` **tiene un espacio en su nombre**. EF Core requiere `entity.ToTable("Order Details")` en `IEntityTypeConfiguration<OrderDetail>`. Su PK es compuesta (`OrderID`, `ProductID`).
   - `Orders.ShipRegion` es `nvarchar(15)` con texto libre (no es FK a la tabla `Region`). Está parcialmente poblado (~muchos NULL). Para el reporte "shipments by region" conviene:
     - Agrupar por `ShipCountry` (siempre presente y con buena cardinalidad), o
     - Tratar `ShipRegion` como texto y mostrar "Sin región" para NULL.
   - La tabla `Region` y `Territories` existe pero no está enlazada a `Orders` directamente — solo a `Employees` vía `EmployeeTerritories`. No usarla como fuente de regiones de envío.

4. **Orquestación con Docker Compose** (`docker-compose.yml` en la raíz, 4 servicios):
   - **`db`**: imagen `mcr.microsoft.com/mssql/server:2022-latest`. Recibe `SA_PASSWORD` desde `.env`. Volumen nombrado para persistencia (`mssql-data:/var/opt/mssql`). **Healthcheck** con `sqlcmd` consultando `SELECT 1` para que `depends_on` espere a que esté lista.
   - **`db-init`**: contenedor temporal basado en `mcr.microsoft.com/mssql-tools18/sql-tools` (la imagen `mssql-tools` clásica está deprecada). `depends_on: db: condition: service_healthy`. Ejecuta en orden:
     1. `sqlcmd -S db -U sa -P $DB_SA_PASSWORD -C -i /database/00-create-database.sql`
     2. `sqlcmd -S db -U sa -P $DB_SA_PASSWORD -C -d Northwind -i /database/script-northwindDataBase.sql`
     3. `sqlcmd -S db -U sa -P $DB_SA_PASSWORD -C -i /database/02-add-geo-columns.sql`
     - La flag `-C` confía en el certificado autofirmado del contenedor SQL Server. `restart: "no"`. Monta `/database` como volumen read-only.
   - **`backend`**: build desde `./backend/Dockerfile`. `depends_on: db-init: condition: service_completed_successfully`. Inyecta `ConnectionStrings__Default=Server=db;Database=Northwind;User Id=sa;Password=${DB_SA_PASSWORD};TrustServerCertificate=True;` y `GoogleMaps__ApiKey=${GOOGLE_MAPS_API_KEY}`. Expone `8080`.
   - **`frontend`**: build desde `./frontend/Dockerfile` con `args: VITE_GOOGLE_MAPS_API_KEY` y `VITE_API_BASE_URL=http://localhost:8080`. Mapea `80` (Nginx interno) → `8080` o `5173` del host. `depends_on: backend`.
   - Definir red `app-network` (bridge) común a los 4 servicios.

---

## Fase 2: Estructura del Backend (Clean Architecture)
**Objetivo:** Solución `.slnx`, proyectos por capa con jerarquía estricta de dependencias.

1. **Proyectos** (en `/backend`):
   - `Northwind.Domain` — entidades, excepciones de dominio, value objects. **Sin dependencias externas.**
   - `Northwind.Application` — DTOs, interfaces, validadores, mappers, casos de uso. Depende solo de `Domain`.
   - `Northwind.Infrastructure` — EF Core, servicios HTTP, generadores de reportes. Depende de `Domain` + `Application`.
   - `Northwind.Api` — controladores, middleware, DI, Swagger. Depende de `Application` + `Infrastructure`.
   - `Northwind.Tests` — pruebas unitarias e integración. Referencia las 3 primeras capas.
   - Generar `Northwind.slnx` y agregar todos los proyectos.

2. **Paquetes NuGet por capa**
   - **Application:** `AutoMapper`, `FluentValidation.DependencyInjectionExtensions`.
   - **Infrastructure:** `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.Http`, `QuestPDF`, `ClosedXML`, `Serilog.AspNetCore`, `Serilog.Sinks.Console`.
   - **Api:** `Swashbuckle.AspNetCore`.
   - **Tests:** `Microsoft.NET.Test.Sdk`, `xUnit`, `xunit.runner.visualstudio`, `Moq`, `FluentAssertions`, `coverlet.collector`, `Microsoft.EntityFrameworkCore.InMemory`.

3. **Manejo de Errores Global** (`Northwind.Api`)
   - Implementar `GlobalExceptionHandler : IExceptionHandler` (introducido en .NET 8, vigente en .NET 10).
   - Mapear excepciones de dominio:
     - `NotFoundException` → 404
     - `DomainException` / `ValidationException` → 400
     - Otras → 500
   - Respuesta siempre en formato `ProblemDetails` (RFC 7807) con `traceId` para correlación.
   - Registrar con `app.UseExceptionHandler()` y `services.AddProblemDetails()`.

4. **CORS**
   - Política `AllowFrontend` que habilite el origen del frontend (configurable por `Cors__AllowedOrigins`).
   - Aplicar antes de `UseAuthorization`.

5. **Logging**
   - Configurar Serilog en `Program.cs` con sink consola en formato JSON estructurado.
   - Incluir `RequestLogging` middleware.

---

## Fase 3: Lógica de Negocio (Domain & Application)
**Objetivo:** Entidades, DTOs, validaciones y contratos.

1. **Entidades (`Domain`)**
   - Mapear: `Customer`, `Employee`, `Shipper`, `Product`, `Order`, `OrderDetail`, `Region` (si aplica para reportes).
   - `Order` incluye propiedades navegacionales y los nuevos campos `Latitude` y `Longitude` (`decimal?`).
   - Excepciones de dominio: `NotFoundException`, `DomainException` en `Domain/Exceptions`.

2. **DTOs (`Application/DTOs`)**
   - Lectura: `OrderDto`, `OrderDetailDto`, `OrderListItemDto`, `ReportDataDto`.
   - Escritura: `CreateOrderDto`, `UpdateOrderDto`, `OrderDetailWriteDto`.
   - Filtros: `OrderFilterDto` con `{ Year?, Month?, Week?, Region?, Page, PageSize, SortBy, SortDir }`.
   - Geo: `AddressValidationRequestDto`, `AddressValidationResultDto { FormattedAddress, Latitude, Longitude, IsValid, Issues[] }`.

3. **AutoMapper**
   - Perfil `OrderProfile` con conversiones bidireccionales y proyección eficiente para listados.

4. **FluentValidation**
   - `CreateOrderDtoValidator`: cliente requerido, empleado requerido, al menos 1 línea, cantidades > 0, precios ≥ 0, fecha no futura.
   - Registrar todos los validadores con `AddValidatorsFromAssembly`.

5. **Interfaces (`Application/Interfaces`)**
   - `IOrderRepository` (CRUD + filtros paginados).
   - `IGeoLocationService` (validar dirección contra Google).
   - `IPdfReportService`, `IExcelReportService`.
   - `IUnitOfWork` (opcional, si se quiere transaccionalidad explícita).

6. **Servicios de aplicación (Use Cases)**
   - `OrderService` orquesta repositorio + geo + mapper. Aquí vive la lógica de negocio (no en controladores).

---

## Fase 4: Infraestructura, API Externa y Reportes
**Objetivo:** EF Core, Google Maps, generación de reportes y controladores.

1. **EF Core (`Infrastructure/Persistence`)**
   - `AppDbContext` con `DbSet` para cada entidad.
   - Configuración por **Fluent API** en clases `IEntityTypeConfiguration<T>`.
   - Mapear nombres de tabla respetando convenciones de Northwind:
     - `OrderDetail` → `entity.ToTable("Order Details")` (con espacio).
     - PK compuesta: `entity.HasKey(od => new { od.OrderID, od.ProductID })`.
     - Resto de tablas: nombre coincide con la entidad en plural (`Orders`, `Customers`, `Employees`, `Shippers`, `Products`).
   - `Latitude` / `Longitude`: `HasColumnType("decimal(18,15)")`, `IsRequired(false)`.
   - **Repositorio**: `OrderRepository` implementa filtros (Year/Month/Week derivados de `OrderDate`, Region desde `ShipRegion` o tabla relacionada), paginación y `AsNoTracking` en lecturas.

2. **Google Maps (`Infrastructure/External/GeoLocationService`)**
   - Inyectar `HttpClient` vía `IHttpClientFactory` (named client `"GoogleMaps"` con base URL y timeout).
   - `ValidateAddressAsync(string address)` → `POST https://addressvalidation.googleapis.com/v1:validateAddress?key={KEY}` con body `{ "address": { "addressLines": [address] } }`.
   - Parsear respuesta: extraer `result.address.formattedAddress`, `result.geocode.location.latitude/longitude`, y banderas de validación (`hasInferredComponents`, `hasReplacedComponents`).
   - Manejo defensivo: timeouts, 4xx/5xx → `DomainException` con mensaje útil para el frontend.
   - **No** loguear la API key.

3. **Reportes (`Infrastructure/Reports`)**
   - **QuestPDF** (`PdfReportService`):
     - `QuestPDF.Settings.License = LicenseType.Community;` en `Program.cs`.
     - Layout: header con branding "Northwind Traders" (logo placeholder), metadata del pedido (fechas, cliente, empleado, transportista, freight), tabla de líneas (producto, cantidad, precio unitario, descuento, subtotal), totales y footer con número de página.
   - **ClosedXML** (`ExcelReportService`):
     - Hoja "Orders" con encabezados estilizados (negrita, fondo), datos de pedidos filtrados, formato de fecha y moneda, auto-fit de columnas.
     - Sin licencia ni límite de filas.

4. **Controladores (`Api/Controllers`)** — delgados, solo orquestan
   - `OrdersController`:
     - `GET /api/orders` (con filtros + paginación)
     - `GET /api/orders/{id}`
     - `POST /api/orders`
     - `PUT /api/orders/{id}`
     - `DELETE /api/orders/{id}`
     - `GET /api/orders/{id}/pdf` → `FileContentResult` (`application/pdf`)
     - `GET /api/orders/excel?...filters` → `FileContentResult` (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`)
   - `LocationController`:
     - `POST /api/location/validate` → `AddressValidationResultDto`
   - `LookupsController` (poblar selects del frontend):
     - `GET /api/lookups/customers`, `/employees`, `/shippers`, `/products`, `/regions`
   - `ReportsController` (datos agregados para gráficos):
     - `GET /api/reports/orders-per-time?granularity=month`
     - `GET /api/reports/shipments-by-region` — agrupa por `ShipCountry` (más confiable que `ShipRegion`, que tiene muchos NULL en Northwind). Renombrar el rótulo del gráfico a "Envíos por país/región" o tratar NULL como "Sin región".

5. **Swagger**
   - Habilitar en `Development` con XML comments para documentación.

6. **Docker Backend** (`backend/Dockerfile`)
   - Multi-stage:
     - **build**: `mcr.microsoft.com/dotnet/sdk:10.0` → `dotnet restore` (capa cacheada) → `dotnet publish -c Release -o /app/publish`.
     - **runtime**: `mcr.microsoft.com/dotnet/aspnet:10.0`. Copia `/app/publish`. `EXPOSE 8080`. `ENV ASPNETCORE_URLS=http://+:8080`. Usuario no root.

---

## Fase 5: Estructura del Frontend (Vue 3 + Quasar + TS + pnpm)
**Objetivo:** Scaffolding, dependencias seguras, configuración base.

1. **Scaffolding**
   - En `/frontend`: `pnpm create vite . --template vue-ts`.
   - Integrar Quasar vía `@quasar/vite-plugin` (registrar en `vite.config.ts`, importar `quasar/src/css/index.sass` en `main.ts`, registrar plugins `Notify` y `Dialog`).
   - Alias `@` → `src` en `vite.config.ts` y `tsconfig.json`.

2. **Seguridad de Suministro (Supply Chain)**
   - Crear `frontend/.npmrc` con:
     ```
     minimum-release-age=2880
     ```
   - (Equivalente moderno a `pnpm-workspace.yaml`; previene instalar paquetes recién publicados, mitigando ataques de supply chain.)

3. **Dependencias**
   - Runtime: `pnpm add quasar @quasar/extras vue-router pinia axios @googlemaps/js-api-loader chart.js vue-chartjs date-fns`.
   - Dev: `pnpm add -D @quasar/vite-plugin sass vitest @vue/test-utils jsdom @vitest/coverage-v8`.

4. **Estructura de carpetas**
   ```
   src/
     components/         # componentes reutilizables (OrderForm, AddressMap, ChartCard, etc.)
     pages/              # vistas (DashboardPage, OrderEditPage, OrdersListPage)
     stores/             # pinia: useOrdersStore, useLookupsStore
     services/           # api.ts (axios), ordersService.ts, locationService.ts, reportsService.ts
     composables/        # useGoogleMaps, useAddressValidation
     types/              # tipos TS (Order, OrderDetail, AddressValidationResult, Filters)
     router/             # vue-router config
     utils/              # formatDate, downloadBlob, etc.
   ```

5. **Configuración de Axios y manejo global de errores** (`src/services/api.ts`)
   - `baseURL = import.meta.env.VITE_API_BASE_URL`.
   - Interceptor de respuesta:
     - Si `error.response?.data` cumple forma `ProblemDetails` (`type`, `title`, `detail`), extraer `detail || title`.
     - Llamar `Notify.create({ message, color: 'negative', icon: 'error', timeout: 5000 })`.
     - Re-lanzar el error para que componentes puedan reaccionar si quieren.
   - Tipar respuestas con genéricos (`api.get<OrderDto>(...)`).

6. **Variables de entorno frontend** (`frontend/.env.example`)
   ```
   VITE_API_BASE_URL=http://localhost:8080
   VITE_GOOGLE_MAPS_API_KEY=AIzaSy...
   ```

7. **Docker Frontend** (`frontend/Dockerfile`)
   - Multi-stage:
     - **build**: `node:20-alpine` → `corepack enable && pnpm install --frozen-lockfile` → `pnpm build`. Recibe `ARG VITE_GOOGLE_MAPS_API_KEY` y `ARG VITE_API_BASE_URL`, expuestos como `ENV` antes del build para que Vite los embeba.
     - **runtime**: `nginx:alpine`. Copia `dist/` a `/usr/share/nginx/html`. Config Nginx con `try_files $uri $uri/ /index.html;` (SPA fallback).

---

## Fase 6: UI, Componentes e Integraciones (Frontend)
**Objetivo:** Vistas funcionales conectadas al backend y a Google Maps.

1. **Formulario de Pedido (`OrderForm.vue`)**
   - Campos con componentes Quasar: `q-select` para cliente/empleado/transportista (datos vía `useLookupsStore`), `q-date` para fecha, repetidor de líneas con `q-select` producto + `q-input` cantidad + `q-input` descuento.
   - Campo "Dirección de envío" (`q-input`):
     - Evento `@blur` → llama a `locationService.validate(address)`.
     - Estado de carga con `q-spinner`.
     - Si válida: muestra dirección estandarizada y actualiza `Latitude`/`Longitude` del modelo.
     - Si inválida: muestra warning con `q-banner`.
   - **Mapa embebido** (`AddressMap.vue`):
     - Composable `useGoogleMaps` carga la JS API una sola vez (singleton).
     - Renderiza mapa centrado en lat/lng con marcador. Reactivo a cambios de coordenadas.
     - Estado vacío amigable cuando aún no hay dirección validada.

2. **Dashboard / Listado (`DashboardPage.vue`)**
   - Filtros superiores en una `q-card`: `q-select` Año, Mes, Semana (dependientes), `q-select` Región. Botón "Limpiar".
   - **Gráficos** (vue-chartjs) en grid responsivo:
     - **Líneas**: "Pedidos a lo largo del tiempo" (`/api/reports/orders-per-time`).
     - **Dona**: "Envíos por región" (`/api/reports/shipments-by-region`).
   - **Tabla** `q-table` con paginación server-side (sincroniza `pagination` con backend), columnas: cliente, fecha, productos (resumido), región, total.
     - Slot `top`: dos `q-btn` "Exportar Excel" y "Exportar PDF".
     - Acción descarga: `axios.get(url, { responseType: 'blob' })` → `downloadBlob` util crea link `<a>` con `URL.createObjectURL` y dispara click.
   - Acciones por fila: editar, eliminar (con `q-dialog` de confirmación), ver PDF.

3. **Estado (Pinia)**
   - `useOrdersStore`: estado de filtros, lista paginada, acciones CRUD.
   - `useLookupsStore`: cache de catálogos (clientes, empleados, productos, regiones) con TTL.

4. **Router**
   - Rutas: `/` (Dashboard), `/orders/new`, `/orders/:id/edit`.
   - Layout con `q-layout` + `q-header` con título "Northwind Traders".

---

## Fase 7: Testing
**Objetivo:** ≥80% cobertura backend (servicios + controladores) y bonus frontend (validación de dirección + interacciones de mapa).

1. **Backend (`Northwind.Tests`)**
   - **Estructura**: carpetas `Application/`, `Infrastructure/`, `Api/`, `Helpers/`.
   - **Application** (unit tests con Moq):
     - Validadores (`CreateOrderDtoValidator`): casos válidos, requeridos faltantes, cantidades inválidas, fechas futuras.
     - `OrderService`: crear pedido invoca geo + repo; not-found lanza `NotFoundException`; flujo de actualización; etc.
   - **Infrastructure**:
     - `OrderRepository` con `EF Core InMemory` o SQLite in-memory: filtros por año/mes/semana/región, paginación, ordenamiento.
     - `GeoLocationService` con `HttpMessageHandler` mockeado: respuestas válidas, dirección no encontrada, timeout, 401 (key inválida).
   - **Api**:
     - Tests de integración con `WebApplicationFactory<Program>` y BD InMemory: happy path de cada endpoint, 404, 400 (validación), formato `ProblemDetails`.
   - **Cobertura**:
     - `dotnet test --collect:"XPlat Code Coverage"` con Coverlet.
     - Generar reporte HTML con `reportgenerator` (documentado en README).
     - Excluir de cobertura: `Program.cs`, migraciones, DTOs sin lógica.
   - **Uso de IA (GitHub Copilot / Claude)**: documentar en README qué tests fueron asistidos por IA (requisito del reto).

2. **Frontend (Bonus, `frontend/tests`)**
   - **Vitest + Vue Test Utils + jsdom**.
   - `OrderForm.spec.ts`: validación de campos requeridos, evento `@blur` dispara llamada a servicio (mockeado), estado de error/éxito reflejado en UI.
   - `AddressMap.spec.ts`: monta con coordenadas mock, verifica que `useGoogleMaps` se invoca con la API key del entorno y se actualiza el marcador al cambiar `props`.
   - Mock de `@googlemaps/js-api-loader` y `Notify` de Quasar.
   - Script: `"test": "vitest run"`, `"test:coverage": "vitest run --coverage"`.

---

## Fase 8: Documentación (README)
**Objetivo:** Que un evaluador pueda levantar el proyecto sin contexto previo.

`README.md` en la raíz debe contener:

1. **Descripción** del proyecto y stack.
2. **Prerrequisitos**: Docker Desktop, Git. (No se requiere instalar .NET ni Node localmente para correr.)
3. **Configuración de claves de Google Maps**:
   - Pasos para habilitar las 3 APIs en Google Cloud Console.
   - Cómo crear una API Key restringida (HTTP referrers para frontend, IP para backend en producción).
   - Dónde pegar las claves (`.env` en raíz).
4. **Configuración de la base de datos**:
   - Cómo se inicializa Northwind (script + script de columnas geo).
   - Credenciales por defecto (modificables vía `.env`).
5. **Cómo levantar el proyecto**:
   ```bash
   cp .env.example .env
   # editar .env con las API keys reales
   docker-compose up -d --build
   ```
   - URLs:
     - Frontend: `http://localhost:8080`
     - Backend Swagger: `http://localhost:8080/swagger` (o el puerto que se defina)
     - SQL Server: `localhost:1433`
6. **Estructura de carpetas** (árbol resumido).
7. **Cómo correr tests y ver cobertura** (backend y frontend).
8. **Notas de arquitectura**: diagrama o explicación breve de Clean Architecture y flujo de una request.
9. **Tests asistidos por IA**: lista de archivos generados/asistidos con Copilot u otra herramienta.
10. **Troubleshooting**: problemas comunes (puerto ocupado, contraseña SA débil, API key sin habilitar, etc.).

---

## Checklist de Cumplimiento del Reto

| Requisito | Fase | Estado planificado |
|---|---|---|
| CRUD de pedidos con formulario completo | 4, 6 | ✅ |
| Address Validation API | 3, 4, 6 | ✅ |
| Coordenadas geocodificadas guardadas | 3, 4 | ✅ |
| Mapa embebido con marcador | 6 | ✅ |
| 1-2 gráficos (orders per time, shipments by region) | 4, 6 | ✅ |
| Tabla con filtros año/mes/semana + región | 4, 6 | ✅ |
| Exportar Excel y PDF desde toolbar | 4, 6 | ✅ |
| PDF con branding Northwind | 4 | ✅ |
| EF Core con Orders, OrderDetails, Customers, Employees, Shippers | 3, 4 | ✅ |
| ASP.NET Core REST API + Clean Architecture | 2, 3, 4 | ✅ |
| Vue 3 + Quasar | 5, 6 | ✅ (con TypeScript) |
| Tests backend ≥80% cobertura | 7 | ✅ |
| Tests frontend (bonus) | 7 | ✅ |
| Documentación API keys + BD | 8 | ✅ |
| Bonus: error handling end-to-end | 2, 5 | ✅ |
| Bonus: Docker | 1, 4, 5 | ✅ |

---

## Instrucciones Finales
- Toda la app debe levantarse con `docker-compose up -d --build` desde la raíz.
- Un solo `.env` en raíz controla todas las credenciales (BD + Google Maps).
- Código limpio: nombres descriptivos, funciones pequeñas, sin comentarios obvios, sin código muerto.
- Commits atómicos por fase recomendado.
- README final debe permitir a cualquier evaluador correr el proyecto en menos de 5 minutos.
