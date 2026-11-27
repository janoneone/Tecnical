-- =============================================================================
-- 03_pkg_datos_body.sql
-- Cuerpo del package PKG_DATOS.
-- =============================================================================

CREATE OR REPLACE PACKAGE BODY PKG_DATOS AS

    PROCEDURE SP_INSERTAR (
        p_codigo          IN  DATOS.CODIGO%TYPE,
        p_secuencia       IN  DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN  DATOS.FECHARECEPCION%TYPE,
        p_peso            IN  DATOS.PESO%TYPE,
        p_descripcion     IN  DATOS.DESCRIPCION%TYPE DEFAULT NULL,
        p_id              OUT DATOS.ID%TYPE
    ) IS
    BEGIN
        INSERT INTO DATOS (ID, CODIGO, SECUENCIA, FECHARECEPCION, PESO, DESCRIPCION)
        VALUES (SEQ_DATOS.NEXTVAL, p_codigo, p_secuencia, p_fecha_recepcion, p_peso, p_descripcion)
        RETURNING ID INTO p_id;
    END SP_INSERTAR;


    PROCEDURE SP_OBTENER (
        p_id     IN  DATOS.ID%TYPE,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT ID, CODIGO, SECUENCIA, FECHARECEPCION, PESO, DESCRIPCION
              FROM DATOS
             WHERE ID = p_id;
    END SP_OBTENER;


    PROCEDURE SP_LISTAR (
        p_codigo IN  DATOS.CODIGO%TYPE DEFAULT NULL,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT d.ID,
                   d.CODIGO,
                   d.SECUENCIA,
                   d.FECHARECEPCION,
                   d.PESO,
                   d.DESCRIPCION,
                   FN_TOTAL_POR_CODIGO(d.CODIGO) AS TOTAL_CODIGO
              FROM DATOS d
             WHERE (p_codigo IS NULL OR UPPER(TRIM(d.CODIGO)) = UPPER(TRIM(p_codigo)))
             ORDER BY TO_CHAR(d.FECHARECEPCION, 'YYYYMMDDHH24MISSFF6') DESC;
    END SP_LISTAR;


    PROCEDURE SP_ACTUALIZAR (
        p_id              IN DATOS.ID%TYPE,
        p_codigo          IN DATOS.CODIGO%TYPE,
        p_secuencia       IN DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN DATOS.FECHARECEPCION%TYPE,
        p_peso            IN DATOS.PESO%TYPE,
        p_descripcion     IN DATOS.DESCRIPCION%TYPE
    ) IS
    BEGIN
        UPDATE DATOS
           SET CODIGO         = p_codigo,
               SECUENCIA      = p_secuencia,
               FECHARECEPCION = p_fecha_recepcion,
               PESO           = p_peso,
               DESCRIPCION    = p_descripcion
         WHERE ID = p_id;

        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(C_ERR_NO_EXISTE, 'No existe un dato con ID ' || p_id);
        END IF;
    END SP_ACTUALIZAR;


    PROCEDURE SP_ELIMINAR (
        p_id IN DATOS.ID%TYPE
    ) IS
    BEGIN
        DELETE FROM DATOS
         WHERE ID = p_id;

        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(C_ERR_NO_EXISTE, 'No existe un dato con ID ' || p_id);
        END IF;
    END SP_ELIMINAR;


    FUNCTION FN_TOTAL_POR_CODIGO (
        p_codigo IN DATOS.CODIGO%TYPE
    ) RETURN NUMBER IS
        v_total NUMBER;
    BEGIN
        SELECT COUNT(*)
          INTO v_total
          FROM DATOS
         WHERE CODIGO = p_codigo;

        RETURN v_total;
    END FN_TOTAL_POR_CODIGO;

END PKG_DATOS;
/
