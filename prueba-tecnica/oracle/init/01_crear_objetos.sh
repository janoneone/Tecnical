#!/bin/bash
# =============================================================================
# Se ejecuta automáticamente SOLO en el primer arranque del contenedor Oracle
# (con el volumen de datos vacío). Crea los objetos y carga los datos en el
# esquema de aplicación (por defecto PRUEBA) dentro de FREEPDB1.
#
# La imagen ejecuta este archivo si tiene permiso de ejecución, o lo "sourcea"
# si no lo tiene; por eso no usa "exit" ni "set -e".
# =============================================================================

echo "PRUEBA: creando objetos en el esquema ${APP_USER} ..."

sqlplus -s -L "${APP_USER}/${APP_USER_PASSWORD}@//localhost/FREEPDB1" <<'EOSQL'
WHENEVER SQLERROR EXIT FAILURE
SET ECHO OFF FEEDBACK ON SERVEROUTPUT ON
PROMPT == 01_schema.sql
@/opt/prueba/sql/01_schema.sql
PROMPT == 02_pkg_datos_spec.sql
@/opt/prueba/sql/02_pkg_datos_spec.sql
PROMPT == 03_pkg_datos_body.sql
@/opt/prueba/sql/03_pkg_datos_body.sql
PROMPT == 04_seed.sql
@/opt/prueba/sql/04_seed.sql
EXIT SUCCESS
EOSQL

if [ $? -eq 0 ]; then
  echo "PRUEBA: objetos y datos creados correctamente."
else
  echo "PRUEBA: ERROR al crear los objetos. Revisa el log con 'docker logs prueba-oracle'."
fi
