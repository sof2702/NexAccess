BEGIN;

CREATE TABLE IF NOT EXISTS empleado (
    id              BIGSERIAL PRIMARY KEY,
    cedula          VARCHAR(20)  NOT NULL,
    nombre          VARCHAR(100) NOT NULL,
    apellido        VARCHAR(100) NOT NULL,
    email           VARCHAR(150),
    telefono        VARCHAR(30),
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_empleado_cedula UNIQUE (cedula),
    CONSTRAINT uq_empleado_email UNIQUE (email)
);

CREATE TABLE IF NOT EXISTS rol (
    id              BIGSERIAL PRIMARY KEY,
    codigo          VARCHAR(50)  NOT NULL,
    nombre          VARCHAR(100) NOT NULL,
    descripcion     VARCHAR(255),
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_rol_codigo UNIQUE (codigo)
);

CREATE TABLE IF NOT EXISTS empleado_rol (
    id_empleado     BIGINT       NOT NULL,
    id_rol          BIGINT       NOT NULL,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_empleado_rol PRIMARY KEY (id_empleado, id_rol),
    CONSTRAINT fk_empleado_rol_empleado FOREIGN KEY (id_empleado) REFERENCES empleado(id),
    CONSTRAINT fk_empleado_rol_rol FOREIGN KEY (id_rol) REFERENCES rol(id)
);

CREATE TABLE IF NOT EXISTS area (
    id              BIGSERIAL PRIMARY KEY,
    codigo          VARCHAR(50)  NOT NULL,
    nombre          VARCHAR(100) NOT NULL,
    descripcion     VARCHAR(255),
    restringida     BOOLEAN      NOT NULL DEFAULT TRUE,
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_area_codigo UNIQUE (codigo)
);

CREATE TABLE IF NOT EXISTS permiso_acceso (
    id              BIGSERIAL PRIMARY KEY,
    id_rol          BIGINT       NOT NULL,
    id_area         BIGINT       NOT NULL,
    hora_desde      TIME,
    hora_hasta      TIME,
    lunes           BOOLEAN      NOT NULL DEFAULT TRUE,
    martes          BOOLEAN      NOT NULL DEFAULT TRUE,
    miercoles       BOOLEAN      NOT NULL DEFAULT TRUE,
    jueves          BOOLEAN      NOT NULL DEFAULT TRUE,
    viernes         BOOLEAN      NOT NULL DEFAULT TRUE,
    sabado          BOOLEAN      NOT NULL DEFAULT FALSE,
    domingo         BOOLEAN      NOT NULL DEFAULT FALSE,
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_permiso_rol FOREIGN KEY (id_rol) REFERENCES rol(id),
    CONSTRAINT fk_permiso_area FOREIGN KEY (id_area) REFERENCES area(id),
    CONSTRAINT uq_permiso_rol_area UNIQUE (id_rol, id_area)
);

CREATE TABLE IF NOT EXISTS credencial (
    id                  BIGSERIAL PRIMARY KEY,
    id_empleado         BIGINT       NOT NULL,
    codigo_credencial   VARCHAR(150) NOT NULL,
    tipo                VARCHAR(20)  NOT NULL,
    nombre              VARCHAR(100),
    activa              BOOLEAN      NOT NULL DEFAULT TRUE,
    fecha_emision       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    fecha_expiracion    TIMESTAMPTZ,
    revocada_at         TIMESTAMPTZ,
    motivo_revocacion   VARCHAR(255),
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_credencial_codigo UNIQUE (codigo_credencial),
    CONSTRAINT ck_credencial_tipo CHECK (tipo IN ('FISICA', 'DIGITAL')),
    CONSTRAINT fk_credencial_empleado FOREIGN KEY (id_empleado) REFERENCES empleado(id)
);

CREATE TABLE IF NOT EXISTS dispositivo (
    id              BIGSERIAL PRIMARY KEY,
    codigo          VARCHAR(50)  NOT NULL,
    nombre          VARCHAR(100) NOT NULL,
    tipo            VARCHAR(20)  NOT NULL,
    id_area         BIGINT,
    descripcion     VARCHAR(255),
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    clave_hash      VARCHAR(128) NOT NULL,
    clave_rotada_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    ultima_conexion TIMESTAMPTZ,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_dispositivo_codigo UNIQUE (codigo),
    CONSTRAINT uq_dispositivo_clave_hash UNIQUE (clave_hash),
    CONSTRAINT ck_dispositivo_tipo CHECK (tipo IN ('ACCESO', 'MARCACION', 'MIXTO')),
    CONSTRAINT fk_dispositivo_area FOREIGN KEY (id_area) REFERENCES area(id)
);

