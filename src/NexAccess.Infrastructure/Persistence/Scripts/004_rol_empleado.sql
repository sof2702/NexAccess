
BEGIN;

-- Eliminar únicamente la restricción anterior de roles.
ALTER TABLE usuario
DROP CONSTRAINT IF EXISTS ck_usuario_rol_sistema;

-- Incorporar EMPLEADO sin modificar los roles existentes.
ALTER TABLE usuario
ADD CONSTRAINT ck_usuario_rol_sistema
CHECK (
    rol_sistema IN (
        'ADMIN',
        'SEGURIDAD',
        'RRHH',
        'EMPLEADO'
    )
);

-- Las cuentas EMPLEADO deben estar vinculadas a un empleado.
ALTER TABLE usuario
ADD CONSTRAINT ck_usuario_empleado_vinculado
CHECK (
    rol_sistema <> 'EMPLEADO'
    OR id_empleado IS NOT NULL
);

COMMIT;

