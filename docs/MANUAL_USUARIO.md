# Manual de usuario de NexAccess

Este manual es para quienes administran NexAccess desde Recursos Humanos, Seguridad o Administración. No hace falta conocer el código.

> **Importante:** esta versión de NexAccess es la **API** (el servidor). El portal web y la aplicación móvil todavía no existen. Mientras tanto, las operaciones se hacen desde la página **Swagger** de la API o con un cliente HTTP (Postman, Insomnia o `curl`). Cuando exista el portal, ofrecerá las mismas funciones con pantallas.

## 1. Objetivo y alcance

NexAccess centraliza dos procesos:

- **Control de acceso físico:** decide si una persona puede abrir una puerta (un *área*) al presentar su credencial NFC en un lector ESP32, y registra cada intento.
- **Asistencia:** registra las **entradas** y **salidas** laborales cuando el empleado presenta su credencial en un marcador, y permite que RRHH las corrija y genere reportes.

| Función | Estado |
|---|---|
| Inicio de sesión, usuarios y roles del sistema | Disponible |
| Empleados, áreas, roles y permisos por horario | Disponible |
| Credenciales NFC (alta, enrolamiento, desactivación, revocación y vencimiento) | Disponible |
| Dispositivos (alta, claves, activación y eventos) | Disponible |
| Verificación de acceso y marcación desde el ESP32 | Disponible en la API; **falta probar con hardware real** |
| Historial de accesos, asistencia, reporte diario, CSV y auditoría | Disponible |
| Portal web y aplicación móvil | **Pendiente** |
| Horarios laborales, tardanzas y horas extra | **No incluido** (no fueron definidos) |
| Notificaciones por correo | **No incluido** |

## 2. Requisitos y acceso

- La dirección del servidor, por ejemplo `https://nexaccess.empresa.local`. En desarrollo es `http://localhost:5131`.
- Un usuario y una contraseña entregados por un administrador.
- Un navegador actualizado para usar Swagger: `https://<servidor>/swagger`. En producción Swagger puede estar desactivado; en ese caso use el portal o un cliente HTTP.

## 3. Inicio de sesión

1. Abra Swagger y busque **Auth → POST /api/v1/auth/login**.
2. Pulse *Try it out* y escriba:
   ```json
   {"username": "su.usuario", "password": "su contraseña"}
   ```
3. Copie el valor de `accessToken` de la respuesta.
4. Pulse **Authorize** (arriba a la derecha), pegue el token en **Bearer** y confirme.
5. Para comprobar con qué usuario entró, use **GET /api/v1/auth/me**.

La sesión dura 60 minutos. Al vencer, las operaciones responden `401` y hay que volver a iniciar sesión. Si escribe mal la contraseña muchas veces seguidas, el sistema le pedirá esperar un minuto (`429`).

## 4. Roles y permisos

Hay dos tipos de rol:

- **Roles del sistema** (quién administra NexAccess):
  - `ADMIN`: todo, incluidos los usuarios y la auditoría.
  - `SEGURIDAD`: áreas, roles, permisos, credenciales, dispositivos, enrolamientos e historial de accesos.
  - `RRHH`: empleados y asistencia.
- **Roles de acceso** (qué puertas abre un empleado): `EMPLEADO`, `SEGURIDAD`, `RRHH`, `ADMINISTRADOR` y los que se creen. Se asignan a los empleados y se vinculan con áreas mediante *permisos de acceso*.

La tabla completa está en `SEGURIDAD.md`. Si intenta una operación que su rol no permite, verá `403`.

**Crear usuarios** (solo ADMIN): `POST /api/v1/users`
```json
{"username": "ana.rrhh", "password": "UnaClaveLarga2026", "role": "RRHH"}
```
Para desactivar un usuario: `PATCH /api/v1/users/{id}/status` con `{"isActive": false}`. El usuario pierde el acceso al instante. Para cambiar la contraseña: `PUT /api/v1/users/{id}/password`.

## 5. Alta y administración de empleados

Pueden hacerlo RRHH y ADMIN. Seguridad solo puede consultar.

- **Alta:** `POST /api/v1/employees`
  ```json
  {"documentNumber": "1234567", "firstName": "Ana", "lastName": "Pérez", "email": "ana@empresa.com", "phone": "0981 000000"}
  ```
  La cédula y el correo no pueden repetirse.
- **Buscar:** `GET /api/v1/employees?search=pérez&isActive=true&page=1&pageSize=50`
- **Modificar:** `PUT /api/v1/employees/{id}` con todos los datos.
- **Dar de baja o reactivar:** `PATCH /api/v1/employees/{id}/status` con `{"isActive": false}`. Un empleado inactivo no puede abrir puertas ni marcar, aunque tenga credenciales activas.
- **Asignar un rol de acceso** (SEGURIDAD o ADMIN): `POST /api/v1/employees/{id}/roles/{roleId}`. Para retirarlo: `DELETE` en la misma ruta.

## 6. Administración de áreas

Seguridad o ADMIN. Un área es un lugar físico (Recepción, Tesorería, Datacenter, etc.).

