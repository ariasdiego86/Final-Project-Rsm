# 🚀 Planning de Desarrollo: Reto Técnico - Northwind Order Management System

## 📋 Contexto y Reglas del Proyecto
Desarrollo de una aplicación Full-Stack para gestión de pedidos utilizando la base de datos **Northwind (SQL Server)**. El sistema no requiere autenticación ni login (se omite la gestión de usuarios). 
Toda la aplicación debe estar contenerizada y orquestada para levantarse en desarrollo con un solo comando (`docker-compose up -d`).

### Stack Tecnológico:
- **Backend:** ASP.NET Core (REST API), Clean Architecture (4 capas + Tests), Entity Framework Core, C#, .net10.
- **Frontend:** Vue 3, Quasar Framework, TypeScript, pnpm.
- **Integraciones Externas:** Google Maps Platform (Address Validation API, Maps JavaScript API, Geocoding API).
- **Reportes:** QuestPDF (PDF) y GemBox.Spreadsheet (Excel).
- **Orquestación:** Docker y Docker Compose.

---

## Fase 1: Entorno, Google Cloud y Orquestación (Docker)
**Objetivo:** Configurar las variables de entorno, preparar la base de datos y crear la orquestación para levantar todo "de una".

1. **Google Cloud Platform:**
   - Asume que el usuario ya habilitó las APIs: *Address Validation API*, *Maps JavaScript API* y *Geocoding API*.
   - La API Key generada se manejará estrictamente mediante variables de entorno.
   - Por obvias razones las variables de entorno las pondrá yo (el desarollador) después en el archivo `.env` lo que signfica que aún no están disponibles. Sin embargo, debes escribir el código asumiendo que estarán disponibles en el entorno de ejecución.

2. **Variables de Entorno Centralizadas:**
   - En la raíz del proyecto, crea un archivo `.env` (y un `.env.example` para versionar) con:
     ```env
     DB_SA_PASSWORD=TuPasswordSeguro123!
     GOOGLE_MAPS_API_KEY=AIzaSy...
     VITE_GOOGLE_MAPS_API_KEY=AIzaSy...
     ```

3. **Orquestación con Docker Compose:**
   - Asume que existe una carpeta `/database` en la raíz que contiene el archivo `script-northwindDataBase.sql`.
   - Crea un archivo `docker-compose.yml` en la raíz definiendo 4 servicios:
     - `db`: Imagen `mcr.microsoft.com/mssql/server:2022-latest`. Pasa la contraseña sa desde el `.env`.
     - `db-init`: Un contenedor temporal basado en `mcr.microsoft.com/mssql-tools` que dependa de `db`. Su comando debe esperar a que SQL Server esté listo y luego ejecutar `sqlcmd` para correr `/database/script-northwindDataBase.sql`.
     - `backend`: Construido desde `./backend/Dockerfile`. Depende de `db`. Se le debe inyectar el string de conexión (usando el nombre del servicio `db`) y `Maps_API_KEY`.
     - `frontend`: Construido desde `./frontend/Dockerfile`. Se le debe inyectar `VITE_GOOGLE_MAPS_API_KEY` como argumento de compilación (build arg) para que Vite lo reemplace. Pasa el puerto 80 (Nginx) al 5173 o 8080 del host.

---

## Fase 2: Estructura del Backend (Clean Architecture)
**Objetivo:** Crear la solución `.slnx` y los proyectos de las capas respetando la jerarquía estricta de dependencias.

1. **Creación de Proyectos:**
   - Crea la carpeta `backend` y dentro inicializa los siguientes proyectos de biblioteca de clases y Web API:
     - `Northwind.Domain` (Sin dependencias externas)
     - `Northwind.Application` (Referencia a `Domain`)
     - `Northwind.Infrastructure` (Referencia a `Domain` y `Application`)
     - `Northwind.Api` (Referencia a `Application` y `Infrastructure`)
     - `Northwind.Tests` (Referencia a `Domain`, `Application` e `Infrastructure`)
   - Genera un archivo de solución `Northwind.slnx` en la carpeta `backend` y agrega todos los proyectos.

2. **Instalación de Paquetes NuGet:**
   - *Application:* `AutoMapper`, `FluentValidation.DependencyInjectionExtensions`.
   - *Infrastructure:* `Microsoft.EntityFrameworkCore.SqlServer`, `QuestPDF`, `GemBox.Spreadsheet`, `Microsoft.Extensions.Http`.
   - *Api:* `Swashbuckle.AspNetCore` (Swagger).
   - *Tests:* `Moq`, `xUnit`, `FluentAssertions`.

3. **Manejo de Errores Global:**
   - En `Northwind.Api`, implementa un `GlobalExceptionHandler` usando `IExceptionHandler` de ASP.NET Core 8+. 
   - Debe capturar excepciones personalizadas (`NotFoundException`, `DomainException`) y devolver respuestas HTTP 404 o 400 en formato estándar `ProblemDetails` (RFC 7807).

---

## Fase 3: Lógica de Negocio (Domain & Application)
**Objetivo:** Definir las entidades de la base de datos, DTOs, validaciones y contratos (interfaces).

1. **Entidades (`Domain`):** - Mapea las tablas clave de Northwind: `Customer`, `Employee`, `Shipper`, `Order` y `OrderDetail`.
   - **Crucial:** A la entidad `Order`, añádele propiedades para las coordenadas: `Latitude` (decimal?) y `Longitude` (decimal?), ya que necesitamos guardar el resultado de Google Maps.
