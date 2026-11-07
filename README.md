# MetalFlow

Prototipo de plataforma web B2B para la comercialización de excedentes y retales
geométricos en pymes metalúrgicas. Trabajo Final de Bàtxelor en Informática,
Universitat Carlemany.

## Estado actual

Fase F3 (desarrollo del backend). Implementado hasta el momento:

- Estructura de la solución en cuatro proyectos conforme al diagrama de componentes.
- Modelo de dominio: 8 entidades y 4 enumeraciones.
- Contexto de persistencia con restricciones, índices y catálogo de materiales.
- Acceso a datos mediante los patrones Repository y Unit of Work.
- Autenticación con JSON Web Tokens y derivación de contraseñas con BCrypt (RF-01).
- Tratamiento homogéneo de errores mediante manejador global de excepciones.
- Gestión manual del inventario con autorización por pertenencia (RF-03).
- Consulta del catálogo normalizado de aleaciones (RD-03).

Pendiente: verificación de empresas, motor de búsqueda paramétrica, proceso
micro-ETL, flujo de reserva, publicación de anuncios, frontend y
contenedorización completa.

## Estructura de la solución

```
MetalFlow.Dominio          Entidades y reglas de negocio. No depende de nadie.
MetalFlow.Aplicacion       Servicios de caso de uso, objetos de transferencia,
                           excepciones propias y abstracciones de los
                           servicios técnicos.
MetalFlow.Infraestructura  Persistencia (EF Core), repositorios y servicios
                           técnicos (hash y tokens).
MetalFlow.API              Controladores REST, manejador de errores y
                           declaración de la especificación. Único punto de
                           entrada.
```

La dirección de las referencias entre proyectos materializa la inversión de
dependencias: el dominio declara las interfaces de repositorio y es la
infraestructura la que las implementa.

## Requisitos previos

- **.NET SDK 10**
- **Docker Desktop**
    - En Windows: con backend WSL2.
    - En macOS con Apple Silicon: con la opción *Use Rosetta for x86_64/amd64
      emulation* activada en Settings → General. La imagen de SQL Server no
      dispone de variante para ARM64 y se ejecuta mediante emulación, lo que
      exige además la directiva `platform: linux/amd64` ya presente en el
      archivo de orquestación.
- **Herramienta de línea de comandos de EF Core**:
  `dotnet tool install --global dotnet-ef`
- **Git**
- **Node.js 24** y npm (necesarios únicamente para el frontend, aún no iniciado)

## Puesta en marcha

### 1. Variables de entorno

```bash
cp .env.example .env
```

Edite el archivo y defina sus propios valores. Tenga en cuenta que:

- La contraseña de SQL Server debe cumplir la política de complejidad (8
  caracteres como mínimo, con mayúsculas, minúsculas, dígitos y símbolos). Si no
  la cumple, el contenedor arranca y se detiene sin llegar a crear la instancia.
- La clave de firma de los tokens debe tener al menos 32 caracteres. Puede
  generarla con `openssl rand -base64 48`.
- La contraseña del administrador debe tener al menos 12 caracteres.
- Si alguna contraseña contiene el carácter `!`, entrecomíllela con comillas
  simples al escribirla en la línea de comandos: en bash y zsh dicho carácter
  activa la expansión del historial y altera el valor.

### 2. Levantar la base de datos

```bash
docker compose up -d metalflow-db
docker compose ps          # debe figurar como healthy
```

La primera ejecución descarga la imagen de SQL Server. Bajo emulación Rosetta,
el contenedor puede tardar cerca de un minuto en pasar de `starting` a `healthy`.
Conviene esperar a dicho estado antes de arrancar la API, que de otro modo
fallaría al intentar aprovisionar la cuenta del administrador.

### 3. Configurar los secretos de la aplicación

Durante el desarrollo, la API no lee el archivo `.env`: obtiene la configuración
sensible del gestor de secretos de usuario de .NET, que la almacena fuera del
directorio del proyecto. Defina los mismos valores del paso 1:

```bash
dotnet user-secrets set "ConnectionStrings:MetalFlowDb" "Server=localhost,1433;Database=MetalFlowDb;User Id=sa;Password=SU_CONTRASENA;TrustServerCertificate=True" --project MetalFlow.API
dotnet user-secrets set "Jwt:Secret" 'SU_CLAVE_DE_FIRMA' --project MetalFlow.API
dotnet user-secrets set "Administrador:Email" 'SU_CORREO_DE_ADMINISTRADOR' --project MetalFlow.API
dotnet user-secrets set "Administrador:Password" 'SU_CONTRASENA_DE_ADMINISTRADOR' --project MetalFlow.API
```

### 4. Confiar el certificado de desarrollo

```bash
dotnet dev-certs https --trust
```

Necesario para acceder a la API por HTTPS en local. En macOS requiere
autorización expresa del sistema.

