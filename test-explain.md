# Explicación de los Tests

Este documento explica cómo se diseñó e implementó cada grupo de tests del proyecto, qué se decidió probar, y los problemas técnicos que surgieron durante el proceso.

---

## Backend — 57 tests (xUnit + Moq + FluentAssertions)

### 1. Validator tests (`CreateOrderDtoValidatorTests`) — 13 tests

**Qué se prueba:** Las reglas de FluentValidation del DTO de creación de órdenes.

**Estrategia:** Tests puramente unitarios sin ninguna dependencia externa. Se instancia `CreateOrderDtoValidator` directamente y se usa el helper `.TestValidateAsync()` de `FluentValidation.TestHelper`. Cada test parte de un DTO válido (`ValidDto()`) y muta un solo campo para verificar que la regla específica dispara.

Casos cubiertos:
- `CustomerID` vacío o de más de 5 caracteres
- `EmployeeID` igual a cero
- `OrderDate` en el futuro
- `RequiredDate` anterior a `OrderDate`
- `Freight` negativo
- `OrderDetails` vacío
- Línea con `ProductID = 0`, `Quantity = 0`, `UnitPrice < 0`, `Discount > 1`
- Más de 100 líneas de detalle

---

### 2. Repository tests (`OrderRepositoryTests`) — 10 tests

**Qué se prueba:** La lógica de filtrado, paginación y persistencia en `OrderRepository`, usando EF Core InMemory.

**Estrategia:** Cada test crea su propia instancia de `AppDbContext` con un nombre de base de datos único (helper `InMemoryDbHelper.CreateContext()`), llama a `SeedData.Seed()` para insertar datos de prueba controlados, e instancia `OrderRepository` directamente. No hay HTTP ni DI framework involucrado.

Por qué base de datos por test: InMemory es un store global por nombre. Si dos tests comparten el mismo nombre, los datos se mezclan. Usar `Guid.NewGuid()` como nombre garantiza aislamiento.

Casos cubiertos:
- `GetPagedAsync` sin filtros devuelve los 3 registros semilla
- Filtro por año, por año+mes, por región
- Paginación respeta `PageSize`
- `GetByIdAsync` devuelve `null` para IDs inexistentes
- `GetByIdAsync` trae las relaciones (`OrderDetails`, `Customer`, `Employee`)
- `CreateAsync` persiste el nuevo registro
- `DeleteAsync` elimina la orden y sus detalles

---

### 3. Service tests (`OrderServiceTests`) — 14 tests

**Qué se prueba:** La lógica de negocio en `OrderService`, aislada del repositorio y del servicio externo de geolocalización.

**Estrategia:** Tests unitarios. Se usan mocks de Moq para `IOrderRepository`, `IGeoLocationService`, `IPdfReportService` e `IExcelReportService`. AutoMapper se configura con el contenedor de DI real para evitar falsos negativos por mapeos mal configurados.

**Problema encontrado:** AutoMapper 16.x requiere `ILoggerFactory` en el contenedor. El test fallaba con `No service for type ILoggerFactory`. Solución: agregar `services.AddLogging()` antes de `services.AddAutoMapper(...)`.

Casos cubiertos:
- `GetOrderByIdAsync` devuelve DTO cuando existe, lanza `NotFoundException` cuando no
- `GetOrdersAsync` devuelve resultado paginado correctamente mapeado
- `CreateOrderAsync` llama a geo cuando hay dirección pero no coordenadas
- `CreateOrderAsync` omite geo cuando las coordenadas ya vienen en el DTO
- `CreateOrderAsync` lanza `ValidationException` si el DTO es inválido
- `UpdateOrderAsync` lanza `NotFoundException` si la orden no existe
- `DeleteOrderAsync` lanza `NotFoundException` o delega al repositorio
- `GenerateOrderPdfAsync` lanza `NotFoundException` si la orden no existe
- `GetOrdersPerTimeAsync` agrupa órdenes por mes correctamente
- `GetShipmentsByRegionAsync` agrupa `null` como "Sin región"

---

### 4. GeoLocation service tests (`GeoLocationServiceTests`) — 4 tests

**Qué se prueba:** `GeoLocationService`, el cliente HTTP que llama a la API de Google Maps Address Validation.

