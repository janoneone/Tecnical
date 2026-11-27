# Prueba técnica — Desarrollador/a Backend .NET

Gracias por participar. Esta prueba consiste en construir una API REST en **.NET 8** sobre una base de datos **Oracle** que ya está creada y cargada, y que además publica mensajes en **RabbitMQ**.

## Plazo

Tienes **48 horas corridas** desde que recibes este documento.

**Fecha y hora límite de entrega:** `26/11/2026, 12:00`.

Se considera entregado lo que esté en el último commit (o en el zip enviado) antes de esa hora.

> Te recomendamos levantar el entorno (sección siguiente) apenas recibas este documento: la primera descarga de las imágenes Docker puede tardar, según tu conexión.

---

## 1. Preparar el entorno

### Requisitos

- **Docker** (Docker Desktop, OrbStack, Colima, Podman con compose, etc.) con al menos **4 GB de RAM** asignados.
  - Se descargan unos **1,3 GB** de imágenes (ocupan cerca de 6 GB en disco).
  - Las imágenes funcionan de forma nativa en **x86_64 (Intel/AMD)** y en **arm64 (Apple Silicon)**.
- **.NET 8 SDK**.
- Para el script de verificación: **bash** (macOS, Linux, WSL o Git Bash) o **PowerShell** (Windows PowerShell 5.1 o PowerShell 7 en cualquier sistema).

### Levantar

Desde esta carpeta:

```bash
docker compose up -d
```

```bash
docker compose ps
```

Espera a que ambos servicios aparezcan como **`healthy`**. En el primer arranque Oracle crea los objetos y carga 100.000 registros; tarda unos minutos.

Luego verifica que todo quedó bien, con bash:

```bash
./scripts/verificar.sh
```

o con PowerShell (en Windows):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verificar.ps1
```

Con PowerShell 7 usa `pwsh -File ./scripts/verificar.ps1`. Ambos scripts hacen las mismas verificaciones y deben terminar con `Entorno listo.`

### Conexiones

| Servicio | Datos |
|---|---|
| Oracle | Host `localhost`, puerto `1521`, servicio `FREEPDB1`, usuario `prueba`, clave `prueba` |
| Cadena de conexión .NET (ODP.NET) | `User Id=prueba;Password=prueba;Data Source=localhost:1521/FREEPDB1` |
| RabbitMQ (AMQP) | `amqp://prueba:prueba@localhost:5672/` (vhost `/`) |
| Consola web de RabbitMQ | <http://localhost:15672> (usuario `prueba`, clave `prueba`) |

Para abrir SQL\*Plus dentro del contenedor:

```bash
docker exec -it prueba-oracle sqlplus prueba/prueba@//localhost/FREEPDB1
```

También puedes usar el cliente que prefieras (DBeaver, SQL Developer, DataGrip, la extensión de Oracle para VS Code, etc.).

Si un puerto está ocupado, copia `.env.example` a `.env` y cámbialo ahí.

### Volver a empezar desde cero

Los scripts de creación se ejecutan **solo en el primer arranque**. Para borrar todo (incluidos los datos) y recrearlo:

```bash
docker compose down -v
```

```bash
docker compose up -d
```

---

## 2. Lo que ya existe en la base de datos

Los scripts están en [`oracle/sql/`](oracle/sql/); puedes leerlos y modificarlos.

### Tabla `DATOS`

| Columna | Tipo | Notas |
|---|---|---|
| `ID` | `NUMBER` | Clave primaria. Se obtiene de la secuencia `SEQ_DATOS`. |
| `CODIGO` | `VARCHAR2(32)` | Obligatorio. |
| `SECUENCIA` | `INTEGER` | Obligatorio. |
| `FECHARECEPCION` | `TIMESTAMP` | Obligatorio. |
| `PESO` | `NUMBER(19,4)` | Obligatorio. |
| `DESCRIPCION` | `VARCHAR2(320 CHAR)` | Opcional. |

- Índice único `UX_DATOS_COD_SEC` sobre (`CODIGO`, `SECUENCIA`): un código no puede repetir secuencia.
- Secuencia `SEQ_DATOS` para el `ID`. Puede haber saltos en la numeración; es normal.
- Carga inicial: **100.000 registros**.

