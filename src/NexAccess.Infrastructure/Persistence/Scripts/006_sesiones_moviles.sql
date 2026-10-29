BEGIN;

-- Cada sesión corresponde a una instalación autenticada
-- de la aplicación móvil NexAccess.
CREATE TABLE IF NOT EXISTS sesion_movil (
    id UUID PRIMARY KEY,

    id_usuario BIGINT NOT NULL,

    -- Identificador generado por la app para su instalación.
    -- No debe utilizarse como secreto de autenticación.
    instalacion_id UUID NOT NULL,

    -- SHA-256 del token de renovación.
    -- Nunca almacenamos el token original.
    refresh_token_hash VARCHAR(64) NOT NULL,

    activa BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ultimo_uso TIMESTAMPTZ,
    token_rotado_at TIMESTAMPTZ,
    revocada_at TIMESTAMPTZ,

    CONSTRAINT fk_sesion_movil_usuario
        FOREIGN KEY (id_usuario)
        REFERENCES usuario(id),

    CONSTRAINT uq_sesion_movil_refresh_hash
        UNIQUE (refresh_token_hash),

    CONSTRAINT ck_sesion_movil_revocacion
        CHECK (
            activa = TRUE
            OR revocada_at IS NOT NULL
        )
);

-- Buscar sesiones de un usuario.
CREATE INDEX IF NOT EXISTS idx_sesion_movil_usuario
    ON sesion_movil (id_usuario);

-- Buscar sesiones activas de una instalación.
CREATE INDEX IF NOT EXISTS idx_sesion_movil_instalacion
    ON sesion_movil (instalacion_id)
    WHERE activa = TRUE;

-- Identificar sesiones revocadas para auditoría y limpieza.
CREATE INDEX IF NOT EXISTS idx_sesion_movil_revocada
    ON sesion_movil (revocada_at)
    WHERE revocada_at IS NOT NULL;


-- Historial de refresh tokens utilizados.
-- Permite identificar reutilizaciones posteriores.
CREATE TABLE IF NOT EXISTS sesion_movil_token_usado (
    id BIGSERIAL PRIMARY KEY,

    id_sesion UUID NOT NULL,

    token_hash VARCHAR(64) NOT NULL,

    usado_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_token_usado_sesion
        FOREIGN KEY (id_sesion)
        REFERENCES sesion_movil(id),

    CONSTRAINT uq_token_usado_hash
        UNIQUE (token_hash)
);

CREATE INDEX IF NOT EXISTS idx_token_usado_sesion
    ON sesion_movil_token_usado (id_sesion);



COMMIT;

