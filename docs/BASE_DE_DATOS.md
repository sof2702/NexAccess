# Base de datos

## Versiones

| Entorno | Versión | Estado |
|---|---|---|
| Indicada por el proyecto | PostgreSQL 9.3.5 de 32 bits | No disponible en este equipo. **Conexión no verificada.** |
| Desarrollo y pruebas (este equipo) | PostgreSQL 18.3 de 64 bits (servicio `postgresql-x64-18`) | Verificada: esquema aplicado y 27 pruebas de integración ejecutadas |

Bases creadas en el servidor local (no se modificó ninguna base existente):

- `nexaccess_dev`: desarrollo y pruebas manuales.
- `nexaccess_test`: pruebas de integración automatizadas.

## Compatibilidad con PostgreSQL 9.3

Los scripts y el mapeo de EF Core evitan funciones que no existen en 9.3:

| Función | Disponible desde | Alternativa usada |
|---|---|---|
| `gen_random_uuid()` / `uuid_generate_v4()` | pgcrypto / 13 | Sin UUID; claves `BIGSERIAL` |
| Columnas `IDENTITY` | 10 | `BIGSERIAL` y `UseSerialColumn()` en EF Core |
| `INSERT ... ON CONFLICT` | 9.5 | `INSERT ... SELECT ... WHERE NOT EXISTS` |
| `CREATE INDEX IF NOT EXISTS` | 9.5 | Bloque `DO` que consulta `pg_class` |
| `jsonb` | 9.4 | No se usa |

EF Core se configura con `SetPostgresVersion(9.3)` (clave `Database:PostgresVersion`) para que no genere SQL de versiones más nuevas.

**Advertencia:** PostgreSQL 9.3 dejó de recibir soporte y parches de seguridad en noviembre de 2018. Npgsql 10 se prueba oficialmente solo contra versiones con soporte vigente. Usar 9.3.5 en producción tiene riesgos de seguridad y compatibilidad. Se recomienda migrar a una versión con soporte (por ejemplo, 16, 17 o 18) antes del despliegue. Si hay que usar 9.3.5, primero ejecuten las pruebas de integración contra una copia de esa instancia, con `ConnectionStrings:NexAccessTest` apuntando a una base `*_test`.

## Scripts

Ubicación: `src/NexAccess.Infrastructure/Persistence/Scripts/`

| Archivo | Contenido |
|---|---|
| `001_esquema_inicial.sql` | Tablas, restricciones e índices. Usa `CREATE TABLE IF NOT EXISTS`. |
| `002_datos_iniciales.sql` | Roles de acceso físico `EMPLEADO`, `SEGURIDAD`, `RRHH` y `ADMINISTRADOR`. |

Ambos son **idempotentes y no destructivos**: no contienen `DROP`, `TRUNCATE` ni `DELETE`, y pueden ejecutarse varias veces. La API **no** aplica migraciones automáticamente.

```powershell
psql -h <host> -U <usuario> -d <base> -v ON_ERROR_STOP=1 -f 001_esquema_inicial.sql
psql -h <host> -U <usuario> -d <base> -v ON_ERROR_STOP=1 -f 002_datos_iniciales.sql
```

## Diferencias con `NexAccess_docs/SQL/sql_inicial.sql`

| Documento original | Implementación | Motivo |
|---|---|---|
| Columnas `uuid UUID DEFAULT gen_random_uuid()` y `CREATE EXTENSION pgcrypto` | Eliminadas | No disponibles en 9.3 y no se usan UUID como identificadores |
| `credencial.uid` | `credencial.codigo_credencial` | Requisito del proyecto |
| `dispositivo` sin clave | `clave_hash` (SHA-256, única) y `clave_rotada_at` | Autenticación `X-DEVICE-KEY` y rotación |
| `marcacion.id_dispositivo NOT NULL` | Admite `NULL` solo si `origen = 'MANUAL'` | Correcciones manuales de RRHH |
| — | `marcacion.origen`, `observacion`, `id_usuario_registro` | Trazabilidad de las correcciones |
| — | `evento_acceso.codigo_credencial` | Registrar el código presentado aunque no exista |
| — | Tablas `enrolamiento`, `comando_dispositivo` y `auditoria` | Enrolamiento por ESP32 y auditoría |

## Tablas

| Tabla | Descripción | Claves y restricciones principales |
|---|---|---|
| `empleado` | Personal | `uq_empleado_cedula`, `uq_empleado_email` |
| `rol` | Roles de acceso físico | `uq_rol_codigo` |
| `empleado_rol` | Roles asignados a cada empleado (N:M) | PK (`id_empleado`, `id_rol`) |
| `area` | Áreas físicas; `restringida = FALSE` permite el ingreso a todo empleado activo | `uq_area_codigo` |
| `permiso_acceso` | Rol → área con días y franja horaria (`hora_desde`/`hora_hasta`; `NULL` = todo el día; admite franjas nocturnas) | `uq_permiso_rol_area` |
| `credencial` | Credencial NFC `FISICA` o `DIGITAL` de un empleado | `uq_credencial_codigo` (único global), `ck_credencial_tipo` |
| `dispositivo` | ESP32 `ACCESO`, `MARCACION` o `MIXTO` | `uq_dispositivo_codigo`, `uq_dispositivo_clave_hash` |
| `usuario` | Usuarios del portal (`ADMIN`, `SEGURIDAD`, `RRHH`) | `uq_usuario_username`, `ck_usuario_rol_sistema` |
| `evento_acceso` | Todos los intentos de acceso, autorizados y rechazados | `ck_evento_resultado` |
| `marcacion` | Entradas y salidas de asistencia | `ck_marcacion_tipo`, `ck_marcacion_origen`, `ck_marcacion_origen_dispositivo` |
| `enrolamiento` | Solicitud para leer una tarjeta en un dispositivo | `ck_enrolamiento_estado` (`PENDIENTE`, `COMPLETADO`, `VENCIDO`, `CANCELADO`) |
| `comando_dispositivo` | Cola de comandos por dispositivo | `ck_comando_tipo` (`ENROLL_CARD`), `ck_comando_estado` |
| `auditoria` | Acciones administrativas y de dispositivos | FK opcionales a `usuario` y `dispositivo` |

Índices adicionales: eventos por empleado, dispositivo y fecha; marcaciones por empleado y fecha; credenciales activas por código; permisos por área; enrolamientos y comandos por dispositivo y estado; auditoría por fecha.

## Relaciones

```
empleado 1─N credencial          empleado N─M rol (empleado_rol)
rol 1─N permiso_acceso N─1 area  dispositivo N─1 area
evento_acceso N─1 dispositivo / empleado / credencial / area
marcacion N─1 empleado / credencial / dispositivo / usuario
enrolamiento N─1 empleado / dispositivo / credencial / usuario
comando_dispositivo N─1 dispositivo / enrolamiento
usuario N─1 empleado (opcional)   auditoria N─1 usuario / dispositivo
```

## Configuración de la conexión

La cadena se lee de `ConnectionStrings:NexAccess`. Nunca va en `appsettings.json`:

- Desarrollo: `dotnet user-secrets set "ConnectionStrings:NexAccess" "..." --project src/NexAccess.API`.
- Servidor: variable de entorno `ConnectionStrings__NexAccess`.

Usuario recomendado para producción: uno dedicado con `SELECT`, `INSERT` y `UPDATE` sobre las tablas y `USAGE` sobre las secuencias, sin `DROP` ni superusuario. Las pruebas de este equipo se hicieron con `postgres` porque es el único usuario disponible.