**Estrategia:** Se mockea `HttpMessageHandler` mediante `Moq.Protected` para interceptar las llamadas HTTP sin hacer peticiones reales. Se construye un `IHttpClientFactory` falso que devuelve un `HttpClient` con el handler mockeado.

Casos cubiertos:
- Respuesta 200 con JSON válido → devuelve coordenadas y dirección formateada
- Respuesta 200 con `hasInferredComponents: true` → agrega issue al resultado
- Respuesta 400 → lanza `DomainException` con el código de estado en el mensaje
- Respuesta 401 → lanza `DomainException`

---

### 5. Integration tests (`NorthwindApiIntegrationTests`) — 16 tests

**Qué se prueba:** Los endpoints HTTP completos, desde el controller hasta el repositorio, pasando por el pipeline de ASP.NET Core.

**Estrategia:** Se usa `WebApplicationFactory<Program>` de `Microsoft.AspNetCore.Mvc.Testing`. La factory reemplaza la base de datos real por EF Core InMemory y sustituye `IGeoLocationService` por un stub que siempre devuelve coordenadas fijas. Cada test llama a `SeedDatabase()` en el constructor para partir de un estado limpio.

**Problemas encontrados y sus causas:**

1. **Dos providers de base de datos registrados:** EF Core 8+ registra `IDbContextOptionsConfiguration<TContext>` además de `DbContextOptions<TContext>`. Eliminar solo el tipo convencional dejaba el descriptor de SQL Server activo. Solución: eliminar todos los descriptores cuyo primer argumento genérico sea `AppDbContext`.

2. **Tests veían la base de datos vacía:** El `Guid.NewGuid()` estaba dentro del lambda de `AddDbContext`. Como `DbContextOptions<TContext>` es scoped, el lambda se reevaluaba por scope y cada scope se conectaba a una base de datos diferente. El seed iba a un scope, las peticiones HTTP a otro. Solución: capturar el Guid en una variable antes del lambda.

3. **Conflicto de tracking en `UpdateAsync`:** Al volver a agregar `OrderDetail` con la propiedad de navegación `.Order` apuntando a una entidad no rastreada con la misma PK que la entidad rastreada `existing`, EF Core lanzaba un error de clave duplicada. Solución: crear nuevos objetos `OrderDetail` sin propiedades de navegación.

4. **Seed duplicado entre tests:** `SeedDatabase()` se llama en el constructor del test, que xUnit instancia una vez por test. En el segundo test, el seed intentaba insertar PKs ya existentes. Solución: llamar `db.Database.EnsureDeleted()` + `db.Database.EnsureCreated()` antes de hacer el seed para limpiar el store InMemory.

Endpoints cubiertos:
- `GET /api/orders` — 200 con resultado paginado
- `GET /api/orders?year=1996` — filtra por año
- `GET /api/orders/{id}` — 200 con detalles, 404 para IDs inexistentes
- `POST /api/orders` — 201 con orden creada, 400 con ProblemDetails para DTO inválido
- `PUT /api/orders/{id}` — 200 con datos actualizados, 404 para IDs inexistentes
- `DELETE /api/orders/{id}` — 204, 404 para IDs inexistentes
- `GET /api/lookups/customers`, `/employees`, `/products`
- `GET /api/reports/orders-per-time`, `/shipments-by-region`
- `POST /api/location/validate`
- `GET /api/orders/excel` — verifica Content-Type `spreadsheetml`
- `GET /api/orders/{id}/pdf` — verifica Content-Type `application/pdf`

---

## Frontend — 8 tests (Vitest + Vue Test Utils)

### Configuración global (`src/tests/setup.ts`)

Antes de cada test se crea una instancia fresca de Pinia con `setActivePinia(createPinia())`. Quasar se registra como plugin global incluyendo explícitamente `Notify`, ya que sin él `$q.notify` no existe en el entorno de tests y los componentes que lo usan lanzarían un error.

```ts
config.global.plugins = [[Quasar, { plugins: { Notify } }]]
```

---

### `AddressMap.spec.ts` — 5 tests

**Qué se prueba:** El componente `AddressMap.vue`, que carga Google Maps y renderiza un mapa interactivo.

**Estrategia:** Se mockean dos módulos externos para evitar inicializar el SDK real de Google Maps en jsdom:

