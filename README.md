# Northwind Order Management System

Full-stack order management application built on the Northwind sample database. Provides a paginated order list, create/edit form with Google Maps address validation, charts, and Excel/PDF export.

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Vue 3, TypeScript, Quasar, Pinia, Chart.js, Vite |
| Backend | ASP.NET Core 10, C#, Clean Architecture |
| ORM | Entity Framework Core 10 + SQL Server |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| Geolocation | Google Maps Address Validation API |
| Tests (backend) | xUnit, Moq, FluentAssertions, EF Core InMemory |
| Tests (frontend) | Vitest, Vue Test Utils |
| Infrastructure | Docker, Docker Compose |

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose)
- A **Google Maps API key** with the following APIs enabled:
  - Address Validation API (backend)
  - Maps JavaScript API + Marker library (frontend)

## Setup

1. **Clone the repository**

```bash
git clone <repo-url>
cd final_rsm
```

2. **Create the environment file**

```bash
cp .env.example .env
```

Edit `.env` and fill in your values:

```env
DB_SA_PASSWORD=YourStrongPassword123!
DB_NAME=Northwind
GOOGLE_MAPS_API_KEY=AIzaSy_YOUR_BACKEND_KEY
VITE_GOOGLE_MAPS_API_KEY=AIzaSy_YOUR_FRONTEND_KEY
ASPNETCORE_ENVIRONMENT=Development
CORS_ALLOWED_ORIGINS=http://localhost:5173
```

> Both `GOOGLE_MAPS_API_KEY` and `VITE_GOOGLE_MAPS_API_KEY` can point to the same key.

3. **Start the application**

```bash
docker-compose up -d --build
```

This command:
- Starts SQL Server and waits for it to be healthy
- Runs the database initialization scripts (creates the Northwind schema and seeds data)
- Builds and starts the ASP.NET Core backend on port `8080`
- Builds and starts the Vue frontend on port `5173`

4. **Open the app**

- Frontend: http://localhost:5173
- Backend API: http://localhost:8080/api
- Swagger: http://localhost:8080/swagger

## Folder Structure

```
final_rsm/
├── backend/
│   ├── src/
│   │   ├── Northwind.Api/            # Controllers, Program.cs, Dockerfile
│   │   ├── Northwind.Application/    # Services, DTOs, Interfaces, Validators, AutoMapper profiles
│   │   ├── Northwind.Domain/         # Entities, Exceptions
│   │   └── Northwind.Infrastructure/ # EF Core DbContext, Repositories, External services
│   └── tests/
│       └── Northwind.Tests/
│           ├── Api/                  # Integration tests (HTTP end-to-end)
│           ├── Application/          # Unit tests for services and validators
│           ├── Infrastructure/       # Repository and external service tests
│           └── Helpers/              # WebApplicationFactory, seed data
├── database/
│   ├── 00-create-database.sql
│   ├── script-northwindDataBase.sql  # Original Northwind schema + data
│   └── 02-add-geo-columns.sql        # Adds Latitude/Longitude to Orders
├── frontend/
│   ├── src/
│   │   ├── components/               # OrderList, OrderForm, AddressMap, Charts
│   │   ├── composables/              # useAddressValidation, useGoogleMaps
│   │   ├── services/                 # Axios wrappers
│   │   ├── stores/                   # Pinia stores
│   │   ├── views/                    # Route-level pages
│   │   └── tests/                    # Vitest spec files + setup.ts
│   ├── Dockerfile
│   └── vite.config.ts
├── docker-compose.yml
└── .env.example
```

## Running Tests

### Backend

Tests require a Linux environment or Docker because Windows Smart App Control blocks locally compiled unsigned DLLs. Run them inside a container:

```bash
docker run --rm \
  -v "$(pwd)/backend:/app" \
  -w /app \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/Northwind.Tests --no-build -v minimal
```

Or on Linux/macOS directly:

```bash
cd backend
dotnet test tests/Northwind.Tests
```

Expected result: **57 tests, 0 failed**.

### Frontend

```bash
cd frontend
npm install
npm test
```

Expected result: **8 tests, 0 failed** across 2 spec files.

### Coverage report (frontend)

```bash
npm run test:coverage
```

## Architecture

The backend follows **Clean Architecture** with four layers:

```
Api → Application → Domain
         ↑
   Infrastructure
```

- **Domain**: Pure entities and exceptions. No dependencies.
- **Application**: Business logic, interfaces, DTOs, FluentValidation validators, AutoMapper profiles. Depends only on Domain.
- **Infrastructure**: EF Core, repository implementations, Google Maps client. Depends on Application and Domain.
- **Api**: ASP.NET Core controllers, DI wiring, middleware. Depends on Application.

Key patterns used:
- Repository pattern with `IOrderRepository`
- Service layer (`OrderService`) coordinating repository + geo + PDF/Excel
- `WebApplicationFactory` for integration tests with EF Core InMemory and a stub `IGeoLocationService`

## Features

- Paginated order list with column sorting and date/region filters
- Create and edit orders with line-item management
- Address validation against Google Maps Address Validation API
- Interactive map (Google Maps JS) rendered after address validation
- Orders per time chart (bar) and shipments by region chart (pie)
- Export to Excel (ClosedXML) and individual PDF (QuestPDF)

## Troubleshooting

| Problem | Solution |
|---|---|
| `docker-compose up` fails with port 1433 already in use | Stop any local SQL Server instance or change the port mapping in `docker-compose.yml` |
| `GOOGLE_MAPS_API_KEY` not set error on backend start | Make sure `.env` exists and the key is not empty |
| Map not rendering in frontend | Verify the Maps JavaScript API and Marker library are enabled in Google Cloud Console for `VITE_GOOGLE_MAPS_API_KEY` |
| Backend tests blocked on Windows (Event ID 3118) | Run tests via Docker (see above); Windows Smart App Control blocks unsigned locally-compiled DLLs |
| `SA_PASSWORD` validation error from SQL Server | Password must meet complexity requirements: uppercase, lowercase, digit, symbol, minimum 8 characters |
| Frontend build fails with `VITE_API_BASE_URL` missing | The value is hardcoded to `http://localhost:8080` as a build arg in `docker-compose.yml`; override there if needed |