### Package `PKG_DATOS`

| Procedimiento | Qué hace |
|---|---|
| `SP_INSERTAR(p_codigo, p_secuencia, p_fecha_recepcion, p_peso, p_descripcion, p_id OUT)` | Inserta un dato y devuelve en `p_id` el ID tomado de la secuencia. |
| `SP_OBTENER(p_id, p_cursor OUT)` | Devuelve el dato con ese ID (cursor vacío si no existe). |
| `SP_LISTAR(p_codigo, p_cursor OUT)` | Lista los datos, del más reciente al más antiguo, con la columna `TOTAL_CODIGO` (cantidad de registros del mismo código). `p_codigo` es un filtro opcional. |
| `SP_ACTUALIZAR(p_id, p_codigo, p_secuencia, p_fecha_recepcion, p_peso, p_descripcion)` | Actualiza un dato. Error `-20001` si no existe. |
| `SP_ELIMINAR(p_id)` | Elimina un dato. Error `-20001` si no existe. |

### Cola `pesos_especiales`

Cola durable en RabbitMQ, ya creada.

---

## 3. Tareas

1. **API REST en .NET 8** que use la base de datos a través del package `PKG_DATOS`. Usa `Oracle.ManagedDataAccess.Core`; puedes trabajar con ADO.NET, Dapper o EF Core, a tu elección.

2. **Rendimiento.** El `SELECT` de `SP_LISTAR` tiene un problema de rendimiento. **Detéctalo y corrígelo.** Entrega el script SQL con la corrección y explica en el README cómo lo diagnosticaste y por qué tu corrección lo resuelve.

Al revisar el procedimiento almacenado SP_LISTAR del package PKG_DATOS, identifiqué que el principal problema de rendimiento estaba en el uso de la función FN_TOTAL_POR_CODIGO, la cual realiza un select count(*) sobre la tabla datos por cada registro del listado.
Considerando que la base de datos contiene inicialmente 100.000 registros, esto podria llegar a generar una gran cantidad de consultas repetitivas, aumentando el tiempo de ejecución y el consumo de recursos de la base
Para solucionar este problema, reemplacé la llamada a la función por una función analítica COUNT(*) OVER (PARTITION BY d.CODIGO) AS TOTAL_CODIGO
asi el total de los registros asociados a cada código se calcula dentro de la misma consulta, evitando ejecutar consultas adicionales por cada fila y manteniendo el resultado esperado y optimizando recursos.
El ordenamiento utilizaba to_char para convertir el campo FECHARECEPCION a texto. No es necesario hacer esa conversion, ya que oracle permite ordenar directamente por un campo de tipo timestamp.
para eso cambie el ordenamiento de la siguiente manera: ORDER BY d.FECHARECEPCION DESC, d.ID DESC
Ademas use el campo ID como segundo criterio para el ordenamiento y asi garantizar que cuando existan registros con la misma fecha de recepcion no generen conflicto al momento de la paginacion.
Con los cambios aplicados se logra reducir el procesamiento innecesario de la base , mantener la información entregada por el procedimiento original y 
mejorar el rendimiento del listado sin alterar las reglas de negocio existentes.


3. **Listado paginado.** El endpoint que lista los datos recibe **solo el número de página** y devuelve **20 registros por página** (cantidad fija). Debe devolver los mismos datos que `SP_LISTAR` (incluido `TOTAL_CODIGO`), junto con la información necesaria para paginar: página actual, tamaño de página, total de registros y total de páginas.

4. **Resto de las operaciones:** consultar un dato por ID, crear, actualizar y eliminar. Al crear, la respuesta debe incluir el ID generado por la secuencia.

5. **Documentación de la API.**
   - Comentarios XML en controladores (o endpoints), acciones y DTOs, que se vean reflejados en el documento OpenAPI.
   - La API debe **exponer el documento OpenAPI en un endpoint** (la ruta la eliges tú; indícala en el README).
   - La API debe ofrecer una **interfaz web para explorar y probar los endpoints**, con **Swagger UI o Scalar**, a tu elección.
   - Ambos deben estar disponibles al ejecutar la API como indica tu README.

   > En .NET 8 el documento OpenAPI se genera con **Swashbuckle** o **NSwag** (la generación nativa con `AddOpenApi()` llegó en .NET 9). Scalar solo muestra el documento, así que si eliges Scalar debes combinarlo con uno de esos generadores. Ninguna de las dos opciones da puntaje adicional.

