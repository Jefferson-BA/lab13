using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using VeterinariaApp.Api.Swagger;
using VeterinariaApp.Negocio.Configuracion;
using VeterinariaApp.Datos;
using VeterinariaApp.Datos.Repositorios;
using VeterinariaApp.Negocio;
using VeterinariaApp.Negocio.Servicios;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Veterinaria API · Laboratorio 14", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pegar únicamente el token obtenido en POST /api/auth/login, sin escribir Bearer."
    });
    options.OperationFilter<AutorizacionOperationFilter>();
});
builder.Services.AddOptions<JwtOpciones>()
    .Bind(builder.Configuration.GetSection(JwtOpciones.Seccion))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var jwt = builder.Configuration.GetSection(JwtOpciones.Seccion).Get<JwtOpciones>()
        ?? throw new InvalidOperationException("Falta la configuración JWT. Ejecutar scripts/Configurar-Local.ps1.");
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Emisor,
        ValidateAudience = true,
        ValidAudience = jwt.Audiencia,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
        RequireSignedTokens = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "email",
        RoleClaimType = "rol"
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            // El perfil depende de estos claims, por eso también se exigen al validar.
            if (new[] { "sub", "email", "rol" }.Any(c =>
                string.IsNullOrWhiteSpace(context.Principal?.FindFirst(c)?.Value)))
                context.Fail("Faltan claims obligatorios.");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddDbContext<VeterinariaContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("VeterinariaDB")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:VeterinariaDB.")));
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IMascotaRepository, MascotaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IMascotaService, MascotaService>();
builder.Services.AddAutoMapper(cfg =>
{
    var key = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(key)) cfg.LicenseKey = key;
}, typeof(MappingProfile));

var app = builder.Build();
// Detecta mapeos incompletos antes de recibir la primera petición.
app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.Run();
