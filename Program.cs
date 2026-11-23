using Estoque.Data;
using Estoque.Exceptions;
using Estoque.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EstoqueDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=estoque.db"));

builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<MovimentacaoService>();

builder.Services.AddExceptionHandler<TratadorDeExcecoes>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Trocar por migrations (dotnet ef migrations add Inicial) quando o schema estabilizar.
    scope.ServiceProvider.GetRequiredService<EstoqueDbContext>().Database.EnsureCreated();
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