6. **Calidad del código.** Comentarios donde aporten y validación de los datos de entrada.

7. **Notificación de pesos especiales.** Cada vez que se **crea** un dato con **`Peso` mayor que 100,64**, la API debe publicar un mensaje en la cola **`pesos_especiales`** con el **ID del dato creado**.
   - Formato del mensaje: JSON, por ejemplo `{"id": 100123}`, con `content_type` `application/json` y persistente.
   - Solo se publica si el dato quedó efectivamente guardado.
   - Aplica solo a las creaciones, no a las actualizaciones.
   - Explica en el README qué hace tu API si RabbitMQ no está disponible en ese momento.

8. **Actualización restringida.** Mediante la actualización **solo se puede modificar `Descripcion`**. Cualquier intento de modificar otro campo debe ser rechazado y no debe llegar a la base de datos. La descripción es opcional (se puede borrar) y admite hasta 320 caracteres.
### 8. Actualización restringida
Se implemento un DTO específico para la actualización de la descripción, evitando utilizar la entidad completa en la solicitud.
Utilicé JsonUnmappedMemberHandling.Disallow para rechazar cualquier propiedad que no esté definida en el DTO. De esta manera, si se intenta modificar el código, la secuencia, el peso o la fecha de recepción, la API responde con un HTTP 400 antes de ejecutar el procedimiento almacenado.
También incorporé la validación de un máximo de 320 caracteres mediante StringLength, permitiendo que la descripción sea nula en caso de que se quiera eliminar su contenido.
A nivel de base de datos, incorporé el procedimiento `SP_ACTUALIZAR_DESCRIPCION` dentro del package `PKG_DATOS`, el cual actualiza exclusivamente la columna DESCRIPCION.
Realicé pruebas desde Swagger y posteriormente consulté los registros directamente en Oracle para comprobar que los demás campos mantuvieran sus valores originales y 
que las solicitudes inválidas fueran rechazadas correctamente.

9. **Códigos HTTP.** Cada endpoint debe responder con el **código HTTP semánticamente correcto** para cada caso de uso (éxito, datos inválidos, recurso inexistente, conflicto, error inesperado, etc.).
   - Todos los códigos posibles de cada endpoint deben estar declarados en el documento OpenAPI.
   - Los errores se devuelven con el formato `ProblemDetails` (RFC 9457).

### Prioridades

Si el tiempo no te alcanza para todo, te sugerimos este orden:

1. **Núcleo:** tareas 1, 2, 3, 4, 8 y 9.
2. **Luego:** tareas 7, 5 y 6.
3. **Extras (opcionales, suman puntos):** tests automatizados y un `Dockerfile` para la API.

Evaluamos mejor un núcleo sólido que todo hecho a medias. Si algo queda pendiente, cuéntanos en el README cómo lo habrías resuelto.

---

## 4. Entregables

Un **repositorio Git** (enlace con acceso para nosotros) o un **archivo zip** con:

1. **El código fuente** de la API.
2. **El script SQL** con la corrección del `SELECT` (y cualquier otro cambio que hayas hecho en la base de datos).
3. **Un `README.md`** con:
   - cómo ejecutar la API;
   - la URL del documento OpenAPI y la de la interfaz (Swagger UI o Scalar);
   - el diagnóstico del problema de rendimiento y la explicación de la corrección;
   - las decisiones de diseño que quieras destacar (por ejemplo, qué pasa si RabbitMQ falla);
   - lo que haya quedado pendiente y cómo lo resolverías.

## 5. Qué vamos a mirar

- Que la API funcione contra el entorno entregado y cumpla lo pedido.
- El diagnóstico y la corrección del problema de rendimiento.
- El diseño de la API: rutas, verbos, códigos HTTP, validaciones y manejo de errores.
- La documentación: comentarios, documento OpenAPI y README.
- La claridad y organización del código.

Si algo del enunciado no está claro, toma una decisión razonable y déjala escrita en el README.

¡Éxito!
