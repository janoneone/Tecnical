CREATE OR REPLACE PACKAGE PKG_DATOS AS

    C_ERR_NO_EXISTE CONSTANT PLS_INTEGER := -20001;
    C_ERR_PAGINA_INVALIDA CONSTANT PLS_INTEGER := -20002;
    C_TAMANIO_PAGINA CONSTANT PLS_INTEGER := 20;

    PROCEDURE SP_INSERTAR (
        p_codigo          IN  DATOS.CODIGO%TYPE,
        p_secuencia       IN  DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN  DATOS.FECHARECEPCION%TYPE,
        p_peso            IN  DATOS.PESO%TYPE,
        p_descripcion     IN  DATOS.DESCRIPCION%TYPE DEFAULT NULL,
        p_id              OUT DATOS.ID%TYPE
    );

    PROCEDURE SP_OBTENER (
        p_id     IN  DATOS.ID%TYPE,
        p_cursor OUT SYS_REFCURSOR
    );

    PROCEDURE SP_LISTAR (
        p_codigo IN  DATOS.CODIGO%TYPE DEFAULT NULL,
        p_cursor OUT SYS_REFCURSOR
    );

    PROCEDURE SP_LISTAR_PAGINADO (
        p_pagina          IN  NUMBER,
        p_total_registros OUT NUMBER,
        p_cursor          OUT SYS_REFCURSOR
    );

    PROCEDURE SP_ACTUALIZAR (
        p_id              IN DATOS.ID%TYPE,
        p_codigo          IN DATOS.CODIGO%TYPE,
        p_secuencia       IN DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN DATOS.FECHARECEPCION%TYPE,
        p_peso            IN DATOS.PESO%TYPE,
        p_descripcion     IN DATOS.DESCRIPCION%TYPE
    );

    PROCEDURE SP_ACTUALIZAR_DESCRIPCION (
        p_id          IN DATOS.ID%TYPE,
        p_descripcion IN DATOS.DESCRIPCION%TYPE
    );

    PROCEDURE SP_ELIMINAR (
        p_id IN DATOS.ID%TYPE
    );

    FUNCTION FN_TOTAL_POR_CODIGO (
        p_codigo IN DATOS.CODIGO%TYPE
    ) RETURN NUMBER;

END PKG_DATOS;
/

CREATE OR REPLACE PACKAGE BODY PKG_DATOS AS

    -- INSERTAR
    PROCEDURE SP_INSERTAR (
        p_codigo          IN  DATOS.CODIGO%TYPE,
        p_secuencia       IN  DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN  DATOS.FECHARECEPCION%TYPE,
        p_peso            IN  DATOS.PESO%TYPE,
        p_descripcion     IN  DATOS.DESCRIPCION%TYPE DEFAULT NULL,
        p_id              OUT DATOS.ID%TYPE
    ) IS
    BEGIN
        INSERT INTO DATOS (
            ID,
            CODIGO,
            SECUENCIA,
            FECHARECEPCION,
            PESO,
            DESCRIPCION
        )
        VALUES (
            SEQ_DATOS.NEXTVAL,
            p_codigo,
            p_secuencia,
            p_fecha_recepcion,
            p_peso,
            p_descripcion
        )
        RETURNING ID INTO p_id;

    END SP_INSERTAR;


    -- OBTENER POR ID
    PROCEDURE SP_OBTENER (
        p_id     IN  DATOS.ID%TYPE,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT ID,
                   CODIGO,
                   SECUENCIA,
                   FECHARECEPCION,
                   PESO,
                   DESCRIPCION
            FROM DATOS
            WHERE ID = p_id;

    END SP_OBTENER;


    -- LISTADO OPTIMIZADO
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
                   COUNT(*) OVER (
                       PARTITION BY d.CODIGO
                   ) AS TOTAL_CODIGO
            FROM DATOS d
            WHERE (
                p_codigo IS NULL
                OR UPPER(TRIM(d.CODIGO)) =
                   UPPER(TRIM(p_codigo))
            )
            ORDER BY d.FECHARECEPCION DESC,
                     d.ID DESC;

    END SP_LISTAR;


    -- LISTADO PAGINADO
    PROCEDURE SP_LISTAR_PAGINADO (
        p_pagina          IN  NUMBER,
        p_total_registros OUT NUMBER,
        p_cursor          OUT SYS_REFCURSOR
    ) IS
        v_offset NUMBER;
    BEGIN

        IF p_pagina IS NULL
           OR p_pagina < 1
           OR p_pagina <> TRUNC(p_pagina) THEN

            RAISE_APPLICATION_ERROR(
                C_ERR_PAGINA_INVALIDA,
                'El número de página debe ser un entero mayor a cero.'
            );

        END IF;

        v_offset := (p_pagina - 1) * C_TAMANIO_PAGINA;

        SELECT COUNT(*)
        INTO p_total_registros
        FROM DATOS;

        OPEN p_cursor FOR
            SELECT d.ID,
                   d.CODIGO,
                   d.SECUENCIA,
                   d.FECHARECEPCION,
                   d.PESO,
                   d.DESCRIPCION,
                   COUNT(*) OVER (
                       PARTITION BY d.CODIGO
                   ) AS TOTAL_CODIGO
            FROM DATOS d
            ORDER BY d.FECHARECEPCION DESC,
                     d.ID DESC
            OFFSET v_offset ROWS
            FETCH NEXT C_TAMANIO_PAGINA ROWS ONLY;

    END SP_LISTAR_PAGINADO;


    -- ACTUALIZACIÓN ORIGINAL
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
            RAISE_APPLICATION_ERROR(
                C_ERR_NO_EXISTE,
                'No existe un dato con ID ' || p_id
            );
        END IF;

    END SP_ACTUALIZAR;


    -- ACTUALIZAR EXCLUSIVAMENTE LA DESCRIPCIÓN
    PROCEDURE SP_ACTUALIZAR_DESCRIPCION (
        p_id          IN DATOS.ID%TYPE,
        p_descripcion IN DATOS.DESCRIPCION%TYPE
    ) IS
    BEGIN
        UPDATE DATOS
        SET DESCRIPCION = p_descripcion
        WHERE ID = p_id;

        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(
                C_ERR_NO_EXISTE,
                'No existe un dato con ID ' || p_id
            );
        END IF;

    END SP_ACTUALIZAR_DESCRIPCION;


    -- ELIMINAR
    PROCEDURE SP_ELIMINAR (
        p_id IN DATOS.ID%TYPE
    ) IS
    BEGIN
        DELETE FROM DATOS
        WHERE ID = p_id;

        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(
                C_ERR_NO_EXISTE,
                'No existe un dato con ID ' || p_id
            );
        END IF;

    END SP_ELIMINAR;


    -- FUNCIÓN ORIGINAL MANTENIDA POR COMPATIBILIDAD
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