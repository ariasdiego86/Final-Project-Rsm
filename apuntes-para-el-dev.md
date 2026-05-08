# Apuntes para el Dev — Pasos manuales después de que la IA termine

Este documento lista **todo lo que tú (el desarrollador) tienes que hacer a mano** una vez que el modelo de IA termine de generar el código siguiendo `planning.md`. El objetivo es que puedas levantar la app y aprobar la prueba con el menor esfuerzo posible.

---

## TL;DR — el flujo en 5 pasos

1. Conseguir una API Key de Google Maps con 3 APIs habilitadas.
2. Copiar `.env.example` a `.env` y pegar la key.
3. Verificar que el script SQL de Northwind esté en `/database/`.
4. Ejecutar `docker-compose up -d --build`.
5. Abrir el frontend y probar.

Si algo no funciona → ver sección **Troubleshooting** al final.

---

## 1. Google Cloud Platform — obtener la API Key

Solo necesitas **una** API Key. La misma key sirve para backend y frontend (aunque las restringes diferente, ver punto 1.4).

### 1.1 Crear proyecto (si no tienes uno)
1. Entra a https://console.cloud.google.com/
2. Arriba a la izquierda, "Select a project" → "New Project".
3. Nombre cualquiera (ej. `northwind-test`). Crear.

### 1.2 Habilitar facturación
- Google Maps APIs requieren tener **Billing** habilitado, aunque haya un free tier mensual.
- Menú lateral → "Billing" → vincular tarjeta o cuenta de facturación.
- **Importante**: el reto entra holgado dentro del free tier mensual ($200 USD de crédito), no te debería cobrar nada si solo es para la prueba.

### 1.3 Habilitar las 3 APIs requeridas
Menú lateral → "APIs & Services" → "Library". Buscar y habilitar **una por una**:

| API | Para qué la usa la app |
|---|---|
| **Address Validation API** | Backend: validar direcciones que el usuario escribe en el formulario |
| **Maps JavaScript API** | Frontend: mostrar el mapa embebido con el marcador |
| **Geocoding API** | Backend/Frontend: convertir direcciones a coordenadas (algunos flujos pueden caer aquí como respaldo) |

> Si te falta cualquiera de las 3, **algo va a fallar en silencio** (la API responde 403). Verifica las 3 antes de seguir.

### 1.4 Crear la API Key
1. Menú lateral → "APIs & Services" → "Credentials".
2. "Create Credentials" → "API Key".
3. Te muestra la key (algo como `AIzaSy...`). **Cópiala.**
4. Click en "Edit API key" para restringirla:
   - **Application restrictions** → "None" (para la prueba local; en producción restringirías por IP/referrer).
   - **API restrictions** → "Restrict key" → marcar las 3 APIs habilitadas arriba.
   - Guardar.

> Restringir la key a solo esas 3 APIs es buena práctica de seguridad y previene cargos accidentales si la key se filtra.

---

## 2. Configurar el archivo `.env`

En la raíz del proyecto debe existir `.env.example` (lo crea la IA). Tú haces:

```bash
cp .env.example .env
```

Edita `.env` y rellena estos valores:

```env
# Contraseña SA del SQL Server contenedor.
# Debe cumplir la política de SQL Server: mínimo 8 chars, mayúsculas+minúsculas+número+símbolo.
DB_SA_PASSWORD=Northwind#2026!Secure

# Nombre de la base de datos (no cambiar salvo que sepas qué haces)
DB_NAME=Northwind

# Tu API Key de Google Maps (la misma para los dos)
GOOGLE_MAPS_API_KEY=AIzaSy...PEGA_AQUI_TU_KEY
VITE_GOOGLE_MAPS_API_KEY=AIzaSy...PEGA_AQUI_TU_KEY

# Entorno ASP.NET Core
ASPNETCORE_ENVIRONMENT=Development
```

**Reglas:**
- `GOOGLE_MAPS_API_KEY` y `VITE_GOOGLE_MAPS_API_KEY` deben tener **el mismo valor** (es la misma key, solo que Vite necesita el prefijo `VITE_` para inyectarla en el bundle del frontend).
- `DB_SA_PASSWORD` **debe ser fuerte** o SQL Server rechazará el contenedor. Mínimo 8 caracteres con mayúscula, minúscula, número y símbolo.
- **Nunca commitees `.env`.** Verifica que `.gitignore` lo incluya. `.env.example` sí va al repo (con valores placeholder).

