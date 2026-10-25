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

Pendiente: gestión de inventario, motor de búsqueda, proceso micro-ETL, flujo de
reserva, frontend y contenedorización completa.

## Estructura de la solución

```
MetalFlow.Dominio          Entidades y reglas de negocio. No depende de nadie.
MetalFlow.Aplicacion       Servicios de caso de uso, objetos de transferencia
                           y abstracciones de los servicios técnicos.
MetalFlow.Infraestructura  Persistencia (EF Core), repositorios y servicios
                           técnicos (hash y tokens).
MetalFlow.API              Controladores REST. Único punto de entrada.
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

### 3. Configurar los secretos de la aplicación

Durante el desarrollo, la API no lee el archivo `.env`: obtiene la configuración
sensible del gestor de secretos de usuario de .NET, que la almacena fuera del
directorio del proyecto. Defina los mismos valores del paso 1:

```bash
dotnet user-secrets set "ConnectionStrings:MetalFlowDb" "Server=localhost,1433;Database=MetalFlowDb;User Id=sa;Password=SU_CONTRASENA;TrustServerCertificate=True" --project MetalFlow.API
dotnet user-secrets set "Jwt:Secret" 'SU_CLAVE_DE_FIRMA' --project MetalFlow.API
dotnet user-secrets set "Administrador:Email" "admin@metalflow.local" --project MetalFlow.API
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
- Comprobación de estado: `https://localhost:7054/api/v1/estado`

### 7. Verificar el funcionamiento

Desde la documentación interactiva:

1. `POST /api/v1/auth/registro` — alta de una empresa y su cuenta de acceso.
   Devuelve 201 y la empresa queda pendiente de verificación.
2. `POST /api/v1/auth/login` — con esas credenciales. Devuelve 200 y el token.
3. `POST /api/v1/auth/login` — con la contraseña alterada. Debe devolver 401 con
   un mensaje genérico.

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
| `docker-compose.yml` | RNF-02, CT-07 | 4.4.2 |
