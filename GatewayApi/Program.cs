using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json",
    optional: true, reloadOnChange: false);
// Permite sustituir hosts y puertos mediante variables de entorno al desplegar.
builder.Configuration.AddEnvironmentVariables();
// Direcciones publicas de los servicios. Evita editar siete rutas en Render.
foreach (var ruta in builder.Configuration.GetSection("Routes").GetChildren())
{
    var esCitas = ruta["UpstreamPathTemplate"]?.StartsWith("/gateway/citas/") == true;
    var direccion = builder.Configuration[esCitas ? "Servicios:CitasUrl" : "Servicios:ApiUrl"];
    if (!string.IsNullOrWhiteSpace(direccion))
    {
        if (!Uri.TryCreate(direccion, UriKind.Absolute, out var url) ||
            url.Scheme != "https" || url.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment) ||
            !string.IsNullOrEmpty(url.UserInfo))
            throw new InvalidOperationException("Servicios:ApiUrl y Servicios:CitasUrl deben ser origenes HTTPS, sin rutas ni credenciales.");
        builder.Configuration[$"{ruta.Path}:DownstreamScheme"] = url.Scheme;
        builder.Configuration[$"{ruta.Path}:DownstreamHostAndPorts:0:Host"] = url.Host;
        builder.Configuration[$"{ruta.Path}:DownstreamHostAndPorts:0:Port"] = url.Port.ToString();
    }
    if (builder.Environment.IsProduction() &&
        builder.Configuration[$"{ruta.Path}:DownstreamHostAndPorts:0:Host"] is "localhost" or "127.0.0.1")
        throw new InvalidOperationException("En Production configura Servicios:ApiUrl y Servicios:CitasUrl con las URL publicas.");
}
var clave = builder.Configuration["Jwt:Clave"];
var emisor = builder.Configuration["Jwt:Emisor"];
var audiencia = builder.Configuration["Jwt:Audiencia"];
if (string.IsNullOrWhiteSpace(clave) || Encoding.UTF8.GetByteCount(clave) < 32 ||
    string.IsNullOrWhiteSpace(emisor) || string.IsNullOrWhiteSpace(audiencia))
    throw new InvalidOperationException("Configura Jwt:Clave, Jwt:Emisor y Jwt:Audiencia con secretos o variables de entorno.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = emisor,
            ValidateAudience = true,
            ValidAudience = audiencia,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "email",
            RoleClaimType = "rol"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddOcelot(builder.Configuration);
var app = builder.Build();
app.UseWhen(context => context.Request.Path == "/health", branch =>
    branch.Run(context => context.Response.WriteAsJsonAsync(new { estado = "ok", servicio = "gateway" })));
app.UseAuthentication();
app.UseAuthorization();
await app.UseOcelot();
app.Run();