---

## 3. Verificar la base de datos

En `/database/` deben existir **3 archivos** después de que la IA termine:

| Archivo | Origen | Qué hace |
|---|---|---|
| `00-create-database.sql` | Lo crea la IA | `CREATE DATABASE Northwind` (el script oficial NO lo hace) |
| `script-northwindDataBase.sql` | **Ya lo tienes tú** (oficial de Microsoft, ~9350 líneas) | Crea tablas y carga datos |
| `02-add-geo-columns.sql` | Lo crea la IA | Añade columnas `Latitude` y `Longitude` a `Orders` |

**Acción tuya:** solo verifica que los 3 archivos existan. Si la IA olvidó crear el 00 o el 02, créalos tú con el contenido del `planning.md` (Fase 1, punto 3).

---

## 4. Levantar la app

Desde la raíz del proyecto:

```bash
docker-compose up -d --build
```

La primera vez tarda **5-10 minutos** (descarga imágenes, compila .NET, hace `pnpm install`, ejecuta el script SQL completo).

### Cómo saber que está todo arriba

```bash
docker-compose ps
```

Deberías ver:
- `db` → `Up (healthy)`
- `db-init` → `Exited (0)` ← es normal, ya hizo su trabajo
- `backend` → `Up`
- `frontend` → `Up`

### URLs

| Servicio | URL local |
|---|---|
| Frontend (la app) | http://localhost:8080 |
| Backend Swagger | http://localhost:5000/swagger (o el puerto que defina la IA) |
| SQL Server | `localhost:1433` (usuario `sa`, password de tu `.env`) |

> Los puertos exactos dependen del mapeo en `docker-compose.yml`. Revísalo si alguno colisiona con algo que ya tengas corriendo.

---

## 5. Probar que todo funciona (smoke test)

Antes de entregar, verifica manualmente:

- [ ] Abre `http://localhost:8080` → carga el dashboard sin errores en consola.
- [ ] Aparecen los 2 gráficos (líneas + dona) con datos.
- [ ] La tabla muestra pedidos y los filtros (año/mes/región) funcionan.
- [ ] Click en "Nuevo pedido": el formulario carga clientes, empleados y productos en los selects.
- [ ] Escribes una dirección real y al salir del campo (`blur`):
  - Aparece la dirección estandarizada.
  - El mapa carga y pone un marcador.
- [ ] Guardas el pedido → aparece en la tabla.
- [ ] Click en "Exportar PDF" → descarga un PDF con branding Northwind.
- [ ] Click en "Exportar Excel" → descarga un `.xlsx` válido.
- [ ] Provoca un error (ej. crea pedido sin productos) → aparece notificación roja de Quasar con mensaje útil (esto valida el bonus de error handling end-to-end).

---

## 6. Tests (requisito del reto: 80% backend)

### Backend

```bash
cd backend
dotnet test --collect:"XPlat Code Coverage"
```

