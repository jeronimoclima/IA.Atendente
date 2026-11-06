using IA.Atendente.Api.Data;
using IA.Atendente.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração dos Serviços da Aplicação (Injeção de Dependência)
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<OpenAiService>();
builder.Services.AddHttpClient<OpenAiService>();
// Adicione junto aos outros serviços
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddHttpClient<WhatsAppService>();

// Configuração dos Controllers e tratamento de ciclos JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();