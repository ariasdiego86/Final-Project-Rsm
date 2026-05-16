# Ejecutar el proyecto en MacBook Air M1

## Por qué ya no es solo "clonar y ejecutar"

El proyecto está completamente dockerizado, **pero** SQL Server no tiene imagen nativa para ARM64 (chip M1).
Se resolvió añadiendo `platform: linux/amd64` en el `docker-compose.yml` para los servicios de base de datos.
Esto hace que Docker use emulación Rosetta 2 para SQL Server — transparente y sin impacto visible en la presentación.

El resto de servicios (.NET 10, Node, Nginx) sí tienen imágenes ARM64 nativas.

---

## Requisitos previos (instalar en la Mac)

- **Docker Desktop para Mac (Apple Silicon)**
  Descargar desde: https://www.docker.com/products/docker-desktop/
  Asegurarse de instalar la versión **Apple Silicon**, no la Intel.

- **Git** (normalmente ya viene en macOS)
  Verificar con: `git --version`

---

## Paso 1 — Habilitar Rosetta 2 en Docker Desktop

● No viene activo por defecto en la mayoría de versiones. Hay que activarlo manualmente:

  1. Abre Docker Desktop
  2. Clic en el engranaje (⚙️) arriba a la derecha → Settings
  3. En la sección General, busca esta opción:

  ☐ Use Rosetta for x86/amd64 emulation on Apple Silicon

  4. Actívala (checkbox)
  5. Clic en Apply & Restart

  ---
  Dato importante: si no está activada, Docker Desktop usa QEMU como alternativa de emulación (que también funciona, pero es significativamente más lento).
   Con Rosetta 2 activado, SQL Server arranca mucho más rápido.

  Para la presentación, definitivamente conviene tenerlo activado.

  
1. Abrir Docker Desktop
2. Ir a **Settings → General**
3. Activar **"Use Rosetta for x86_64/amd64 emulation on Apple Silicon"**
4. Clic en **Apply & Restart**

> Esto es necesario para que SQL Server funcione bajo emulación.

---

## Paso 2 — Clonar el repositorio

```bash
git clone https://github.com/ariasdiego86/final_rsm.git
cd final_rsm
```

---

## Paso 3 — Copiar el archivo `.env`

El `.env` **no está en el repositorio** (está en `.gitignore`). Debes transferirlo manualmente desde la PC Windows.

Opciones para transferirlo:
- AirDrop (desde Windows necesitas un intermediario, ej. iCloud Drive o Google Drive)
- USB / pendrive
- Correo o WhatsApp a ti mismo

El archivo `.env` debe quedar en la **raíz del proyecto** (junto al `docker-compose.yml`).

Contenido que debe tener (reemplaza los valores si es necesario):

```env
DB_SA_PASSWORD=TuPasswordSeguro123!
DB_NAME=Northwind
GOOGLE_MAPS_API_KEY=tu_clave_real_aqui
VITE_GOOGLE_MAPS_API_KEY=tu_clave_real_aqui
ASPNETCORE_ENVIRONMENT=Development
CORS_ALLOWED_ORIGINS=http://localhost:5173
```

---

## Paso 4 — Verificar recursos de Docker

SQL Server necesita al menos **2 GB de RAM** asignados a Docker.

1. Docker Desktop → **Settings → Resources**
2. Asegurarse de que **Memory** esté en **4 GB o más** (recomendado)
3. Clic en **Apply & Restart** si se hizo algún cambio

---

## Paso 5 — Levantar el proyecto

```bash
docker compose up --build
```

La primera vez tarda varios minutos porque:
- Descarga las imágenes base (SQL Server ~1.5 GB, .NET SDK, Node, Nginx)
- Compila el backend (.NET)
- Compila el frontend (React/Vite)
- Inicializa la base de datos con los scripts SQL

Esperar hasta ver en consola algo como:

```
northwind-backend   | Now listening on: http://[::]:8080
northwind-frontend  | ...start worker process
```

---

## Paso 6 — Acceder a la aplicación

| Servicio | URL |
|---|---|
| Frontend (aplicación) | http://localhost:5173 |
| Backend (API) | http://localhost:8080 |

---

## Para detener el proyecto

```bash
# Detener sin borrar datos
docker compose down

# Detener Y borrar la base de datos (volumen)
docker compose down -v
```

---

## Solución a problemas comunes

### "no matching manifest for linux/arm64"
Ya está resuelto con `platform: linux/amd64` en el `docker-compose.yml`.
Si aparece igual, verificar que Rosetta 2 esté habilitado (Paso 1).

### SQL Server no arranca / healthcheck falla
- Verificar que Docker tiene al menos 2 GB de RAM asignados (Paso 4)
- Esperar más tiempo — SQL Server puede tardar hasta 60 segundos en el primer arranque
- Reintentar: `docker compose down -v && docker compose up --build`

### Puerto ya en uso (1433, 8080, 5173)
Algún proceso local usa ese puerto. Verificar con:
```bash
lsof -i :1433
lsof -i :8080
lsof -i :5173
```
Y detenerlo, o reiniciar la Mac antes de ejecutar el proyecto.

### El mapa de Google Maps no carga
La API key puede tener restricciones de dominio configuradas en Google Cloud Console.
Verificar que `http://localhost` esté autorizado, o usar una clave sin restricciones para la presentación.
