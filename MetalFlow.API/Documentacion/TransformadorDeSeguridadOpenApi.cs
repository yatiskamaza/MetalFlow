using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MetalFlow.API.Documentacion;

/// <summary>
/// Declara el esquema de autenticación en el documento OpenAPI.
///
/// La especificación generada describe los extremos y sus esquemas de datos,
/// pero no el mecanismo de autenticación: este no se infiere de los atributos
/// de autorización de los controladores. Sin dicha declaración, la
/// especificación resulta incompleta —un consumidor no sabría cómo acreditarse
/// ante la API— y las herramientas de documentación interactiva no ofrecen el
/// campo correspondiente para el token.
///
/// El esquema se declara como requisito global, dado que la totalidad de los
/// extremos exige autenticación con la sola excepción de los de registro y
/// acceso.
/// </summary>
public class TransformadorDeSeguridadOpenApi : IOpenApiDocumentTransformer
{
    private const string NombreDelEsquema = "Bearer";

    private readonly IAuthenticationSchemeProvider _esquemas;

    public TransformadorDeSeguridadOpenApi(IAuthenticationSchemeProvider esquemas)
        => _esquemas = esquemas;

    public async Task TransformAsync(
        OpenApiDocument documento, OpenApiDocumentTransformerContext contexto,
        CancellationToken ct)
    {
        var registrados = await _esquemas.GetAllSchemesAsync();

        // Solo se declara si la autenticación se encuentra efectivamente
        // configurada, de modo que el documento no describa un mecanismo
        // inexistente.
        if (registrados.All(e => e.Name != NombreDelEsquema))
            return;

        var esquema = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Token obtenido mediante POST /api/v1/auth/login. " +
                "Se transmite en la cabecera de autorización y su vigencia es limitada."
        };

        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes[NombreDelEsquema] = esquema;

        // En la versión 2 de la biblioteca, los requisitos de seguridad
        // referencian el esquema por su identificador dentro del documento, en
        // lugar de incorporar una copia de su definición.
        documento.Security ??= new List<OpenApiSecurityRequirement>();
        documento.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(NombreDelEsquema, documento)] = []
        });
    }
}
