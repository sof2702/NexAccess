# Seguridad

## Usuarios administrativos (JWT)

- `POST /api/v1/auth/login` devuelve un JWT firmado con HMAC-SHA256 y la clave `Jwt:Key`, que debe tener al menos 32 bytes. Si falta o es corta, la aplicación no arranca.
- El token se valida con emisor, audiencia, firma y vigencia (`Jwt:ExpirationMinutes`, 60 por defecto; tolerancia de reloj de 30 s).
- En **cada request** se verifica en la base que el usuario siga activo y conserve el rol del token. Desactivar a un usuario o cambiarle el rol invalida sus tokens al instante.
- El login responde siempre `401 INVALID_CREDENTIALS`, tanto si el usuario no existe como si la contraseña es incorrecta o el usuario está inactivo. Para no filtrar qué usuarios existen, también se calcula un hash cuando el usuario no existe.
- Límite de intentos: `RateLimiting:LoginPermitsPerMinute` (10 por IP y minuto). Al superarlo responde `429`.
- Los inicios de sesión, correctos y fallidos, quedan en `auditoria`, sin la contraseña.

### Contraseñas

PBKDF2-HMAC-SHA256 con 600 000 iteraciones, sal aleatoria de 16 bytes y hash de 32 bytes, en el formato `PBKDF2-SHA256$<iteraciones>$<sal>$<hash>`. La comparación es de tiempo constante. Las contraseñas de usuarios nuevos y los cambios requieren al menos 10 caracteres; el administrador inicial, al menos 12.

### Roles del sistema

| Endpoint | ADMIN | SEGURIDAD | RRHH |
|---|:-:|:-:|:-:|
| Usuarios (`/users`) y auditoría (`/audit`) | ✔ | | |
| Empleados: consultar | ✔ | ✔ | ✔ |
| Empleados: crear, editar, activar o desactivar | ✔ | | ✔ |
| Asignar o retirar roles a empleados | ✔ | ✔ | |
| Áreas y roles: consultar | ✔ | ✔ | ✔ |
| Áreas y roles: crear o editar | ✔ | ✔ | |
| Permisos de acceso, credenciales, dispositivos y enrolamientos | ✔ | ✔ | |
| Eventos de acceso y su exportación | ✔ | ✔ | |
| Asistencia: consultar, corregir, reporte y exportación | ✔ | | ✔ |

### Administrador inicial

No hay usuarios ni contraseñas fijas en el código ni en los scripts. Si la tabla `usuario` está vacía y existen `Bootstrap:AdminUsername` y `Bootstrap:AdminPassword`, la API crea un único `ADMIN` al arrancar y registra una advertencia en el log. Después de entrar por primera vez:

1. Cambiar la contraseña (`PUT /api/v1/users/{id}/password`).
2. Borrar `Bootstrap:AdminPassword` de la configuración.

## Dispositivos (X-DEVICE-KEY)

- La clave tiene 256 bits aleatorios (`nxd_` + Base64URL) y solo se muestra al crear el dispositivo o al rotar la clave.
- La base guarda el SHA-256 de la clave (`clave_hash`, único). Al ser una clave de alta entropía, no hace falta sal ni un algoritmo lento.
- Desactivar el dispositivo o rotar su clave invalida la clave anterior al instante.
- La clave identifica al dispositivo. Además, `deviceCode` en el cuerpo y `{codigo}` en la ruta deben coincidir con él (`DEVICE_MISMATCH`), y un enrolamiento solo acepta tarjetas del dispositivo al que se asignó.
- El esquema de dispositivos no da acceso a ningún endpoint administrativo.

## Datos sensibles y logs

- Nunca se registran claves, contraseñas ni tokens. Si una clave de dispositivo es rechazada, solo se registra la IP de origen.
- El código de la credencial se enmascara (`******C3D4`) en los logs, en la auditoría, en las respuestas de eventos y en el CSV de eventos. El listado de credenciales (solo ADMIN o SEGURIDAD) muestra el código completo para poder gestionarlas.
- Las respuestas de usuario no incluyen `password_hash`.
- Las exportaciones CSV escapan las celdas que empiezan con `=`, `+`, `-` o `@` para evitar inyección de fórmulas, y quedan registradas en la auditoría.
- Los errores 500 devuelven un mensaje genérico con `traceId`. El detalle va solo al log del servidor.

## Manejo de secretos

| Secreto | Desarrollo | Producción |
|---|---|---|
| `ConnectionStrings:NexAccess` | User Secrets de `NexAccess.API` | Variable `ConnectionStrings__NexAccess` |
| `Jwt:Key` | User Secrets | Variable `Jwt__Key` o un gestor de secretos |
| `Bootstrap:AdminPassword` | User Secrets (temporal) | Variable temporal; borrarla después del primer arranque |
| `ConnectionStrings:NexAccessTest` | User Secrets `--id nexaccess-integration-tests` | Variable en CI |

`appsettings*.json` no contiene secretos. El `.gitignore` excluye `.env`, `*.pfx`, `appsettings.*.local.json`, `TestResults/` y las carpetas de compilación.

## Transporte

Fuera de Development, la API activa `UseHsts` y `UseHttpsRedirection`. Hay que publicarla con un certificado válido. Los ESP32 deben validar ese certificado.

## Advertencias pendientes

1. **PostgreSQL 9.3.5 no tiene soporte desde 2018** (no recibe parches de seguridad). Ver `BASE_DE_DATOS.md`.
2. **El UID de una tarjeta NFC se puede clonar.** Un control basado solo en el UID (MIFARE Classic o Ultralight) no protege áreas críticas. Para el datacenter o la tesorería conviene usar tarjetas con autenticación criptográfica (por ejemplo, DESFire EV2/EV3) o un segundo factor.
3. La contraseña local de `postgres` es débil y es superusuario. Para cualquier entorno compartido hay que cambiarla y crear un usuario dedicado con permisos mínimos.
4. No hay refresh tokens: al vencer el JWT hay que volver a iniciar sesión.
5. El límite de intentos es por IP y vive en memoria. Detrás de un proxy hay que configurar `ForwardedHeaders` (todavía no está configurado) y, si hay varias instancias, un límite distribuido.
6. No hay bloqueo de cuenta después de muchos intentos fallidos (solo el límite por IP).
7. CORS no está habilitado. Habrá que configurarlo con los orígenes exactos cuando exista el portal web.
