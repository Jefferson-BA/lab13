using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VeterinariaApp.Datos;
using VeterinariaApp.Datos.Repositorios;
using VeterinariaApp.Negocio;
using VeterinariaApp.Negocio.Servicios;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<VeterinariaContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("VeterinariaDB")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:VeterinariaDB.")));
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IMascotaRepository, MascotaRepository>();
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.Run();
