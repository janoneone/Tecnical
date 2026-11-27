# Prueba técnica Backend .NET

API REST para la administración de registros de la tabla `DATOS` en Oracle y la publicación de notificaciones de pesos especiales en RabbitMQ.

## Requisitos

- .NET 8 SDK.
- Docker y Docker Compose, con al menos 4 GB de RAM asignados.
- Entorno Oracle y RabbitMQ proporcionado con la prueba técnica.

## Ejecución

Desde la carpeta del entorno entregado:

```powershell
docker compose up -d
docker compose ps
powershell -ExecutionPolicy Bypass -File .\scripts\verificar.ps1
```

Esperar a que ambos servicios estén saludables y el script indique `Entorno listo.`

Ejecutar `01_Optimizar_PKG_DATOS.sql` conectado como usuario `prueba` al servicio `FREEPDB1`, utilizando SQL*Plus o el modo de ejecución de scripts del cliente Oracle. El archivo reemplaza la especificación y el cuerpo del package sin eliminar los registros existentes. Verificar que el package y su cuerpo queden válidos.

```sql
SELECT object_name, object_type, status
FROM user_objects
WHERE object_name = 'PKG_DATOS';
```

Desde la carpeta que contiene `PruebaTecnica.csproj`, configurar las contraseñas locales del entorno de prueba:

```powershell
dotnet user-secrets set "Oracle:Password" "prueba"
dotnet user-secrets set "RabbitMQ:Password" "prueba"
dotnet restore
dotnet build
dotnet dev-certs https --trust
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --no-launch-profile --urls "https://localhost:7059"
```

Las demás opciones de conexión se encuentran en `appsettings.json`. Las contraseñas también pueden configurarse mediante las variables de entorno `Oracle__Password` y `RabbitMQ__Password`.

## Ejecución de la API mediante Docker

Con Oracle y RabbitMQ iniciados y el script SQL de optimización aplicado, construir la imagen desde el directorio que contiene el Dockerfile:

```powershell
docker build -t prueba-tecnica-api:1.0 .
docker network ls
```

La API debe conectarse a la misma red Docker que Oracle y RabbitMQ. En el entorno utilizado, la red es `prueba-tecnica-backend_default`. Dentro de esta red se utilizan los nombres `prueba-oracle` y `prueba-rabbitmq` en lugar de `localhost`.

Ejecutar el contenedor de la API:

```powershell
docker run -d `
  --name prueba-api `
  --network prueba-tecnica-backend_default `
  -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e Oracle__Host=prueba-oracle `
  -e Oracle__Port=1521 `
  -e Oracle__ServiceName=FREEPDB1 `
  -e Oracle__UserName=prueba `
  -e Oracle__Password=prueba `
  -e RabbitMQ__HostName=prueba-rabbitmq `
  -e RabbitMQ__Port=5672 `
  -e RabbitMQ__UserName=prueba `
  -e RabbitMQ__Password=prueba `
  -e RabbitMQ__VirtualHost=/ `
  -e RabbitMQ__QueueName=pesos_especiales `
  prueba-tecnica-api:1.0
```

Las variables de entorno corresponden a las claves definidas en `appsettings.json`. Las credenciales son exclusivas del entorno local proporcionado con la prueba técnica.

La API utiliza el puerto 8080 y el entorno Development para habilitar Swagger.

Si la red Docker tiene un nombre diferente, reemplazar el valor del parámetro `--network` por el nombre correspondiente.

Verificar la ejecución:

```powershell
docker ps
docker logs prueba-api
```

- Swagger UI: http://localhost:8080/swagger/index.html
- Documento OpenAPI: http://localhost:8080/swagger/v1/swagger.json

Para detener y eliminar únicamente el contenedor de la API:

```powershell
docker stop prueba-api
docker rm prueba-api
```

El despliegue local utiliza HTTP; para producción se requiere una configuración de HTTPS apropiada.

## Documentación

- Swagger UI: https://localhost:7059/swagger/index.html
- Documento OpenAPI: https://localhost:7059/swagger/v1/swagger.json
- Consola RabbitMQ: http://localhost:15672

