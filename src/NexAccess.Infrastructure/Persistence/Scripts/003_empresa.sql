
CREATE TABLE IF NOT EXISTS empresa (
    id BIGSERIAL PRIMARY KEY,
    razon_social VARCHAR(200) NOT NULL,
    nombre_fantasia VARCHAR(200),
    ruc VARCHAR(20),
    direccion VARCHAR(300),
    telefono VARCHAR(50),
    email VARCHAR(150),
    logo_path VARCHAR(500),
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

