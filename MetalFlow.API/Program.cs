using System.Text;
using MetalFlow.API.Middleware;
using MetalFlow.Aplicacion.Abstracciones;
using MetalFlow.Aplicacion.Servicios;
using MetalFlow.Dominio.Repositorios;
using MetalFlow.Infraestructura.Persistencia;
using MetalFlow.Infraestructura.Persistencia.Repositorios;
using MetalFlow.Infraestructura.ServiciosTecnicos;
using MetalFlow.API.Documentacion;
using MetalFlow.API.Tareas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
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
builder.Services.AddScoped<IRepositorioCatalogo, RepositorioCatalogo>();
builder.Services.AddScoped<MotorBusqueda>();

// ---------------------------------------------------------------------------
// Servicios de caso de uso
// ---------------------------------------------------------------------------
builder.Services.AddScoped<ServicioIdentidad>();
builder.Services.AddScoped<ServicioInventario>();
builder.Services.AddScoped<ServicioAdministracion>();
builder.Services.AddScoped<IRepositorioReservas, RepositorioReservas>();
builder.Services.AddScoped<ServicioReservas>();
builder.Services.AddScoped<ServicioExpiracionReservas>();
builder.Services.AddHostedService<ProcesoDeExpiracionDeReservas>();

// ---------------------------------------------------------------------------
// Tratamiento homogéneo de errores (apartado 4.2; Tabla 23, OWASP A02 y A10)
// El manejador traduce las excepciones a códigos de estado y garantiza que
// ninguna respuesta incorpore trazas ni detalles de la implementación.
// ---------------------------------------------------------------------------
builder.Services.AddExceptionHandler<ManejadorGlobalDeExcepciones>();
builder.Services.AddProblemDetails();

// ---------------------------------------------------------------------------
// API y documentación
//
// La personalización de las respuestas de validación se encadena a la propia
// inscripción de los controladores. El orden resulta determinante: dicha
// inscripción registra la configuración predeterminada del comportamiento de
// la API, que asigna su propia fábrica de respuestas sin comprobar si existe
// una previa. Una configuración declarada con anterioridad quedaría, por
// tanto, sobrescrita sin producir error alguno.
// ---------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(opciones =>
    {
        // La validación de los objetos de entrada la resuelve el propio marco
        // a partir de las anotaciones declaradas en los objetos de
        // transferencia, sin que llegue a producirse excepción alguna; dichas
        // respuestas no pasan, por tanto, por el manejador global. Se
        // personaliza aquí su composición para que el título y la estructura
        // coincidan con los de las restantes respuestas de error del sistema,
        // pues de otro modo la interfaz habría de contemplar un formato y un
        // idioma distintos según el origen del error. El detalle de cada campo
        // procede de las anotaciones de los objetos de transferencia.
        opciones.InvalidModelStateResponseFactory = contexto =>
        {
            var problema = new ValidationProblemDetails(contexto.ModelState)
            {
                Title = "Se han detectado errores de validación",
                Detail = "Uno o varios campos de la solicitud no son válidos. " +
                         "Consulte el detalle correspondiente a cada campo.",
                Status = StatusCodes.Status400BadRequest,
                Instance = contexto.HttpContext.Request.Path
            };

            problema.Extensions["traceId"] = contexto.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problema)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddOpenApi(opciones =>
    opciones.AddDocumentTransformer<TransformadorDeSeguridadOpenApi>());

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

// El manejador de excepciones debe situarse al inicio del canal de peticiones,
// de modo que alcance a los errores producidos en cualquier componente
// posterior. Sustituye a la página de diagnóstico que el marco habilita por
// defecto en desarrollo, lo que garantiza un comportamiento uniforme en todos
// los entornos y evita que las trazas se expongan por configuración.
app.UseExceptionHandler();

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
        version = "0.3.0",
        baseDeDatos = conectada ? "conectada" : "no disponible"
    });
});

app.Run();