### 5. Crear el esquema de la base de datos

```bash
dotnet ef migrations add MigracionInicial \
  --project MetalFlow.Infraestructura \
  --startup-project MetalFlow.API

dotnet ef database update \
  --project MetalFlow.Infraestructura \
  --startup-project MetalFlow.API
```

Si el repositorio ya incluye la migración, basta con el segundo comando.

### 6. Ejecutar la aplicación

```bash
dotnet run --project MetalFlow.API
```

En el arranque se aprovisiona la cuenta del administrador a partir de las
credenciales del paso 3. La operación es idempotente: si la cuenta ya existe, no
se modifica.

Direcciones disponibles:

- Documentación interactiva: `https://localhost:7054/scalar/v1`
- Especificación OpenAPI: `https://localhost:7054/openapi/v1.json`
- Comprobación de estado: `https://localhost:7054/api/v1/estado`

## Uso de la API

Salvo el registro y el acceso, todos los extremos exigen un token válido. La
documentación interactiva permite introducirlo una sola vez, al declarar la
especificación el esquema de autenticación correspondiente.

| Método | Ruta | Rol | Caso de uso |
|---|---|---|---|
| POST | `/api/v1/auth/registro` | público | CU-01 |
| POST | `/api/v1/auth/login` | público | CU-02 |
| GET | `/api/v1/materiales` | autenticado | — |
| GET | `/api/v1/items` | proveedor | CU-04 |
| GET | `/api/v1/items/{id}` | proveedor | CU-04 |
| POST | `/api/v1/items` | proveedor | CU-04 |
| PUT | `/api/v1/items/{id}` | proveedor | CU-04 |
| DELETE | `/api/v1/items/{id}` | proveedor | CU-04 |

### Verificación de empresas

Una empresa registrada queda pendiente de verificación y no puede incorporar
material hasta que esta se complete (RD-02). El extremo correspondiente (CU-10)
aún no está implementado, de modo que durante el desarrollo ha de realizarse
directamente sobre la base de datos:

```bash
docker exec -it metalflow-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'SU_CONTRASENA' -C -d MetalFlowDb \
  -Q "UPDATE EMPRESA SET Verificada = 1 WHERE Nif = 'EL_NIF'"
```

### Comprobación del funcionamiento

1. `POST /api/v1/auth/registro` — alta de empresa. Devuelve 201 y queda
   pendiente de verificación.
2. `POST /api/v1/auth/login` — devuelve 200 y el token.
3. `POST /api/v1/auth/login` con la contraseña alterada — devuelve 401 con un
   mensaje genérico.
4. `GET /api/v1/materiales` — devuelve el catálogo de aleaciones.
5. `POST /api/v1/items` — alta de un ítem. Devuelve 201.
6. `PUT` y `DELETE` sobre ese ítem — devuelven 200 y 204 respectivamente; la
   baja es lógica y el ítem pasa a estado retirado.
7. Cualquier extremo sin token — devuelve 401:
   `curl -k -i https://localhost:7054/api/v1/items`
8. Un ítem perteneciente a otra empresa — devuelve 403, con independencia de
   que la cuenta solicitante ostente el rol de proveedor.

## Gestión de la configuración sensible

Ningún secreto figura en los artefactos versionados, en ninguna forma. Las
credenciales se obtienen del gestor de secretos de usuario durante el desarrollo
y de variables de entorno en el despliegue contenedorizado. El archivo de
configuración versionado contiene únicamente parámetros no sensibles.

La cuenta del administrador no se incorpora como dato semilla de la migración,
precisamente para no versionar la derivación de su contraseña: se aprovisiona en
el arranque a partir de la configuración del entorno.

## Trazabilidad con la memoria

| Elemento del código | Requisito | Apartado |
|---|---|---|
| `ItemInventario` y su índice compuesto | RF-04, RNF-01 | 4.5.2 |
| `Usuario` / `UsuarioRol` | RF-01, RD-02 | 4.5.2 |
| `ClasificacionRegulatoria` | RD-01 | 4.4.3 |
| `CargaInventario` / `IncidenciaCarga` | RF-02, RD-04 | 4.4.4 |
| `RepositorioEF` / `UnitOfWork` | CT-02, CT-06 | 4.2 |
| `ServicioIdentidad` / `ProveedorJWT` | RF-01, CT-01 | 4.4.1 |
| `ServicioHashBCrypt` | RNF-03, CT-08 | 4.7 |
| `InicializadorDeDatos` | RD-02 | 4.5.2 |
| `ManejadorGlobalDeExcepciones` | RNF-03 | 4.2, 4.7 |
| `ServicioInventario` / `InventarioController` | RF-03, RD-01 | 4.4.1 |
| `ClaimsPrincipalExtensiones` | RNF-03, CT-09 | 4.7 |
| `docker-compose.yml` | RNF-02, CT-07 | 4.4.2 |
