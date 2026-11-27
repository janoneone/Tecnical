# =============================================================================
# Verifica que el entorno de la prueba quedó bien levantado.
# Versión PowerShell de verificar.sh, para quien no tenga bash (por ejemplo, en Windows).
#
# Uso (desde la carpeta prueba-tecnica):
#   Windows PowerShell 5.1:  powershell -ExecutionPolicy Bypass -File .\scripts\verificar.ps1
#   PowerShell 7 (pwsh):     pwsh -File ./scripts/verificar.ps1
#
# Requiere Docker. No deja cambios en la base: las pruebas de escritura se deshacen con ROLLBACK.
# =============================================================================

$OracleContainer = 'prueba-oracle'
$RabbitContainer = 'prueba-rabbitmq'
$DbUser   = if ($env:ORACLE_APP_USER)          { $env:ORACLE_APP_USER }          else { 'prueba' }
$DbPass   = if ($env:ORACLE_APP_PASSWORD)      { $env:ORACLE_APP_PASSWORD }      else { 'prueba' }
$MgmtPort = if ($env:RABBITMQ_MANAGEMENT_PORT) { $env:RABBITMQ_MANAGEMENT_PORT } else { '15672' }

# UTF-8 para lo que se envía a docker y lo que se lee de él (Windows PowerShell 5.1 usa ASCII por defecto).
$OutputEncoding = New-Object System.Text.UTF8Encoding $false
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false } catch { }

$script:Fallas = 0
function Ok([string]$Texto)    { Write-Host "  [OK]    $Texto" }
function Falla([string]$Texto) { Write-Host "  [FALLA] $Texto"; $script:Fallas++ }

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host 'No se encontró el comando docker. Instala Docker y vuelve a intentarlo.'
    exit 1
}

# Ejecuta SQL en Oracle y devuelve solo el resultado (sin encabezados).
# Dentro del contenedor se quitan los retornos de carro (CR) que Windows agrega a cada línea,
# porque SQL*Plus en Linux no reconoce el "/" final de un bloque PL/SQL si viene con CR.
function Invoke-Sql([string]$Consulta) {
    $entrada = @"
SET HEADING OFF FEEDBACK OFF PAGESIZE 0 VERIFY OFF SERVEROUTPUT ON LINESIZE 300
WHENEVER SQLERROR EXIT FAILURE
$Consulta
EXIT
"@
    $conexion = "$DbUser/$DbPass@//localhost/FREEPDB1"
    $entrada | docker exec -i $OracleContainer sh -c 'tr -d ''\015'' | sqlplus -s -L $0' $conexion
}

# Compara el resultado de una consulta con el valor esperado.
function Esperar([string]$Descripcion, [string]$Consulta, [string]$Esperado) {
    $obtenido = (-join (Invoke-Sql $Consulta)) -replace '\s', ''
    if ($obtenido -eq $Esperado) {
        Ok $Descripcion
    } else {
        $mostrar = if ($obtenido) { $obtenido } else { 'vacío' }
        Falla "$Descripcion (esperado: $Esperado, obtenido: $mostrar)"
    }
}

Write-Host '== Contenedores'
foreach ($c in @($OracleContainer, $RabbitContainer)) {
    $estado = docker inspect -f '{{.State.Health.Status}}' $c 2>$null
    if ($estado -eq 'healthy') {
        Ok "${c}: healthy"
    } else {
        $mostrar = if ($estado) { $estado } else { 'no existe' }
        Falla "${c}: $mostrar (espera a que esté healthy o revisa 'docker compose logs')"
    }
}
if ($script:Fallas -gt 0) {
    Write-Host ''
    Write-Host 'Los contenedores no están listos; se omiten las demás verificaciones.'
    exit 1
}

Write-Host '== Oracle: objetos'
Esperar 'Tabla DATOS existe' `
    "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'DATOS';" '1'
Esperar 'Índice único UX_DATOS_COD_SEC (CODIGO, SECUENCIA)' `
    "SELECT u.UNIQUENESS || ':' || LISTAGG(c.COLUMN_NAME, ',') WITHIN GROUP (ORDER BY c.COLUMN_POSITION) FROM USER_INDEXES u JOIN USER_IND_COLUMNS c ON c.INDEX_NAME = u.INDEX_NAME WHERE u.INDEX_NAME = 'UX_DATOS_COD_SEC' GROUP BY u.UNIQUENESS;" `
    'UNIQUE:CODIGO,SECUENCIA'
Esperar 'Secuencia SEQ_DATOS existe' `
    "SELECT COUNT(*) FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'SEQ_DATOS';" '1'
