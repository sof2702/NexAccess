# API para dispositivos ESP32

Todas las rutas usan el prefijo `/api/v1` y JSON UTF-8. Cada dispositivo se autentica con el encabezado:

```
X-DEVICE-KEY: nxd_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

La clave se muestra **una sola vez**, al registrar el dispositivo (`POST /api/v1/devices`) o al rotarla (`POST /api/v1/devices/{id}/rotate-key`). La base solo guarda su hash SHA-256. Una clave inválida, una clave rotada o un dispositivo desactivado devuelven `401` con `code = DEVICE_UNAUTHORIZED`.

Los dispositivos **solo** pueden usar los tres endpoints de este documento. Cualquier endpoint administrativo responde `401` a una solicitud que traiga solo `X-DEVICE-KEY`.

## Regla de seguridad del firmware

**La puerta se abre únicamente si la respuesta es HTTP 200, el JSON es válido, `authorized == true` y `action == "OPEN"`.** Ante un timeout, un error de red o de TLS, cualquier código distinto de 200 o un JSON inválido o incompleto, el ESP32 **no abre** y muestra la señal de rechazo.

Recomendaciones:

- Timeout total de 3 a 5 s por solicitud.
- HTTPS con validación del certificado del servidor (CA embebida o pinning). No desactivar la validación.
- No imprimir la clave en el monitor serie ni en los logs.
- Abrir el relé durante `durationMs` milisegundos.

## 1. Consulta de comandos

```
GET /api/v1/devices/{codigo}/commands
X-DEVICE-KEY: <clave>
```

`{codigo}` debe ser el código del dispositivo dueño de la clave (sin distinguir mayúsculas). Si no lo es, la respuesta es `403 DEVICE_MISMATCH`.

Sin comandos:

```json
{"command":"NONE"}
```

Con un enrolamiento pendiente:

```json
{"command":"ENROLL_CARD","enrollmentId":15,"expiresAt":"2026-10-18T02:50:17.8986Z"}
```

- Los comandos tienen los estados `PENDIENTE` → `ENTREGADO` → `COMPLETADO`, o bien `VENCIDO` o `CANCELADO`.
- Un comando entregado se repite en cada consulta hasta que se complete, venza o se cancele. Así el dispositivo puede reintentar después de un reinicio.
- Los comandos vencidos se cierran automáticamente (y su enrolamiento pasa a `VENCIDO`).
- Intervalo de consulta sugerido: entre 2 y 5 s mientras el dispositivo esté inactivo.

## 2. Envío de la tarjeta leída (enrolamiento)

```
POST /api/v1/enrollments/{enrollmentId}/card
X-DEVICE-KEY: <clave>
Content-Type: application/json

{"codigo_credencial":"04A1B2C3D4"}
```

El campo se llama `codigo_credencial`. Si el lector entrega el UID con separadores (`04:A1:B2:C3:D4` o `04 a1 b2`), la API quita los espacios, `:` y `-` y lo pasa a mayúsculas. Después de normalizarlo, el código debe tener entre 4 y 150 caracteres alfanuméricos.

| Respuesta | Significado |
|---|---|
| `200 {"enrollmentId":15,"status":"COMPLETADO","credentialId":42}` | Credencial creada y activa |
| `400 VALIDATION_ERROR` | Código vacío o con formato inválido |
| `403 ENROLLMENT_DEVICE_MISMATCH` | El enrolamiento pertenece a otro dispositivo |
| `404 NOT_FOUND` | El enrolamiento no existe |
| `409 ENROLLMENT_EXPIRED` | Venció; hay que pedir uno nuevo desde administración |
| `409 ENROLLMENT_ALREADY_COMPLETED` / `ENROLLMENT_CANCELLED` | Ya está cerrado |
| `409 CREDENTIAL_ALREADY_REGISTERED` | Esa tarjeta ya está registrada (en cualquier empleado) |
| `422 EMPLOYEE_INACTIVE` | El empleado fue desactivado después de crear el enrolamiento |

## 3. Verificación de acceso

```
POST /api/v1/access/verify
X-DEVICE-KEY: <clave>
Content-Type: application/json

