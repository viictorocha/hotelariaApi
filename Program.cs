using Microsoft.EntityFrameworkCore;
using HotelariaApi.Data;
using HotelariaApi.Domain;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("://")) {
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    int port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;
    connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<HotelDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotelaria API v1");
    c.RoutePrefix = "swagger";
});

// Endpoints
app.MapGet("/", () => "HotelariaPro API v1 - Online");

app.MapGet("/quartos", async (HotelDbContext db) => await db.Quartos.ToListAsync());

app.MapPost("/quartos", async (Quarto quarto, HotelDbContext db) => {
    db.Quartos.Add(quarto);
    await db.SaveChangesAsync();
    return Results.Created($"/quartos/{quarto.Id}", quarto);
});

app.Run();