CREATE TABLE IF NOT EXISTS usuario (
    id              BIGSERIAL PRIMARY KEY,
    id_empleado     BIGINT,
    username        VARCHAR(100) NOT NULL,
    password_hash   VARCHAR(255) NOT NULL,
    rol_sistema     VARCHAR(30)  NOT NULL,
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    ultimo_login    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_usuario_username UNIQUE (username),
    CONSTRAINT ck_usuario_rol_sistema CHECK (rol_sistema IN ('ADMIN', 'SEGURIDAD', 'RRHH')),
    CONSTRAINT fk_usuario_empleado FOREIGN KEY (id_empleado) REFERENCES empleado(id)
);

CREATE TABLE IF NOT EXISTS evento_acceso (
    id                  BIGSERIAL PRIMARY KEY,
    id_empleado         BIGINT,
    id_credencial       BIGINT,
    id_dispositivo      BIGINT       NOT NULL,
    id_area             BIGINT,
    codigo_credencial   VARCHAR(150),
    fecha_hora          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    resultado           VARCHAR(20)  NOT NULL,
    motivo              VARCHAR(100),
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_evento_resultado CHECK (resultado IN ('AUTORIZADO', 'RECHAZADO')),
    CONSTRAINT fk_evento_empleado FOREIGN KEY (id_empleado) REFERENCES empleado(id),
    CONSTRAINT fk_evento_credencial FOREIGN KEY (id_credencial) REFERENCES credencial(id),
    CONSTRAINT fk_evento_dispositivo FOREIGN KEY (id_dispositivo) REFERENCES dispositivo(id),
    CONSTRAINT fk_evento_area FOREIGN KEY (id_area) REFERENCES area(id)
);

CREATE TABLE IF NOT EXISTS marcacion (
    id                  BIGSERIAL PRIMARY KEY,
    id_empleado         BIGINT       NOT NULL,
    id_credencial       BIGINT,
    id_dispositivo      BIGINT,
    tipo                VARCHAR(20)  NOT NULL,
    fecha_hora          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    origen              VARCHAR(20)  NOT NULL DEFAULT 'DISPOSITIVO',
    observacion         VARCHAR(255),
    id_usuario_registro BIGINT,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_marcacion_tipo CHECK (tipo IN ('ENTRADA', 'SALIDA')),
    CONSTRAINT ck_marcacion_origen CHECK (origen IN ('DISPOSITIVO', 'MANUAL')),
    CONSTRAINT ck_marcacion_origen_dispositivo CHECK (origen = 'MANUAL' OR id_dispositivo IS NOT NULL),
    CONSTRAINT fk_marcacion_empleado FOREIGN KEY (id_empleado) REFERENCES empleado(id),
    CONSTRAINT fk_marcacion_credencial FOREIGN KEY (id_credencial) REFERENCES credencial(id),
    CONSTRAINT fk_marcacion_dispositivo FOREIGN KEY (id_dispositivo) REFERENCES dispositivo(id),
    CONSTRAINT fk_marcacion_usuario FOREIGN KEY (id_usuario_registro) REFERENCES usuario(id)
);

CREATE TABLE IF NOT EXISTS enrolamiento (
    id                  BIGSERIAL PRIMARY KEY,
    id_empleado         BIGINT       NOT NULL,
    id_dispositivo      BIGINT       NOT NULL,
    tipo_credencial     VARCHAR(20)  NOT NULL DEFAULT 'FISICA',
    nombre_credencial   VARCHAR(100),
    estado              VARCHAR(20)  NOT NULL DEFAULT 'PENDIENTE',
    fecha_expiracion    TIMESTAMPTZ  NOT NULL,
    id_credencial       BIGINT,
    id_usuario_solicitante BIGINT,
    completado_at       TIMESTAMPTZ,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_enrolamiento_tipo CHECK (tipo_credencial IN ('FISICA', 'DIGITAL')),
    CONSTRAINT ck_enrolamiento_estado CHECK (estado IN ('PENDIENTE', 'COMPLETADO', 'VENCIDO', 'CANCELADO')),
    CONSTRAINT fk_enrolamiento_empleado FOREIGN KEY (id_empleado) REFERENCES empleado(id),
    CONSTRAINT fk_enrolamiento_dispositivo FOREIGN KEY (id_dispositivo) REFERENCES dispositivo(id),
    CONSTRAINT fk_enrolamiento_credencial FOREIGN KEY (id_credencial) REFERENCES credencial(id),
    CONSTRAINT fk_enrolamiento_usuario FOREIGN KEY (id_usuario_solicitante) REFERENCES usuario(id)
);

