using MetalFlow.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Persistencia
// La cadena de conexión no se versiona: se define mediante el gestor de
// secretos de usuario en desarrollo y mediante variables de entorno en el
// despliegue contenedorizado (CT-07).
// ---------------------------------------------------------------------------
var cadenaConexion = builder.Configuration.GetConnectionString("MetalFlowDb")
    ?? throw new InvalidOperationException(
        "No se ha definido la cadena de conexión 'MetalFlowDb'. " +
        "Configúrela mediante los secretos de usuario o la variable de entorno " +
        "ConnectionStrings__MetalFlowDb.");

builder.Services.AddDbContext<MetalFlowDbContext>(opciones =>
    opciones.UseSqlServer(cadenaConexion, sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), null)));

// ---------------------------------------------------------------------------
// API y documentación
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// La documentación interactiva solo se expone en desarrollo: publicarla en
// producción ampliaría innecesariamente la superficie de exposición del
// sistema (OWASP A02, configuración de seguridad defectuosa).
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                       // Documento OpenAPI en /openapi/v1.json
    app.MapScalarApiReference(opciones =>   // Interfaz interactiva en /scalar/v1
        opciones.WithTitle("MetalFlow API")
                .WithTheme(ScalarTheme.BluePlanet));
}

app.UseHttpsRedirection();
app.MapControllers();

// Extremo de comprobación: permite verificar que la API responde y que la
// conexión con la base de datos está operativa.
app.MapGet("/api/v1/estado", async (MetalFlowDbContext db) =>
{
    var conectada = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        servicio = "MetalFlow.API",
        version = "0.1.0",
        baseDeDatos = conectada ? "conectada" : "no disponible"
    });
});

app.Run();