{"deviceCode":"PUERTA-RRHH-01","credential":"04A1B2C3D4"}
```

Autorizado en un dispositivo `ACCESO`:

```json
{"authorized":true,"action":"OPEN","reason":"AUTHORIZED","durationMs":3000}
```

Autorizado en un dispositivo `MIXTO` (abre y marca asistencia):

```json
{"authorized":true,"action":"OPEN","reason":"AUTHORIZED","durationMs":3000,
 "attendance":{"type":"ENTRADA","duplicate":false,"occurredAt":"2026-10-18T02:45:18.24Z"}}
```

Autorizado en un dispositivo `MARCACION` (solo asistencia, no abre):

```json
{"authorized":true,"action":"MARK","reason":"AUTHORIZED",
 "attendance":{"type":"SALIDA","duplicate":false,"occurredAt":"2026-10-18T21:02:10Z"}}
```

Rechazado:

```json
{"authorized":false,"action":"DENY","reason":"CREDENTIAL_INACTIVE"}
```

Todo intento queda registrado en `evento_acceso`. Un rechazo de negocio responde **HTTP 200** con `authorized=false`. Los errores de autenticación (`401`) o de formato (`400`) también se tratan como rechazo en el firmware.

### Motivos estables

| `reason` | Causa |
|---|---|
| `AUTHORIZED` | Acceso permitido |
| `DEVICE_MISMATCH` | `deviceCode` no coincide con el dispositivo de la clave |
| `INVALID_REQUEST` | Credencial vacía o con formato inválido |
| `CREDENTIAL_NOT_FOUND` | La credencial no está registrada |
| `CREDENTIAL_REVOKED` | La credencial fue revocada |
| `CREDENTIAL_INACTIVE` | La credencial está desactivada |
| `CREDENTIAL_EXPIRED` | La credencial venció |
| `EMPLOYEE_INACTIVE` | El empleado está inactivo |
| `DEVICE_WITHOUT_AREA` | El dispositivo de acceso no tiene área asignada |
| `AREA_INACTIVE` | El área está inactiva |
| `NO_PERMISSION` | Ningún rol activo del empleado tiene un permiso activo para el área |
| `OUTSIDE_SCHEDULE` | Hay permiso, pero no para el día o la hora actuales (zona `Access:TimeZoneId`) |

## Reglas de asistencia

- La primera marcación del día (en hora local) es `ENTRADA`. Si la anterior fue una `ENTRADA` del mismo día, la siguiente es `SALIDA`. Después de una `SALIDA` viene otra `ENTRADA`.
- Si el mismo empleado vuelve a presentar la credencial dentro de `Attendance:DuplicateWindowSeconds` (60 s por defecto), no se crea otra marcación y la respuesta trae `"duplicate": true`.
- No hay horarios laborales ni reglas de tardanza configuradas, porque la documentación del proyecto no las define.

## Ejemplos con curl

```bash
curl -s https://servidor/api/v1/devices/PUERTA-RRHH-01/commands -H "X-DEVICE-KEY: $KEY"
curl -s -X POST https://servidor/api/v1/enrollments/15/card -H "X-DEVICE-KEY: $KEY" \
     -H "Content-Type: application/json" -d '{"codigo_credencial":"04A1B2C3D4"}'
curl -s -X POST https://servidor/api/v1/access/verify -H "X-DEVICE-KEY: $KEY" \
     -H "Content-Type: application/json" -d '{"deviceCode":"PUERTA-RRHH-01","credential":"04A1B2C3D4"}'
```

## Pasos pendientes para conectar el hardware

El firmware **no** está en este repositorio y **no se probó con hardware físico**.

1. Registrar cada ESP32 en `POST /api/v1/devices` (tipo, área y código) y guardar la clave en el almacenamiento seguro del dispositivo (NVS cifrado).
2. Publicar la API con HTTPS y un certificado válido en la red de los dispositivos, y cargar la CA en el firmware.
3. Implementar en el firmware el ciclo: leer UID → `POST /access/verify` → actuar según la regla de seguridad.
4. Implementar la consulta periódica de `/commands` y el modo de enrolamiento (señal visual "acerque la tarjeta" → `POST /enrollments/{id}/card`).
5. Definir con Seguridad física el comportamiento ante cortes de red (por diseño, la puerta permanece cerrada).
6. Comprobar la compatibilidad de las credenciales digitales (HCE del teléfono) con el lector elegido. Muchos teléfonos usan UID aleatorios, así que una credencial `DIGITAL` necesita un identificador estable que defina la aplicación móvil.
7. Medir los tiempos de respuesta reales y ajustar `Access:OpenDurationMs`.