CREATE TABLE IF NOT EXISTS comando_dispositivo (
    id                  BIGSERIAL PRIMARY KEY,
    id_dispositivo      BIGINT       NOT NULL,
    tipo                VARCHAR(30)  NOT NULL,
    id_enrolamiento     BIGINT,
    estado              VARCHAR(20)  NOT NULL DEFAULT 'PENDIENTE',
    fecha_expiracion    TIMESTAMPTZ  NOT NULL,
    entregado_at        TIMESTAMPTZ,
    completado_at       TIMESTAMPTZ,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_comando_tipo CHECK (tipo IN ('ENROLL_CARD')),
    CONSTRAINT ck_comando_estado CHECK (estado IN ('PENDIENTE', 'ENTREGADO', 'COMPLETADO', 'VENCIDO', 'CANCELADO')),
    CONSTRAINT fk_comando_dispositivo FOREIGN KEY (id_dispositivo) REFERENCES dispositivo(id),
    CONSTRAINT fk_comando_enrolamiento FOREIGN KEY (id_enrolamiento) REFERENCES enrolamiento(id)
);

CREATE TABLE IF NOT EXISTS auditoria (
    id              BIGSERIAL PRIMARY KEY,
    id_usuario      BIGINT,
    id_dispositivo  BIGINT,
    accion          VARCHAR(60)  NOT NULL,
    entidad         VARCHAR(60)  NOT NULL,
    id_entidad      BIGINT,
    detalle         VARCHAR(1000),
    direccion_ip    VARCHAR(45),
    fecha_hora      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_auditoria_usuario FOREIGN KEY (id_usuario) REFERENCES usuario(id),
    CONSTRAINT fk_auditoria_dispositivo FOREIGN KEY (id_dispositivo) REFERENCES dispositivo(id)
);

DO $$
DECLARE
    definiciones TEXT[][] := ARRAY[
        ['idx_evento_acceso_empleado_fecha',    'CREATE INDEX idx_evento_acceso_empleado_fecha ON evento_acceso (id_empleado, fecha_hora DESC)'],
        ['idx_evento_acceso_dispositivo_fecha', 'CREATE INDEX idx_evento_acceso_dispositivo_fecha ON evento_acceso (id_dispositivo, fecha_hora DESC)'],
        ['idx_evento_acceso_fecha',             'CREATE INDEX idx_evento_acceso_fecha ON evento_acceso (fecha_hora DESC)'],
        ['idx_marcacion_empleado_fecha',        'CREATE INDEX idx_marcacion_empleado_fecha ON marcacion (id_empleado, fecha_hora DESC)'],
        ['idx_credencial_codigo_activa',        'CREATE INDEX idx_credencial_codigo_activa ON credencial (codigo_credencial) WHERE activa = TRUE'],
        ['idx_credencial_empleado',             'CREATE INDEX idx_credencial_empleado ON credencial (id_empleado)'],
        ['idx_permiso_area',                    'CREATE INDEX idx_permiso_area ON permiso_acceso (id_area)'],
        ['idx_empleado_activo',                 'CREATE INDEX idx_empleado_activo ON empleado (activo)'],
        ['idx_enrolamiento_dispositivo_estado', 'CREATE INDEX idx_enrolamiento_dispositivo_estado ON enrolamiento (id_dispositivo, estado)'],
        ['idx_comando_dispositivo_estado',      'CREATE INDEX idx_comando_dispositivo_estado ON comando_dispositivo (id_dispositivo, estado, created_at)'],
        ['idx_auditoria_fecha',                 'CREATE INDEX idx_auditoria_fecha ON auditoria (fecha_hora DESC)']
    ];
    i INT;
BEGIN
    FOR i IN 1 .. array_length(definiciones, 1) LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = definiciones[i][1] AND relkind = 'i') THEN
            EXECUTE definiciones[i][2];
        END IF;
    END LOOP;
END $$;

COMMIT;