Para reporte HTML legible:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool   # solo la primera vez
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
# Abre coverage-report/index.html en el navegador
```

**Verifica que cobertura de Application + Infrastructure + Api esté ≥ 80%.** Si no, pídele a la IA (o a Copilot) que genere los tests faltantes para los servicios/controladores que estén bajos.

### Frontend (bonus)

```bash
cd frontend
pnpm test            # corre suite
pnpm test:coverage   # con cobertura
```

---

## 7. Documentar tests asistidos por IA

El reto pide explícitamente: *"Usar herramientas de IA para generar pruebas para los endpoints de la API y la lógica de negocio"*.

En el `README.md` (sección "Tests asistidos por IA") añade una lista breve, por ejemplo:

```markdown
## Tests asistidos por IA
Los siguientes archivos de test fueron generados o asistidos con Claude / GitHub Copilot:
- `Northwind.Tests/Application/OrderServiceTests.cs`
- `Northwind.Tests/Infrastructure/GeoLocationServiceTests.cs`
- `Northwind.Tests/Api/OrdersControllerIntegrationTests.cs`
- `frontend/tests/OrderForm.spec.ts` (bonus)
- `frontend/tests/AddressMap.spec.ts` (bonus)
```

Esto cuenta como evidencia para los evaluadores.

---

## 8. Commits y entrega

Antes de empujar al repo:

- [ ] `.env` está en `.gitignore` y **NO** se ha commiteado.
- [ ] `.env.example` sí está commiteado, con valores dummy.
- [ ] `README.md` tiene la guía de setup completa (la IA la genera en Fase 8 del planning).
- [ ] `docker-compose up -d --build` desde una carpeta limpia y un `.env` recién copiado **funciona** sin pasos manuales adicionales.
- [ ] Cobertura backend ≥ 80% (verificado con reporte HTML).

---

## 9. Troubleshooting

### "Login failed for user 'sa'"
- La contraseña de `.env` no cumple la política de SQL Server. Cámbiala a una con mayús+minús+num+símbolo y mínimo 8 chars.
- Borra el volumen y vuelve a levantar: `docker-compose down -v && docker-compose up -d --build`.

### "Database 'Northwind' does not exist"
- Falta el archivo `00-create-database.sql` o `db-init` no lo está ejecutando primero. Revisa el orden en `docker-compose.yml`.

### El mapa no carga / consola del navegador dice "RefererNotAllowedMapError" o "ApiNotActivatedMapError"
- No habilitaste alguna de las 3 APIs en Google Cloud (sección 1.3).
- O la API Key tiene restricciones de referrer activas y `localhost:8080` no está permitido. Pon "Application restrictions" en "None" para la prueba local.

### "Address validation returned 403"
- La key no tiene habilitada **Address Validation API** específicamente. Habilítala en GCP Console.
- Suele tardar 1-2 minutos en propagarse después de habilitar.

### Frontend muestra `undefined` para `VITE_GOOGLE_MAPS_API_KEY`
- Vite embebe las variables `VITE_*` **en build time**. Si cambiaste el `.env` después de construir la imagen, tienes que reconstruir:
  ```bash
  docker-compose up -d --build frontend
  ```

### Puerto 1433 / 8080 / 5000 ya en uso
- Algo más en tu máquina está usando ese puerto (otro SQL Server, IIS, etc.). Cambia el mapeo de puertos en `docker-compose.yml` (lado izquierdo del `:`, no el derecho).

### `db-init` falla con "Login timeout expired"
- SQL Server todavía no está listo. El `healthcheck` de `db` debería resolverlo, pero a veces tarda más de lo configurado. Sube el `start_period` del healthcheck a `60s`.

### Cobertura de tests muy baja
- Pídele a Copilot/Claude tests específicos para los archivos con baja cobertura. El reportgenerator HTML te dice exactamente qué líneas no están cubiertas.

---

## 10. Lo que NO tienes que hacer

Para que no pierdas tiempo:

- ❌ No tienes que instalar .NET 10, Node, ni SQL Server localmente. Todo corre en Docker.
- ❌ No tienes que correr migraciones EF Core (la IA decidió usar scripts SQL planos para no tocar el schema legacy).
- ❌ No tienes que desplegar a ningún cloud. "Desplegar" en este reto = `docker-compose up` local.
- ❌ No tienes que crear usuarios / login. El reto explícitamente lo omite.
- ❌ No tienes que poblar `Latitude`/`Longitude` para los pedidos existentes. Los nuevos los obtendrán de Google al crearse; los viejos quedan NULL (es el comportamiento esperado).

---

## 11. Si vas a presentar la prueba

Recomendaciones de demo:

1. Levanta el proyecto **antes** de la reunión (no en vivo — el primer build tarda).
2. Ten una dirección real de prueba lista para escribir (ej. `1600 Amphitheatre Parkway, Mountain View, CA`).
3. Abre Swagger en una pestaña para mostrar la API REST limpia.
4. Abre el reporte de cobertura HTML en otra pestaña para mostrar el ≥80%.
5. Muestra que **un solo `docker-compose up`** levanta todo — es el bonus point más visible.
