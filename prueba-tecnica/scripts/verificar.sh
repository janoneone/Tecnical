#!/usr/bin/env bash
# =============================================================================
# Verifica que el entorno de la prueba quedó bien levantado.
# Uso (desde la carpeta prueba-tecnica):   ./scripts/verificar.sh
# Requiere bash (macOS, Linux, WSL o Git Bash en Windows) y Docker.
# No deja cambios en la base: las pruebas de escritura se deshacen con ROLLBACK.
# =============================================================================

ORACLE_CONTAINER="prueba-oracle"
RABBIT_CONTAINER="prueba-rabbitmq"
DB_USER="${ORACLE_APP_USER:-prueba}"
DB_PASS="${ORACLE_APP_PASSWORD:-prueba}"
MGMT_PORT="${RABBITMQ_MANAGEMENT_PORT:-15672}"

fallas=0
ok()    { printf '  [OK]    %s\n' "$1"; }
falla() { printf '  [FALLA] %s\n' "$1"; fallas=$((fallas + 1)); }

# Ejecuta SQL en Oracle y devuelve solo el resultado (sin encabezados).
sql() {
  docker exec -i "$ORACLE_CONTAINER" sqlplus -s -L "${DB_USER}/${DB_PASS}@//localhost/FREEPDB1" <<EOF
SET HEADING OFF FEEDBACK OFF PAGESIZE 0 VERIFY OFF SERVEROUTPUT ON LINESIZE 300
WHENEVER SQLERROR EXIT FAILURE
$1
EXIT
EOF
}

# Compara el resultado de una consulta con el valor esperado.
esperar() {
  local descripcion="$1" consulta="$2" esperado="$3" obtenido
  obtenido="$(sql "$consulta" | tr -d '[:space:]')"
  if [ "$obtenido" = "$esperado" ]; then ok "$descripcion"; else falla "$descripcion (esperado: $esperado, obtenido: ${obtenido:-vacío})"; fi
}

echo "== Contenedores"
for c in "$ORACLE_CONTAINER" "$RABBIT_CONTAINER"; do
  estado="$(docker inspect -f '{{.State.Health.Status}}' "$c" 2>/dev/null)"
  if [ "$estado" = "healthy" ]; then ok "$c: healthy"; else falla "$c: ${estado:-no existe} (espera a que esté healthy o revisa 'docker compose logs')"; fi
done
if [ "$fallas" -gt 0 ]; then
  echo; echo "Los contenedores no están listos; se omiten las demás verificaciones."; exit 1
fi

echo "== Oracle: objetos"
esperar "Tabla DATOS existe"              "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'DATOS';" "1"
esperar "Índice único UX_DATOS_COD_SEC (CODIGO, SECUENCIA)" \
  "SELECT u.UNIQUENESS || ':' || LISTAGG(c.COLUMN_NAME, ',') WITHIN GROUP (ORDER BY c.COLUMN_POSITION) FROM USER_INDEXES u JOIN USER_IND_COLUMNS c ON c.INDEX_NAME = u.INDEX_NAME WHERE u.INDEX_NAME = 'UX_DATOS_COD_SEC' GROUP BY u.UNIQUENESS;" \
  "UNIQUE:CODIGO,SECUENCIA"
esperar "Secuencia SEQ_DATOS existe"      "SELECT COUNT(*) FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'SEQ_DATOS';" "1"
esperar "Package PKG_DATOS válido"        "SELECT LISTAGG(OBJECT_TYPE || '=' || STATUS, ',') WITHIN GROUP (ORDER BY OBJECT_TYPE) FROM USER_OBJECTS WHERE OBJECT_NAME = 'PKG_DATOS';" "PACKAGE=VALID,PACKAGEBODY=VALID"
esperar "DESCRIPCION: opcional, 320 caracteres" \
  "SELECT NULLABLE || ':' || CHAR_LENGTH || ':' || CHAR_USED FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'DATOS' AND COLUMN_NAME = 'DESCRIPCION';" \
  "Y:320:C"

echo "== Oracle: datos"
esperar "100.000 registros cargados"      "SELECT COUNT(*) FROM DATOS;" "100000"
esperar "Sin duplicados de CODIGO + SECUENCIA" \
  "SELECT COUNT(*) FROM (SELECT CODIGO, SECUENCIA FROM DATOS GROUP BY CODIGO, SECUENCIA HAVING COUNT(*) > 1);" "0"

echo "== Oracle: package (con ROLLBACK)"
esperar "SP_INSERTAR devuelve un ID de la secuencia, con y sin descripción" "
DECLARE
  v_id1 NUMBER; v_id2 NUMBER; v_max NUMBER;
BEGIN
  SELECT MAX(ID) INTO v_max FROM DATOS;
  PKG_DATOS.SP_INSERTAR('VERIF-1', 1, SYSTIMESTAMP, 1.5, NULL, v_id1);
  PKG_DATOS.SP_INSERTAR('VERIF-1', 2, SYSTIMESTAMP, 2.5, 'con descripción', v_id2);
  DBMS_OUTPUT.PUT_LINE(CASE WHEN v_id1 > v_max AND v_id2 > v_id1 THEN 'SI' ELSE 'NO' END);
  ROLLBACK;
END;
/" "SI"
esperar "DESCRIPCION acepta 320 caracteres con tildes y ñ" "
DECLARE
  v_id NUMBER; v_len NUMBER;
BEGIN
  PKG_DATOS.SP_INSERTAR('VERIF-2', 1, SYSTIMESTAMP, 1, RPAD('ñ', 320, 'á'), v_id);
  SELECT LENGTH(DESCRIPCION) INTO v_len FROM DATOS WHERE ID = v_id;
  DBMS_OUTPUT.PUT_LINE(v_len);
  ROLLBACK;
END;
/" "320"
esperar "CODIGO + SECUENCIA repetidos se rechazan (ORA-00001)" "
DECLARE
  v_id NUMBER;
BEGIN
  PKG_DATOS.SP_INSERTAR('COD-00001', 1, SYSTIMESTAMP, 1, NULL, v_id);
  DBMS_OUTPUT.PUT_LINE('NO');
  ROLLBACK;
EXCEPTION
  WHEN DUP_VAL_ON_INDEX THEN DBMS_OUTPUT.PUT_LINE('SI'); ROLLBACK;
END;
/" "SI"

echo "== RabbitMQ"
cola="$(docker exec "$RABBIT_CONTAINER" rabbitmqctl -q list_queues name durable 2>/dev/null | awk '$1 == "pesos_especiales" {print $2}')"
if [ "$cola" = "true" ]; then ok "Cola pesos_especiales existe y es durable"; else falla "Cola pesos_especiales no existe o no es durable"; fi
codigo_http="$(curl -s -o /dev/null -w '%{http_code}' -u prueba:prueba "http://localhost:${MGMT_PORT}/api/whoami")"
if [ "$codigo_http" = "200" ]; then ok "Usuario prueba entra a la consola (http://localhost:${MGMT_PORT})"; else falla "Login en la consola de RabbitMQ (HTTP ${codigo_http})"; fi

echo
if [ "$fallas" -eq 0 ]; then
  echo "Entorno listo."
else
  echo "Hubo ${fallas} verificación(es) con falla."
  exit 1
fi
