BEGIN;

INSERT INTO rol (codigo, nombre, descripcion)
SELECT v.codigo, v.nombre, v.descripcion
FROM (VALUES
    ('EMPLEADO',      'Empleado',       'Personal general de la organización'),
    ('SEGURIDAD',     'Seguridad',      'Personal de seguridad física y lógica'),
    ('RRHH',          'Recursos Humanos', 'Personal de Recursos Humanos'),
    ('ADMINISTRADOR', 'Administrador',  'Administración general del sistema')
) AS v (codigo, nombre, descripcion)
WHERE NOT EXISTS (SELECT 1 FROM rol r WHERE r.codigo = v.codigo);

COMMIT;