- **Crear:** `POST /api/v1/areas`
  ```json
  {"code": "TESORERIA", "name": "Tesorería", "isRestricted": true}
  ```
  - `isRestricted: true`: solo entran los empleados con un permiso para esa área.
  - `isRestricted: false`: entra cualquier empleado activo con una credencial válida.
- **Modificar o desactivar:** `PUT /api/v1/areas/{id}` (con `"isActive": false`, el área rechaza todos los accesos).

## 7. Credenciales NFC

Seguridad o ADMIN. Una credencial es una tarjeta o llavero (`FISICA`) o un teléfono (`DIGITAL`) asociado a un empleado. El identificador que lee el lector se guarda como `codigo_credencial`.

- **Registrar a mano** (si conoce el código): `POST /api/v1/credentials`
  ```json
  {"employeeId": 10, "codigo_credencial": "04A1B2C3D4", "type": "FISICA", "name": "Tarjeta principal", "expiresAt": "2027-12-31T23:59:59Z"}
  ```
  `expiresAt` es opcional. Una misma tarjeta no puede pertenecer a dos personas.
- **Registrar con el lector:** vea el punto 9 (enrolamiento). Es el método recomendado.
- **Consultar:** `GET /api/v1/credentials?employeeId=10`. El campo `status` puede ser `ACTIVA`, `INACTIVA`, `VENCIDA` o `REVOCADA`.
- **Suspender temporalmente:** `PATCH /api/v1/credentials/{id}/status` con `{"isActive": false}`. Se puede reactivar.
- **Revocar** (pérdida, robo o baja): `POST /api/v1/credentials/{id}/revoke` con `{"reason": "Extravío reportado"}`. **Es definitivo**: una credencial revocada no se puede reactivar. Si la persona recupera la tarjeta, hay que pedir una nueva.

## 8. Dispositivos

Seguridad o ADMIN.

- **Registrar:** `POST /api/v1/devices`
  ```json
  {"code": "PUERTA-RRHH-01", "name": "Puerta RRHH", "type": "ACCESO", "areaId": 3}
  ```
  Tipos: `ACCESO` (abre una puerta y requiere un área), `MARCACION` (solo asistencia) y `MIXTO` (ambas cosas; requiere un área).

  La respuesta incluye `deviceKey`. **Cópiela y entréguela de forma segura al técnico que configura el ESP32. No se vuelve a mostrar.**
- **Consultar:** `GET /api/v1/devices`. `lastConnectionAt` indica la última vez que el dispositivo se comunicó.
- **Desactivar:** `PATCH /api/v1/devices/{id}/status` con `{"isActive": false}`. El dispositivo deja de funcionar al instante.
- **Rotar la clave** (si se filtró o al reemplazar el equipo): `POST /api/v1/devices/{id}/rotate-key`. La clave anterior deja de valer y hay que cargar la nueva en el ESP32.
- **Ver sus eventos:** `GET /api/v1/devices/{id}/events`.

## 9. Enrolamiento de tarjetas

Permite registrar una tarjeta acercándola al lector, sin escribir el código.

1. Seguridad crea el enrolamiento: `POST /api/v1/enrollments`
   ```json
   {"employeeId": 10, "deviceId": 1, "credentialType": "FISICA", "credentialName": "Tarjeta principal", "expiresInMinutes": 5}
   ```
2. En unos segundos el lector recibe la orden `ENROLL_CARD` y queda esperando una tarjeta.
3. El empleado acerca la tarjeta al lector.
4. Consulte el resultado: `GET /api/v1/enrollments/{id}`. Los estados son:
   - `COMPLETADO`: la credencial quedó creada y activa (`credentialId`).
   - `VENCIDO`: nadie acercó una tarjeta dentro del plazo (5 minutos por defecto, 60 como máximo). Cree otro.
   - `CANCELADO`: se canceló con `POST /api/v1/enrollments/{id}/cancel`.

Si la tarjeta ya estaba registrada, el lector recibe un rechazo y el enrolamiento sigue pendiente hasta que venza o se cancele.

## 10. Permisos de acceso

Seguridad o ADMIN. Un permiso dice qué **rol** puede entrar a qué **área**, qué días y en qué horario.

- **Crear:** `POST /api/v1/access-permissions`
  ```json
  {"roleId": 1, "areaId": 3, "startTime": "07:00:00", "endTime": "19:00:00",
   "monday": true, "tuesday": true, "wednesday": true, "thursday": true, "friday": true, "saturday": false, "sunday": false}
  ```
  - Sin `startTime` ni `endTime`, el permiso vale todo el día.
  - Admite horarios nocturnos (por ejemplo, de 22:00 a 06:00).
  - Debe haber al menos un día habilitado. Solo puede haber un permiso por cada par rol-área.
- **Consultar:** `GET /api/v1/access-permissions?areaId=3`
- **Modificar:** `PUT /api/v1/access-permissions/{id}`
- **Revocar o reactivar:** `PATCH /api/v1/access-permissions/{id}/status` con `{"isActive": false}`.