Esperar 'Package PKG_DATOS válido' `
    "SELECT LISTAGG(OBJECT_TYPE || '=' || STATUS, ',') WITHIN GROUP (ORDER BY OBJECT_TYPE) FROM USER_OBJECTS WHERE OBJECT_NAME = 'PKG_DATOS';" `
    'PACKAGE=VALID,PACKAGEBODY=VALID'
Esperar 'DESCRIPCION: opcional, 320 caracteres' `
    "SELECT NULLABLE || ':' || CHAR_LENGTH || ':' || CHAR_USED FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'DATOS' AND COLUMN_NAME = 'DESCRIPCION';" `
    'Y:320:C'

Write-Host '== Oracle: datos'
Esperar '100.000 registros cargados' 'SELECT COUNT(*) FROM DATOS;' '100000'
Esperar 'Sin duplicados de CODIGO + SECUENCIA' `
    'SELECT COUNT(*) FROM (SELECT CODIGO, SECUENCIA FROM DATOS GROUP BY CODIGO, SECUENCIA HAVING COUNT(*) > 1);' '0'

Write-Host '== Oracle: package (con ROLLBACK)'
# Los textos con tildes y ñ se arman con UNISTR para no depender de la codificación de la consola.
Esperar 'SP_INSERTAR devuelve un ID de la secuencia, con y sin descripción' @'
DECLARE
  v_id1 NUMBER; v_id2 NUMBER; v_max NUMBER;
BEGIN
  SELECT MAX(ID) INTO v_max FROM DATOS;
  PKG_DATOS.SP_INSERTAR('VERIF-1', 1, SYSTIMESTAMP, 1.5, NULL, v_id1);
  PKG_DATOS.SP_INSERTAR('VERIF-1', 2, SYSTIMESTAMP, 2.5, UNISTR('con descripci\00F3n'), v_id2);
  DBMS_OUTPUT.PUT_LINE(CASE WHEN v_id1 > v_max AND v_id2 > v_id1 THEN 'SI' ELSE 'NO' END);
  ROLLBACK;
END;
/
'@ 'SI'
Esperar 'DESCRIPCION acepta 320 caracteres con tildes y ñ' @'
DECLARE
  v_id NUMBER; v_len NUMBER;
BEGIN
  PKG_DATOS.SP_INSERTAR('VERIF-2', 1, SYSTIMESTAMP, 1, RPAD(UNISTR('\00F1'), 320, UNISTR('\00E1')), v_id);
  SELECT LENGTH(DESCRIPCION) INTO v_len FROM DATOS WHERE ID = v_id;
  DBMS_OUTPUT.PUT_LINE(v_len);
  ROLLBACK;
END;
/
'@ '320'
Esperar 'CODIGO + SECUENCIA repetidos se rechazan (ORA-00001)' @'
DECLARE
  v_id NUMBER;
BEGIN
  PKG_DATOS.SP_INSERTAR('COD-00001', 1, SYSTIMESTAMP, 1, NULL, v_id);
  DBMS_OUTPUT.PUT_LINE('NO');
  ROLLBACK;
EXCEPTION
  WHEN DUP_VAL_ON_INDEX THEN DBMS_OUTPUT.PUT_LINE('SI'); ROLLBACK;
END;
/
'@ 'SI'

Write-Host '== RabbitMQ'
$cola = docker exec $RabbitContainer rabbitmqctl -q list_queues name durable 2>$null |
    ForEach-Object { , ($_.Trim() -split '\s+') } |
    Where-Object { $_[0] -eq 'pesos_especiales' } |
    ForEach-Object { $_[1] }
if ($cola -eq 'true') { Ok 'Cola pesos_especiales existe y es durable' } else { Falla 'Cola pesos_especiales no existe o no es durable' }

# Invoke-WebRequest en vez de curl: en Windows PowerShell 5.1 "curl" es un alias de Invoke-WebRequest.
$credenciales = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('prueba:prueba'))
try {
    $respuesta = Invoke-WebRequest -Uri "http://localhost:$MgmtPort/api/whoami" `
        -Headers @{ Authorization = "Basic $credenciales" } -UseBasicParsing -TimeoutSec 10
    $codigoHttp = [int]$respuesta.StatusCode
} catch {
    $codigoHttp = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
}
if ($codigoHttp -eq 200) {
    Ok "Usuario prueba entra a la consola (http://localhost:$MgmtPort)"
} else {
    Falla "Login en la consola de RabbitMQ (HTTP $codigoHttp)"
}

Write-Host ''
if ($script:Fallas -eq 0) {
    Write-Host 'Entorno listo.'
} else {
    Write-Host "Hubo $($script:Fallas) verificación(es) con falla."
    exit 1
}
