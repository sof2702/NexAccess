
BEGIN;

-- Separar el acceso administrativo del acceso móvil.
ALTER TABLE usuario
    ADD COLUMN IF NOT EXISTS acceso_web BOOLEAN;

ALTER TABLE usuario
    ADD COLUMN IF NOT EXISTS acceso_movil BOOLEAN;

-- Conservar el comportamiento de las cuentas existentes.
UPDATE usuario
SET
    acceso_web = (rol_sistema <> 'EMPLEADO'),
    acceso_movil = (rol_sistema = 'EMPLEADO')
WHERE acceso_web IS NULL
   OR acceso_movil IS NULL;

ALTER TABLE usuario
    ALTER COLUMN acceso_web SET NOT NULL,
    ALTER COLUMN acceso_movil SET NOT NULL,
    ALTER COLUMN acceso_web SET DEFAULT FALSE,
    ALTER COLUMN acceso_movil SET DEFAULT FALSE;

-- No permitir acceso móvil sin empleado vinculado.
ALTER TABLE usuario
    ADD CONSTRAINT ck_usuario_movil_empleado
    CHECK (
        NOT acceso_movil
        OR id_empleado IS NOT NULL
    );

-- El rol EMPLEADO no habilita el panel administrativo.
ALTER TABLE usuario
    ADD CONSTRAINT ck_usuario_empleado_sin_web
    CHECK (
        rol_sistema <> 'EMPLEADO'
        OR NOT acceso_web
    );

COMMIT;