Un empleado entra a un área restringida si **alguno de sus roles activos** tiene un permiso activo para el día y la hora actuales (hora de Paraguay, configurable).

## 11. Historial de accesos

Seguridad o ADMIN.

- `GET /api/v1/access-events?employeeId=10&result=RECHAZADO&from=2026-10-01T00:00:00-03:00&to=2026-10-31T23:59:59-03:00`
- Cada evento muestra fecha y hora (UTC), resultado (`AUTORIZADO` o `RECHAZADO`), motivo, empleado, dispositivo, área y los últimos 4 caracteres de la credencial.
- Motivos de rechazo frecuentes: `CREDENTIAL_NOT_FOUND` (tarjeta desconocida), `NO_PERMISSION`, `OUTSIDE_SCHEDULE`, `CREDENTIAL_REVOKED`, `EMPLOYEE_INACTIVE`. La lista completa está en `API_ESP32.md`.

## 12. Asistencia

RRHH o ADMIN.

- **Cómo se registra:** el empleado presenta su credencial en un dispositivo `MARCACION` o `MIXTO`. La primera marcación del día es una **entrada**; la siguiente, una **salida**, y así sucesivamente. Si la presenta dos veces en menos de 60 segundos, la segunda no cuenta.
- **Consultar:** `GET /api/v1/attendance?employeeId=10&from=...&to=...`
- **Corregir u olvidos de tarjeta:** `POST /api/v1/attendance`
  ```json
  {"employeeId": 10, "type": "ENTRADA", "occurredAt": "2026-10-17T08:05:00-03:00", "note": "Olvidó la tarjeta"}
  ```
  El motivo (`note`) es obligatorio. La marcación queda como `MANUAL`, con el usuario que la registró, y se audita. No se permiten marcaciones en el futuro ni a menos de 60 segundos de otra del mismo empleado.

## 13. Reportes y exportación

- **Reporte diario de asistencia:** `GET /api/v1/attendance/report?from=...&to=...&employeeId=` (RRHH o ADMIN). Muestra por empleado y por día la primera entrada, la última salida, la cantidad de marcaciones y si el día quedó **incompleto** (entradas y salidas desparejas).
- **Exportar marcaciones a CSV:** `GET /api/v1/attendance/export?from=...&to=...` (RRHH o ADMIN).
- **Exportar eventos de acceso a CSV:** `GET /api/v1/access-events/export?from=...&to=...` (Seguridad o ADMIN).
- **Auditoría:** `GET /api/v1/audit` (ADMIN). Muestra quién hizo qué y cuándo.

Los CSV usan UTF-8 con BOM y comas como separador, y abren directamente en Excel. Las fechas del CSV están en hora local. Las exportaciones exigen `from` y `to` y abarcan como máximo 366 días.

## 14. Errores frecuentes

| Respuesta | Causa probable | Qué hacer |
|---|---|---|
| `401 UNAUTHORIZED` | Sesión vencida, usuario desactivado o token mal pegado | Volver a iniciar sesión |
| `401 INVALID_CREDENTIALS` | Usuario o contraseña incorrectos | Revisar los datos o pedir un cambio de contraseña al ADMIN |
| `403 FORBIDDEN` | Su rol no permite la operación | Pedir la operación a quien corresponda |
| `400 VALIDATION_ERROR` | Faltan datos o tienen mal formato; `errors` indica cuáles | Corregir y reenviar |
| `404 NOT_FOUND` | El identificador no existe | Revisar el `id` |
| `409 ..._DUPLICATE` / `CREDENTIAL_ALREADY_REGISTERED` | Cédula, correo, código o tarjeta repetidos | Buscar el registro existente |
| `422` | Una regla de negocio lo impide (empleado inactivo, credencial revocada, etc.) | Leer `detail` |
| `429` | Demasiados intentos de inicio de sesión | Esperar un minuto |
| `503 DATABASE_UNAVAILABLE` | El servidor no llega a la base de datos | Avisar a soporte técnico |

Problemas en las puertas:

- **El lector siempre rechaza:** revise en el historial el motivo del rechazo. Si no aparecen eventos, el dispositivo no se está comunicando (revise `lastConnectionAt`, la red, la clave o si está desactivado).
- **El enrolamiento no avanza:** compruebe que el dispositivo esté activo y que el enrolamiento no haya vencido.
- **Sin conexión:** la puerta **permanece cerrada** por seguridad. Use el procedimiento manual que defina Seguridad física.

## 15. Recomendaciones de seguridad

- No comparta su usuario. Cada persona debe tener el suyo.
- Use contraseñas largas y únicas (más de 12 caracteres).
- Revoque de inmediato las credenciales perdidas o robadas y desactive a los empleados que dejan la organización.
- Rote la clave de un dispositivo si se filtró o si el equipo fue manipulado.
- Revise periódicamente los rechazos y la auditoría.
- Para áreas críticas (datacenter, tesorería), Seguridad debe evaluar tarjetas con autenticación criptográfica: el UID de una tarjeta común se puede copiar.
- Las exportaciones CSV contienen datos personales: guárdelas solo en lugares autorizados y bórrelas cuando ya no las necesite.
