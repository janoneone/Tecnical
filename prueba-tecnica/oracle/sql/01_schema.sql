-- =============================================================================
-- 01_schema.sql
-- Tabla DATOS, índice único (CODIGO, SECUENCIA) y secuencia para el ID.
-- Se ejecuta como el usuario de aplicación (PRUEBA) en FREEPDB1.
-- =============================================================================

CREATE TABLE DATOS (
    ID              NUMBER              NOT NULL,
    CODIGO          VARCHAR2(32)        NOT NULL,
    SECUENCIA       INTEGER             NOT NULL,
    FECHARECEPCION  TIMESTAMP           NOT NULL,
    PESO            NUMBER(19,4)        NOT NULL,
    DESCRIPCION     VARCHAR2(320 CHAR)  NULL,
    CONSTRAINT PK_DATOS PRIMARY KEY (ID)
);

COMMENT ON TABLE  DATOS                IS 'Datos recibidos, identificados por código y secuencia.';
COMMENT ON COLUMN DATOS.ID             IS 'Identificador. Se obtiene de la secuencia SEQ_DATOS.';
COMMENT ON COLUMN DATOS.CODIGO         IS 'Código del dato.';
COMMENT ON COLUMN DATOS.SECUENCIA      IS 'Número de secuencia dentro del código.';
COMMENT ON COLUMN DATOS.FECHARECEPCION IS 'Fecha y hora de recepción.';
COMMENT ON COLUMN DATOS.PESO           IS 'Peso, con 4 decimales.';
COMMENT ON COLUMN DATOS.DESCRIPCION    IS 'Descripción opcional, hasta 320 caracteres.';

-- Un mismo código no puede repetir secuencia.
CREATE UNIQUE INDEX UX_DATOS_COD_SEC ON DATOS (CODIGO, SECUENCIA);

-- Secuencia para el campo ID.
CREATE SEQUENCE SEQ_DATOS
    START WITH 1
    INCREMENT BY 1
    CACHE 20
    NOCYCLE;
