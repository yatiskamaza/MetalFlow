# MetalFlow

Prototipo de plataforma web B2B para la comercialización de excedentes y retales
geométricos en pymes metalúrgicas. Trabajo Final de Bàtxelor en Informática,
Universitat Carlemany.

## Estado actual

Fase F3 (desarrollo del backend). Implementado hasta el momento:

- Estructura de la solución en cuatro proyectos conforme al diagrama de componentes.
- Modelo de dominio: 8 entidades y 4 enumeraciones.
- Contexto de persistencia con restricciones, índices y datos semilla.

## Estructura de la solución

```
MetalFlow.Dominio          Entidades y reglas de negocio. No depende de nadie.
MetalFlow.Aplicacion       Servicios de caso de uso.
MetalFlow.Infraestructura  Persistencia (EF Core), repositorios y servicios técnicos.
MetalFlow.API              Controladores REST. Único punto de entrada.
```

La dirección de las referencias entre proyectos materializa la inversión de
dependencias: el dominio declara las interfaces de repositorio y es la
infraestructura la que las implementa.

## Requisitos previos

- .NET SDK 10
- Docker Desktop (con backend WSL2 en Windows)
- Herramienta de EF Core: `dotnet tool install --global dotnet-ef`

## Puesta en marcha

**1. Variables de entorno.** Copie la plantilla y defina sus valores:

```bash
cp .env.example .env
```

La contraseña de SQL Server debe cumplir la política de complejidad (8 caracteres
como mínimo, con mayúsculas, minúsculas, dígitos y símbolos). Si no la cumple, el
contenedor arranca y se detiene sin crear la instancia.

**2. Levantar la base de datos.**

```bash
docker compose up -d metalflow-db
docker compose ps          # debe figurar como healthy
```

**3. Crear la migración inicial y aplicarla.**

```bash
dotnet ef migrations add MigracionInicial \
  --project src/MetalFlow.Infraestructura \
  --startup-project src/MetalFlow.API

dotnet ef database update \
  --project src/MetalFlow.Infraestructura \
  --startup-project src/MetalFlow.API
```

**4. Comprobar el esquema.** Conéctese a `localhost,1433` con las credenciales
del archivo `.env` y verifique que existen las nueve tablas y que el índice
`IX_ITEM_BUSQUEDA_PARAMETRICA` figura sobre `ITEM_INVENTARIO`.

## Trazabilidad con la memoria

| Elemento del código | Requisito | Apartado de la memoria |
|---|---|---|
| `ItemInventario` y su índice compuesto | RF-04, RNF-01 | 4.5.2 |
| `Usuario` / `UsuarioRol` | RF-01, RD-02 | 4.5.2 |
| `ClasificacionRegulatoria` | RD-01 | 4.4.3 |
| `CargaInventario` / `IncidenciaCarga` | RF-02, RD-04 | 4.4.4 |
| Datos semilla del administrador | RD-02 | 4.5.2 |
| `docker-compose.yml` | RNF-02, CT-07 | 4.4.2 |