La documentación se genera mediante Swashbuckle y los comentarios XML del proyecto.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/datos?pagina=1` | Listado paginado de 20 registros. |
| GET | `/api/datos/{id}` | Consulta por identificador. |
| POST | `/api/datos` | Creación y devolución del ID generado. |
| PATCH | `/api/datos/{id}/descripcion` | Actualización exclusiva de la descripción. |
| DELETE | `/api/datos/{id}` | Eliminación del registro. |

Ejemplo de creación:

```json
{
  "codigo": "COD-PRUEBA-001",
  "secuencia": 1,
  "fechaRecepcion": "2026-11-26T12:00:00",
  "peso": 150.5,
  "descripcion": "Registro de prueba"
}
```

La combinación de código y secuencia debe ser única. Una creación exitosa devuelve HTTP 201, el ID generado y la ubicación del recurso.

Para eliminar el contenido de la descripción, enviar al endpoint PATCH:

```json
{"descripcion": null}
```

## Diagnóstico y optimización de rendimiento

Al revisar `SP_LISTAR`, se identificó que la función `FN_TOTAL_POR_CODIGO` ejecutaba un `COUNT(*)` sobre la tabla `DATOS` por cada fila del listado. Con una carga inicial de 100.000 registros, esto genera trabajo repetitivo en Oracle.

La llamada se reemplazó por una función analítica:

```sql
COUNT(*) OVER (PARTITION BY d.CODIGO) AS TOTAL_CODIGO
```

Esto permite calcular los totales dentro de la consulta, evitando las llamadas repetidas a la función PL/SQL. También se eliminó la conversión de `FECHARECEPCION` mediante `TO_CHAR` y se utiliza el ordenamiento directo:

```sql
ORDER BY d.FECHARECEPCION DESC, d.ID DESC
```

El ID actúa como segundo criterio para obtener un orden determinista cuando existen fechas iguales. Se incorporó `SP_LISTAR_PAGINADO`, que calcula el total de registros y utiliza `OFFSET` y `FETCH NEXT` para devolver 20 filas. El total por código se calcula antes de aplicar la paginación.

El diagnóstico se basa en la revisión del SQL original; no se incluyen mediciones comparativas de tiempos de ejecución.

## Diseño y manejo de errores

La API utiliza controladores, servicios, repositorios y DTOs. El acceso a Oracle se realiza mediante ODP.NET y procedimientos del package `PKG_DATOS`, con parámetros enlazados por nombre. Las operaciones de escritura utilizan transacciones explícitas.

La actualización emplea un DTO específico que rechaza propiedades JSON desconocidas y valida una longitud máxima de 320 caracteres. El procedimiento `SP_ACTUALIZAR_DESCRIPCION` modifica únicamente esa columna.

Los errores se representan mediante ProblemDetails. El manejo global traduce los errores de registro inexistente a HTTP 404, duplicidad a HTTP 409, solicitudes inválidas a HTTP 400 y errores inesperados a HTTP 500, sin exponer detalles internos de Oracle.

## Notificaciones RabbitMQ

Cuando se crea un registro con peso estrictamente superior a 100,64, se publica un mensaje después de confirmar la transacción en Oracle. Las actualizaciones no generan notificaciones.

El mensaje contiene únicamente el ID generado, utiliza `application/json`, es persistente y se envía a la cola durable `pesos_especiales` con confirmación de publicación habilitada.

Si RabbitMQ no está disponible, se conserva el registro en Oracle, se registra el error y se devuelve HTTP 201 con el ID generado. La publicación tiene un límite de espera de 10 segundos mediante un token de cancelación. No existe un mecanismo de reintento posterior; por lo tanto, una notificación puede quedar sin entregar. Una mejora futura sería implementar Transactional Outbox con reintentos y procesamiento idempotente.

## Pruebas automatizadas

El proyecto `PruebaTecnica.Tests` contiene pruebas unitarias de la capa de servicios, incluyendo validaciones de entrada, inserción, publicación de pesos especiales y manejo de errores del repositorio y RabbitMQ.

Desde el directorio de la solución:

```powershell
dotnet test
```

La ejecución observada en el Explorador de pruebas de Visual Studio finalizó con 12 pruebas superadas, sin errores ni pruebas omitidas. Las pruebas unitarias complementan las verificaciones de integración con Oracle y RabbitMQ.

## Verificación funcional

| Caso | Resultado observado |
|---|---|
| Listado paginado desde Docker | HTTP 200; tamaño de página de 20 registros. |
| Creación de registro | HTTP 201 con ID generado y encabezado Location. |
| Código y secuencia duplicados | HTTP 409. |
| Omisión de campos obligatorios | HTTP 400. |
| Creación con RabbitMQ detenido | HTTP 201; registro 222263 conservado en Oracle. |
| Creación después de restablecer RabbitMQ | Registro 222264 guardado y mensaje publicado. |
| Integración desde Docker | Registro 222265 guardado en Oracle y mensaje con el mismo ID en RabbitMQ. |

En la prueba de integración desde Docker, el registro `COD-DOCKER-001`, con peso 180,50, quedó almacenado en Oracle y se verificó el siguiente mensaje en la cola `pesos_especiales`:

```json
{"id":222265}
```

El mensaje presenta `content_type` igual a `application/json` y `delivery_mode` igual a 2 (persistente).

