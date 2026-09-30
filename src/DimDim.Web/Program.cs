using System.Globalization;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using DimDim.Web.Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1) MONITORAMENTO - Application Insights
//    A connection string vem da App Setting APPLICATIONINSIGHTS_CONNECTION_STRING
//    (configurada pelo script scripts/01-criar-recursos.sh). Sem ela (ex.: rodando
//    localmente), o monitoramento simplesmente não é ativado.
// ---------------------------------------------------------------------------
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

// ---------------------------------------------------------------------------
// 2) BANCO DE DADOS - Azure SQL Database via Entity Framework Core
//    A connection string "DimDimDb" vem das App Settings do Web App (tipo SQLAzure),
//    ou do "dotnet user-secrets" no desenvolvimento local. Nunca fica no código.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DimDimDb")
    ?? throw new InvalidOperationException(
        "Connection string 'DimDimDb' não encontrada. Configure-a nas App Settings do Web App " +
        "(veja o README, passo de criação dos recursos) ou via 'dotnet user-secrets' localmente.");

builder.Services.AddDbContext<DimDimContext>(opcoes =>
    opcoes.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Rede de segurança: se as tabelas ainda não existirem, o EF as cria a partir do modelo.
// Se o script scripts/ddl-tabelas.sql já foi executado, esta chamada não faz nada.
using (var escopo = app.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<DimDimContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Não foi possível verificar/criar as tabelas no Azure SQL. Confira o firewall do servidor SQL e a connection string.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Valores monetários e datas no padrão brasileiro (vírgula nos centavos, dd/MM/yyyy).
var ptBr = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBr),
    SupportedCultures = new List<CultureInfo> { ptBr },
    SupportedUICultures = new List<CultureInfo> { ptBr }
});

app.UseStaticFiles();
app.UseRouting();

// Endpoint de saúde: confirma que o Web App consegue falar com o Azure SQL.
app.MapGet("/health", async (DimDimContext db) =>
{
    if (await db.Database.CanConnectAsync())
    {
        return Results.Ok(new { status = "ok", banco = "conectado" });
    }
    return Results.Json(new { status = "erro", banco = "sem conexao" }, statusCode: 503);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
