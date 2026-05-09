FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Northwind.Domain/Northwind.Domain.csproj", "src/Northwind.Domain/"]
COPY ["src/Northwind.Application/Northwind.Application.csproj", "src/Northwind.Application/"]
COPY ["src/Northwind.Infrastructure/Northwind.Infrastructure.csproj", "src/Northwind.Infrastructure/"]
COPY ["src/Northwind.Api/Northwind.Api.csproj", "src/Northwind.Api/"]
RUN dotnet restore "src/Northwind.Api/Northwind.Api.csproj"

COPY . .
RUN dotnet publish "src/Northwind.Api/Northwind.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Northwind.Api.dll"]
