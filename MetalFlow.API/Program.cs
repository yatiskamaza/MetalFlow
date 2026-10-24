using MetalFlow.Dominio.Repositorios;
using MetalFlow.Infraestructura.Persistencia;
using MetalFlow.Infraestructura.Persistencia.Repositorios;
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

// Nota: no se habilita la estrategia de reintentos ante fallos transitorios.
// Dicha estrategia resulta incompatible con las transacciones iniciadas de
// forma explícita por la unidad de trabajo, necesarias para la atomicidad de
// la carga masiva (CT-02) y del flujo de reserva (CT-06). Al ejecutarse el
// gestor en el mismo anfitrión, los fallos transitorios de red que justifican
// los reintentos no concurren en este despliegue.
builder.Services.AddDbContext<MetalFlowDbContext>(opciones =>
    opciones.UseSqlServer(cadenaConexion));

// ---------------------------------------------------------------------------
// Acceso a datos
// Se registra únicamente la unidad de trabajo: los repositorios se obtienen a
// través de ella, lo que garantiza que todas las escrituras de un caso de uso
// compartan el mismo contexto y, por tanto, la misma transacción.
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

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
    app.MapOpenApi();
    app.MapScalarApiReference(opciones =>
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
