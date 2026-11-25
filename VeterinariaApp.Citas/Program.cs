using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
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

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

Cita[] citas =
[
    new(1, "Luna", "Consulta general", new DateOnly(2026, 12, 1)),
    new(2, "Rocky", "Vacunacion", new DateOnly(2026, 12, 2))
];
var endpoints = app.MapGroup("/api/citas").RequireAuthorization();
endpoints.MapGet("/", () => Results.Ok(citas));
endpoints.MapGet("/{id:int}", (int id) =>
    citas.FirstOrDefault(c => c.Id == id) is { } cita
        ? Results.Ok(cita)
        : Results.NotFound());

app.Run();
record Cita(int Id, string Mascota, string Motivo, DateOnly Fecha);
