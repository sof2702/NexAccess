# Instalación y despliegue

## Requisitos

- .NET SDK 10.0 (verificado con 10.0.301; el equipo también tiene los SDK 8 y 9).
- PostgreSQL. Verificado con 18.3; la versión 9.3.5 no se verificó (ver `BASE_DE_DATOS.md`).
- `psql` (incluido en PostgreSQL) o pgAdmin para aplicar los scripts.
- Git.

## 1. Preparar la base de datos

Con pgAdmin: crear la base, abrir el *Query Tool* sobre ella y ejecutar los dos scripts, en orden. Con psql:

```powershell
$env:PGCLIENTENCODING = "UTF8"
psql -h localhost -U postgres -c "CREATE DATABASE nexaccess_dev ENCODING 'UTF8' TEMPLATE template0"
psql -h localhost -U postgres -d nexaccess_dev -v ON_ERROR_STOP=1 -f src/NexAccess.Infrastructure/Persistence/Scripts/001_esquema_inicial.sql
psql -h localhost -U postgres -d nexaccess_dev -v ON_ERROR_STOP=1 -f src/NexAccess.Infrastructure/Persistence/Scripts/002_datos_iniciales.sql
```

Los scripts pueden ejecutarse varias veces sin perder datos.

## 2. Configurar los secretos de desarrollo

```powershell
$p = "src/NexAccess.API"
dotnet user-secrets set "ConnectionStrings:NexAccess" "Host=localhost;Port=5432;Database=nexaccess_dev;Username=postgres;Password=<clave>" --project $p
dotnet user-secrets set "Jwt:Key" "<al menos 32 caracteres aleatorios>" --project $p
dotnet user-secrets set "Bootstrap:AdminUsername" "admin" --project $p
dotnet user-secrets set "Bootstrap:AdminPassword" "<al menos 12 caracteres>" --project $p
dotnet user-secrets list --project $p
```

Para generar una clave JWT en PowerShell:

```powershell
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
```

## 3. Compilar, probar y ejecutar

```powershell
dotnet restore
dotnet build
dotnet test tests/NexAccess.UnitTests
dotnet run --project src/NexAccess.API --launch-profile http
```

- API: `http://localhost:5131`
- Swagger UI: `http://localhost:5131/swagger`
- Estado: `GET http://localhost:5131/api/v1/health`

## 4. Pruebas de integración

```powershell
dotnet user-secrets set "ConnectionStrings:NexAccessTest" "Host=localhost;Database=nexaccess_test;Username=postgres;Password=<clave>" --id nexaccess-integration-tests
dotnet test tests/NexAccess.IntegrationTests
```

En CI se puede usar la variable `ConnectionStrings__NexAccessTest`. El nombre de la base debe contener `test`.

## Configuración (`appsettings.json`)

| Clave | Valor por defecto | Descripción |
|---|---|---|
| `Database:PostgresVersion` | `9.3` | Versión que EF Core toma como referencia para generar SQL |
| `Jwt:Issuer` / `Jwt:Audience` | `NexAccess` / `NexAccess.Admin` | Validación del token |
| `Jwt:ExpirationMinutes` | `60` | Vigencia del token (entre 1 y 1440) |
| `Access:OpenDurationMs` | `3000` | Tiempo de apertura que se informa al ESP32 |
| `Access:TimeZoneId` | `America/Asuncion` | Zona horaria de los permisos y los reportes |
| `Attendance:DuplicateWindowSeconds` | `60` | Ventana anti-duplicados de marcaciones |
| `Attendance:MaxReportDays` | `366` | Rango máximo de reportes y exportaciones |
| `Enrollment:DefaultExpirationMinutes` | `5` | Vigencia por defecto de un enrolamiento |
| `Enrollment:MaxExpirationMinutes` | `60` | Vigencia máxima permitida |
| `RateLimiting:LoginPermitsPerMinute` | `10` | Intentos de login por IP y minuto |
| `OpenApi:Enabled` | `false` (`true` en Development) | Expone `/openapi/v1.json` y `/swagger` |

## Despliegue

```powershell
dotnet publish src/NexAccess.API -c Release -o publish
```

Variables de entorno mínimas en el servidor:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443
ConnectionStrings__NexAccess=Host=...;Database=nexaccess;Username=nexaccess_app;Password=...
Jwt__Key=<secreto>
Bootstrap__AdminUsername=admin          (solo en el primer arranque)
Bootstrap__AdminPassword=<temporal>     (borrar después del primer arranque)
```

Lista de verificación:

1. Aplicar los scripts en la base de producción con un usuario con permisos de DDL. La aplicación debe conectarse con un usuario sin DDL.
2. Configurar HTTPS con un certificado válido (Kestrel o un proxy inverso). Si hay proxy, configurar los encabezados reenviados.
3. Mantener `OpenApi:Enabled=false`, salvo en redes internas controladas.
4. Hacer backups periódicos de la base. Las tablas `evento_acceso`, `marcacion` y `auditoria` crecen de forma continua.
5. Registrar los dispositivos y cargar sus claves (ver `API_ESP32.md`).
