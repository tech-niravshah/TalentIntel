using System.Reflection;
using Microsoft.OpenApi.Models;
using TalentIntel.Application.Services;
using TalentIntel.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Application Insights uses the configured connection string.
builder.Services.AddApplicationInsightsTelemetry(options =>
    options.ConnectionString = builder.Configuration["Azure:ApplicationInsights:ConnectionString"]);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TalentIntel API", Version = "v1" });

    // Load XML comments for controller and model documentation.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<DocumentUploadService>();
builder.Services.AddScoped<MatchingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
