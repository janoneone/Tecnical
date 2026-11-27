-- =============================================================================
-- 04_seed.sql
-- Carga 100.000 registros en DATOS: 5.000 códigos x 20 secuencias.
-- Los valores se derivan de ORA_HASH, así que la carga es idéntica en cada
-- instalación (no depende de la fecha ni de un generador aleatorio).
-- =============================================================================

INSERT /*+ APPEND */ INTO DATOS (ID, CODIGO, SECUENCIA, FECHARECEPCION, PESO, DESCRIPCION)
SELECT SEQ_DATOS.NEXTVAL,
       'COD-' || LPAD(CEIL(n / 20), 5, '0'),
       MOD(n - 1, 20) + 1,
       -- Fechas durante el año anterior al 01-11-2026, con precisión de microsegundos.
       TIMESTAMP '2026-11-01 00:00:00'
         - NUMTODSINTERVAL(ORA_HASH(n, 31535999, 3), 'SECOND')
         + NUMTODSINTERVAL(ORA_HASH(n, 999999, 5) / 1000000, 'SECOND'),
       -- Peso entre 0,0000 y 200,0000.
       ORA_HASH(n, 2000000, 7) / 10000,
       -- Cerca del 60 % de las filas tiene descripción.
       CASE
         WHEN ORA_HASH(n, 9, 11) < 6 THEN
           DECODE(ORA_HASH(n, 7, 13),
                  0, 'Recepción en bodega central',
                  1, 'Envío desde sucursal Ñuñoa',
                  2, 'Revisión de calidad pendiente',
                  3, 'Paquete con daño en el embalaje',
                  4, 'Devolución por dirección errónea',
                  5, 'Pedido urgente para Concepción',
                  6, 'Carga consolidada de proveedores',
                     'Recepción parcial; falta documentación')
           || ' (lote ' || TO_CHAR(n) || ')'
       END
  FROM (SELECT LEVEL AS n FROM DUAL CONNECT BY LEVEL <= 100000);

COMMIT;

-- Estadísticas actualizadas para que el optimizador conozca el volumen real.
BEGIN
    DBMS_STATS.GATHER_TABLE_STATS(ownname => USER, tabname => 'DATOS', cascade => TRUE);
END;
/
