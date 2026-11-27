-- =============================================================================
-- 02_pkg_datos_spec.sql
-- Especificación del package PKG_DATOS: operaciones CRUD sobre la tabla DATOS.
-- =============================================================================

CREATE OR REPLACE PACKAGE PKG_DATOS AS

    -- Código de error de aplicación cuando el ID solicitado no existe.
    C_ERR_NO_EXISTE CONSTANT PLS_INTEGER := -20001;

    /**
     * Inserta un dato. El ID se toma de SEQ_DATOS y se devuelve en p_id.
     * Si CODIGO + SECUENCIA ya existen, Oracle lanza ORA-00001.
     */
    PROCEDURE SP_INSERTAR (
        p_codigo          IN  DATOS.CODIGO%TYPE,
        p_secuencia       IN  DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN  DATOS.FECHARECEPCION%TYPE,
        p_peso            IN  DATOS.PESO%TYPE,
        p_descripcion     IN  DATOS.DESCRIPCION%TYPE DEFAULT NULL,
        p_id              OUT DATOS.ID%TYPE
    );

    /**
     * Devuelve el dato con el ID indicado.
     * Si no existe, el cursor no trae filas.
     */
    PROCEDURE SP_OBTENER (
        p_id     IN  DATOS.ID%TYPE,
        p_cursor OUT SYS_REFCURSOR
    );

    /**
     * Lista los datos, del más reciente al más antiguo, junto con la cantidad
     * de registros que tiene cada código (TOTAL_CODIGO).
     * p_codigo es un filtro opcional (NULL = todos).
     */
    PROCEDURE SP_LISTAR (
        p_codigo IN  DATOS.CODIGO%TYPE DEFAULT NULL,
        p_cursor OUT SYS_REFCURSOR
    );

    /**
     * Actualiza el dato con el ID indicado.
     * Lanza C_ERR_NO_EXISTE (-20001) si el ID no existe.
     */
    PROCEDURE SP_ACTUALIZAR (
        p_id              IN DATOS.ID%TYPE,
        p_codigo          IN DATOS.CODIGO%TYPE,
        p_secuencia       IN DATOS.SECUENCIA%TYPE,
        p_fecha_recepcion IN DATOS.FECHARECEPCION%TYPE,
        p_peso            IN DATOS.PESO%TYPE,
        p_descripcion     IN DATOS.DESCRIPCION%TYPE
    );

    /**
     * Elimina el dato con el ID indicado.
     * Lanza C_ERR_NO_EXISTE (-20001) si el ID no existe.
     */
    PROCEDURE SP_ELIMINAR (
        p_id IN DATOS.ID%TYPE
    );

    /**
     * Cantidad de registros que tiene un código. Usada por SP_LISTAR.
     */
    FUNCTION FN_TOTAL_POR_CODIGO (
        p_codigo IN DATOS.CODIGO%TYPE
    ) RETURN NUMBER;

END PKG_DATOS;
/
