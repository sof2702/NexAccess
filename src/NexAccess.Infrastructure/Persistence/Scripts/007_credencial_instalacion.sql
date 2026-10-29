BEGIN;

-- Vincular las credenciales digitales
-- con una instalación Android.
ALTER TABLE credencial
    ADD COLUMN IF NOT EXISTS instalacion_id UUID;

-- Facilitar la búsqueda de credenciales
-- correspondientes a una instalación.
CREATE INDEX IF NOT EXISTS idx_credencial_instalacion
    ON credencial (instalacion_id)
    WHERE instalacion_id IS NOT NULL;

-- Las credenciales físicas no deben
-- estar vinculadas a instalaciones Android.
ALTER TABLE credencial
    ADD CONSTRAINT ck_credencial_instalacion_digital
    CHECK (
        instalacion_id IS NULL
        OR tipo = 'DIGITAL'
    );

COMMIT;