2. **DTOs y Mapeos (`Application`):** - Crea DTOs (ej. `OrderDto`, `CreateOrderDto`, `ReportDataDto`).
   - Usa `AutoMapper` para configurar las conversiones entre Entidades y DTOs.
3. **Validaciones (`Application`):** - Implementa `FluentValidation` para validar los DTOs de entrada (ej. cantidades mayores a cero, campos obligatorios).
4. **Interfaces (`Application`):** - Define los contratos que implementará la infraestructura: `IOrderRepository`, `IGeoLocationService` (para validar direcciones con Google) y `IReportService` (para generar PDF y Excel).

---

## Fase 4: Infraestructura y API Externa
**Objetivo:** Acceso a datos con EF Core, integración con Google Maps, Reportes y Controladores.

1. **EF Core (`Infrastructure`):** - Configura `AppDbContext`. Usa *Fluent API* para mapear las relaciones. 
   - Asegúrate de definir las columnas `Latitude` y `Longitude` con alta precisión (ej. `decimal(18,15)`).
2. **Google Maps (`Infrastructure`):** - Implementa `GeoLocationService`. Usa `IHttpClientFactory` para hacer un `POST` a `https://addressvalidation.googleapis.com/v1:validateAddress?key={API_KEY}`. 
   - Extrae del JSON de respuesta la dirección estandarizada y la ubicación (latitud/longitud).
3. **Reportes (`Infrastructure`):**
   - **QuestPDF:** Configura `QuestPDF.Settings.License = LicenseType.Community;`. Diseña un PDF estructurado (header con logo falso de Northwind, tabla de detalles de pedido, totales).
   - **GemBox:** Configura `SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");`. Crea un archivo Excel con los metadatos de los pedidos.
4. **Controladores (`Api`):** - `OrdersController`: Endpoints para CRUD (`GET`, `POST`, `PUT`, `DELETE`).
   - Endpoints de exportación: `GET /api/orders/{id}/pdf` y `GET /api/orders/excel`.
   - Endpoint de validación: `POST /api/location/validate` para que el frontend valide la dirección mientras el usuario escribe.
5. **Docker Backend:** Crea `backend/Dockerfile`. Usa un multi-stage build (`mcr.microsoft.com/dotnet/sdk` para build, y `aspnet` para runtime). Expón el puerto 8080.

---

## Fase 5: Estructura del Frontend (Vue 3 + Quasar + TS + pnpm)
**Objetivo:** Scaffolding inicial, seguridad de dependencias y configuración base.

1. **Scaffolding:** - Crea la carpeta `frontend` y genera el proyecto: `pnpm create vite . --template vue-ts`.
2. **Seguridad de Suministro (Supply Chain):**
   - Crea en la raíz de `frontend` el archivo `pnpm-workspace.yaml` con este contenido exacto:
     ```yaml
     minimumReleaseAge: 2880
     ```
3. **Dependencias:**
   - Instala: `pnpm add quasar @quasar/extras vue-router pinia axios @googlemaps/js-api-loader chart.js vue-chartjs`.
   - Instala dev: `pnpm add -D @quasar/vite-plugin sass`.
4. **Manejo de Errores Global (Axios a Quasar):**
   - Configura la instancia de Axios en `src/services/api.ts`.
   - **Regla estricta:** Crea un interceptor de respuestas (`api.interceptors.response.use`). Si la API devuelve un error 400 o 500 y el cuerpo es un `ProblemDetails`, extrae el campo `detail` o `title` y usa `Notify.create({ message: errorText, color: 'negative' })` de Quasar para mostrar el error al usuario.
5. **Docker Frontend:** - Crea `frontend/Dockerfile` (Multi-stage build). Usa Node para `pnpm install` y `pnpm build`. Luego, usa `nginx:alpine` para copiar la carpeta `dist` y servirla. Asegúrate de pasar el build arg `VITE_GOOGLE_MAPS_API_KEY`.

---

## Fase 6: Desarrollo de la UI y Componentes (Frontend)
**Objetivo:** Desarrollar las vistas y conectar con las integraciones.

1. **Formulario de Pedidos (Componente CRUD):**
   - Usa componentes de Quasar (`q-input`, `q-select`, `q-form`).
   - Incluye un campo para "Dirección de envío". Al dispararse el evento `@blur` (pérdida de foco), llama a `/api/location/validate`.
   - Si la dirección es válida, obtén la latitud/longitud y muéstralas en un mapa embebido de Google Maps usando la librería `@googlemaps/js-api-loader`. Coloca un marcador en las coordenadas exactas.
2. **Dashboard y Reportes (Vista Principal):**
   - Crea un `q-table` para mostrar los pedidos.
   - Añade filtros superiores: Selección por Año/Mes/Semana y un selector de Región.
   - En el `template v-slot:top` de la tabla, agrega dos botones: "Exportar Excel" y "Exportar PDF". Estos deben hacer la petición GET al backend y forzar la descarga del archivo binario (`blob`) en el navegador.
3. **Visualización de Datos (Gráficos):**
   - Usando `vue-chartjs` y `chart.js`, añade en la parte superior del Dashboard dos gráficos:
     - Gráfico de Líneas: "Pedidos a lo largo del tiempo" (Orders per time).
     - Gráfico de Dona/Pastel: "Envíos por región" (Shipments by region).

---
**INSTRUCCIONES FINALES*
- El proyecto debe ser completamente funcional al ejecutar `docker-compose up -d` desde la raíz.
- Resume todo lo desarrollado en un README.md con instrucciones claras para levantar el proyecto, estructura de carpetas y cualquier detalle relevante.