- `@/composables/useGoogleMaps` → devuelve `mockLoad` (fn controlable) y un ref `mapsError`
- `@googlemaps/js-api-loader` → `importLibrary` devuelve constructores falsos para `Map` y `AdvancedMarkerElement`

**Problema técnico clave — arrow functions como constructores:**
El componente usa `new Map(container, opts)` y `new AdvancedMarkerElement(opts)`. Si el mock usaba una arrow function, JavaScript lanzaba `TypeError: MockMap is not a constructor`. Solución: usar funciones regulares con `function(this: Record<string,unknown>) { ... }`.

**Problema técnico clave — `flush: 'post'` en el watcher:**
El test que verifica que el mapa se inicializa cuando las props cambian de `null` a coordenadas válidas fallaba porque `mockLoad` no se llamaba. El `watch` del componente tenía `flush: 'pre'` (el default), que dispara antes del update del DOM. Cuando el watcher corría, el bloque `v-else` con `ref="mapContainer"` aún no estaba renderizado, entonces `mapContainer.value` era `null` y `initMap()` retornaba sin hacer nada. Solución: agregar `{ flush: 'post' }` al watcher para que corra después del update del DOM.

Casos cubiertos:
- Muestra texto placeholder cuando `latitude` y `longitude` son `null`
- Llama a `load()` al montar con coordenadas válidas
- Renderiza el div del mapa (no el placeholder) cuando hay coordenadas
- Llama a `load()` vía el watcher cuando las props cambian de `null` a valores válidos
- Llama a `setCenter` cuando las coordenadas cambian después de que el mapa ya está inicializado

---

### `OrderForm.spec.ts` — 3 tests

**Qué se prueba:** El componente `OrderForm.vue`, que gestiona creación/edición de órdenes con validación de dirección.

**Estrategia:** Se mockean varios módulos para aislar el componente:

- `@/composables/useAddressValidation` → `mockValidateAddress`, `mockReset`, refs controlables `geoResult` y `geoValidationError`
- `@/services/ordersService` → `mockCreate`, `mockUpdate`
- `@/stores/useLookupsStore` → datos de prueba fijos (clientes, empleados, productos)
- `vue-router` → `useRouter` con `push` mockeado
- `@/components/AddressMap.vue` → stub simple `<div class="map-stub" />` para evitar inicializar Maps

**Problema técnico — `vi.hoisted()` para mocks en factories:**
`vi.mock` es hoisted (elevado) por Vitest antes de que se ejecute cualquier declaración `const`. Si se declara `const mockCreate = vi.fn()` en el cuerpo del módulo y luego se usa dentro del factory de `vi.mock`, `mockCreate` aún es `undefined` en el momento de la hoisting. Solución: declarar las variables que se usan en factories con `vi.hoisted()`:

```ts
const { mockCreate, mockUpdate } = vi.hoisted(() => ({
  mockCreate: vi.fn(),
  mockUpdate: vi.fn(),
}))
```

**Problema técnico — eventos de Quasar en tests:**
`wrapper.find('input').trigger('blur')` no propaga el evento al handler `@blur` del `<q-input>` porque Quasar envuelve el input nativo en su propia capa de componente. Los eventos DOM nativos dentro de un componente de Quasar no se propagan automáticamente al nivel Vue. Solución: emitir a nivel del componente Vue:

```ts
await addressQInput.vm.$emit('update:modelValue', 'Obere Str. 57')
await nextTick()
await addressQInput.vm.$emit('blur')
```

Casos cubiertos:
- No llama a `ordersService.create` ni `update` si `customerID` y `employeeID` están vacíos
- Llama a `validate` con la dirección combinada cuando el campo de dirección pierde el foco
- Muestra el `QBanner` de advertencia cuando `geoValidationError` tiene valor

---

## Cómo ejecutar todos los tests

**Backend (requiere Linux o Docker por Windows Smart App Control):**
```bash
docker run --rm \
  -v "$(pwd)/backend:/app" \
  -w /app \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/Northwind.Tests -v minimal
```
Resultado esperado: **57 passed, 0 failed**

**Frontend:**
```bash
cd frontend
npm test
```
Resultado esperado: **8 passed, 0 failed** (2 spec files)
