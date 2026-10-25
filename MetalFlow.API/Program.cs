using System.Text;
using MetalFlow.Aplicacion.Abstracciones;
using MetalFlow.Aplicacion.Servicios;
using MetalFlow.Dominio.Repositorios;
using MetalFlow.Infraestructura.Persistencia;
using MetalFlow.Infraestructura.Persistencia.Repositorios;
using MetalFlow.Infraestructura.ServiciosTecnicos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
// la carga masiva (CT-02) y del flujo de reserva (CT-06).
builder.Services.AddDbContext<MetalFlowDbContext>(opciones =>
    opciones.UseSqlServer(cadenaConexion));

// ---------------------------------------------------------------------------
// Acceso a datos y servicios técnicos
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IServicioHash, ServicioHashBCrypt>();
builder.Services.AddSingleton<IProveedorTokens, ProveedorJWT>();

// ---------------------------------------------------------------------------
// Servicios de caso de uso
// ---------------------------------------------------------------------------
builder.Services.AddScoped<ServicioIdentidad>();

// ---------------------------------------------------------------------------
// Autenticación mediante JSON Web Tokens (RF-01, CT-01)
// La validación comprueba la firma, el emisor, el destinatario y la vigencia
// del token. El margen de tolerancia temporal se reduce a cero: el valor por
// defecto de cinco minutos prolongaría la validez efectiva más allá de lo
// declarado.
// ---------------------------------------------------------------------------
var claveDeFirma = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "No se ha definido la clave de firma 'Jwt:Secret'. " +
        "Configúrela mediante los secretos de usuario o la variable de entorno Jwt__Secret.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveDeFirma)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MetalFlow",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MetalFlowClientes",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// API y documentación
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Aprovisionamiento de la cuenta de administrador
// Se ejecuta en el arranque, con un ámbito propio, por cuanto los servicios
// registrados por petición no están disponibles fuera de una. La operación es
// idempotente: no altera la cuenta si esta ya existe.
// ---------------------------------------------------------------------------
using (var ambito = app.Services.CreateScope())
{
    var contexto = ambito.ServiceProvider.GetRequiredService<MetalFlowDbContext>();
    var hash = ambito.ServiceProvider.GetRequiredService<IServicioHash>();
    var registro = ambito.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Inicializacion");

    await InicializadorDeDatos.AprovisionarAdministradorAsync(
        contexto, app.Configuration, hash, registro);
}

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

// El orden es significativo: la autenticación identifica al solicitante y la
// autorización decide si puede acceder al recurso, por lo que debe precederla.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Extremo de comprobación: permite verificar que la API responde y que la
// conexión con la base de datos está operativa.
app.MapGet("/api/v1/estado", async (MetalFlowDbContext db) =>
{
    var conectada = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        servicio = "MetalFlow.API",
        version = "0.2.0",
        baseDeDatos = conectada ? "conectada" : "no disponible"
    });
});

app.Run();
