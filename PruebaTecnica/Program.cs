using System.Reflection;
using PruebaTecnica.Infrastructure;
using PruebaTecnica.Repositories;
using PruebaTecnica.Services;


var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Manejo global de excepciones
builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Infrastructure
builder.Services.AddSingleton<OracleConnectionFactory>();
builder.Services.AddSingleton<IRabbitMqPublisher,RabbitMqPublisher>();
// Repositories
builder.Services.AddScoped<IDatosRepository, DatosRepository>();
// Services
builder.Services.AddScoped<IDatosService, DatosService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath = Path.Combine(
        AppContext.BaseDirectory,
        xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Manejo de excepciones
app.UseExceptionHandler();

app.UseStatusCodePages();

// Documentación
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS
app.UseHttpsRedirection();

// Endpoints
app.MapControllers();

await app.RunAsync();