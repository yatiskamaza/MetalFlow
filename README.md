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
- Verificación de empresas por el administrador (CU-10, RD-02).
- Motor de búsqueda paramétrica del catálogo, con paginación (RF-04, RNF-01).
- Flujo de reserva con control de concurrencia y expiración automática
  (RF-05, RD-06).

Pendiente: gestión de cuentas y supervisión (CU-11), publicación de anuncios
(RF-06), proceso micro-ETL (RF-02), frontend y contenedorización completa.

## Estructura de la solución

```
MetalFlow.Dominio          Entidades y reglas de negocio. No depende de nadie.
MetalFlow.Aplicacion       Servicios de caso de uso, objetos de transferencia,
                           excepciones propias y abstracciones de los
                           servicios técnicos.
MetalFlow.Infraestructura  Persistencia (EF Core), repositorios y servicios
                           técnicos (hash y tokens).
MetalFlow.API              Controladores REST, manejador de errores,
                           declaración de la especificación y procesos de
                           ejecución periódica. Único punto de entrada.
```

La dirección de las referencias entre proyectos materializa la inversión de
dependencias: el dominio declara las interfaces de repositorio y es la
infraestructura la que las implementa.

Junto al repositorio genérico conviven dos repositorios especializados. El del
catálogo compone la consulta de búsqueda de forma diferida, de modo que el
filtrado se resuelva en el gestor y no en memoria. El de reservas recupera el
material bajo bloqueo exclusivo, mecanismo que el marco de persistencia no
permite expresar por sus medios ordinarios.

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
dotnet ef database update \
  --project MetalFlow.Infraestructura \
  --startup-project MetalFlow.API
```

El repositorio incluye las migraciones, por lo que no es preciso generarlas.

### 6. Ejecutar la aplicación

```bash
dotnet run --project MetalFlow.API
```

En el arranque se aprovisiona la cuenta del administrador a partir de las
credenciales del paso 3. La operación es idempotente: si la cuenta ya existe, no
se modifica. Se inicia asimismo el proceso periódico de expiración de reservas,
cuyo intervalo se configura en `appsettings.json` bajo
`Reservas:IntervaloExpiracionMinutos` y que por defecto se ejecuta cada quince
minutos.

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
| GET | `/api/v1/materiales` | autenticado | RD-03 |
| GET | `/api/v1/items` | proveedor | CU-04 |
| GET | `/api/v1/items/{id}` | proveedor | CU-04 |
| POST | `/api/v1/items` | proveedor | CU-04 |
| PUT | `/api/v1/items/{id}` | proveedor | CU-04 |
| DELETE | `/api/v1/items/{id}` | proveedor | CU-04 |
| GET | `/api/v1/catalogo` | comprador | CU-07 |
| GET | `/api/v1/catalogo/{id}` | comprador | CU-08 |
| POST | `/api/v1/reservas` | comprador | CU-09 |
| GET | `/api/v1/reservas` | comprador | CU-09 |
| GET | `/api/v1/reservas/recibidas` | proveedor | CU-09 |
| PATCH | `/api/v1/reservas/{id}/cancelacion` | ambas partes | CU-09 |
| PATCH | `/api/v1/reservas/{id}/confirmacion` | proveedor | CU-09 |
| GET | `/api/v1/empresas` | administrador | CU-10 |
| GET | `/api/v1/empresas/{id}` | administrador | CU-10 |
| PATCH | `/api/v1/empresas/{id}/verificacion` | administrador | CU-10 |

Conviene distinguir los extremos de inventario de los de catálogo. Los primeros
operan sobre el material propio de la empresa proveedora y quedan sujetos a la
autorización por pertenencia; los segundos exponen el material publicado por
terceros, con un conjunto de datos distinto que excluye toda información de
contacto hasta la formalización de una reserva.

### Flujo completo de uso

1. Registrar una empresa proveedora y otra compradora.
2. Verificar ambas con el token del administrador
   (`PATCH /api/v1/empresas/{id}/verificacion`). Sin ello no pueden operar.
3. Dar de alta material con la proveedora (`POST /api/v1/items`).
4. Buscarlo con la compradora (`GET /api/v1/catalogo`). El material propio
   queda excluido de los resultados.
5. Reservarlo (`POST /api/v1/reservas`). La respuesta incorpora los datos de
   contacto de ambas partes, que es el objeto de la operación.
6. Confirmar la reserva con la proveedora o cancelarla desde cualquiera de las
   dos. La confirmación retira el material por transmisión; la cancelación lo
   devuelve al catálogo.

### Comprobación del funcionamiento

1. `POST /api/v1/auth/login` con la contraseña alterada — devuelve 401 con un
   mensaje genérico.
2. Cualquier extremo sin token — devuelve 401:
   `curl -k -i https://localhost:7054/api/v1/items`
3. Un ítem perteneciente a otra empresa — devuelve 403, con independencia de
   que la cuenta solicitante ostente el rol requerido.
4. `POST /api/v1/reservas` sobre un ítem ya reservado — devuelve 409.
5. Dos solicitudes simultáneas sobre el mismo ítem — una devuelve 201 y la otra
   409, nunca ambas 201.

## Scripts

La carpeta `scripts/` contiene dos utilidades destinadas exclusivamente al
entorno de desarrollo:

- `generar-datos-prueba.sql` genera un catálogo sintético con dimensiones
  distribuidas conforme a los formatos habituales de la chapa comercial. Los
  registros reciben una ubicación con el prefijo `SINT-`, lo que permite
  identificarlos y eliminarlos.
- `medir-rendimiento.sql` compara el comportamiento de la búsqueda paramétrica
  con y sin el índice compuesto, mediante las estadísticas de entrada y salida
  del gestor.

Los datos sintéticos han de eliminarse antes de cualquier demostración:

```sql
DELETE FROM ITEM_INVENTARIO WHERE Ubicacion LIKE 'SINT-%';
```

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
| `ServicioAdministracion` / `EmpresasController` | RD-02 | 4.4.1 |
| `RepositorioCatalogo` / `MotorBusqueda` | RF-04, RNF-01 | 4.4.1, 4.5.2 |
| `RepositorioReservas` / `ServicioReservas` | RF-05, CT-06 | 4.4.4 |
| `ProcesoDeExpiracionDeReservas` | RF-05 | 4.4.1 |
| `docker-compose.yml` | RNF-02, CT-07 | 4.4.2 |
