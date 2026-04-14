using Microsoft.EntityFrameworkCore;
using StockService.Data;
using StockService.Models;
using StockService.Services;

AppContext.SetSwitch("System.Net.Sockets.IPAddress.IPv6IsNotSupported", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IGroqService, GroqService>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(
                "https://korp-teste-ruan-campos.vercel.app",
                "http://localhost:4200"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("*")));

var app = builder.Build();

app.UseCors();

// GET /products
app.MapGet("/products", async (AppDbContext db) =>
    await db.Products.ToListAsync());

// GET /products/{id}
app.MapGet("/products/{id}", async (int id, AppDbContext db) =>
    await db.Products.FindAsync(id) is Product p ? Results.Ok(p) : Results.NotFound());

// POST /products - Cadastro de Produto
app.MapPost("/products", async (Product product, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(product.Code))
        return Results.BadRequest(new { error = "Código é obrigatório." });

    var exists = await db.Products.AnyAsync(p => p.Code == product.Code);
    if (exists)
        return Results.Conflict(new { error = $"Já existe um produto com o código '{product.Code}'" });

    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
});

// PUT /products/{id}/balance
app.MapPut("/products/{id}/balance", async (int id, int quantity, AppDbContext db) =>
{
    await using var transaction = await db.Database.BeginTransactionAsync();
    try
    {
        var product = await db.Products
            .FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Id\" = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync();

        if (product is null)
        {
            await transaction.RollbackAsync();
            return Results.NotFound();
        }

        if (product.Balance < quantity)
        {
            await transaction.RollbackAsync();
            return Results.BadRequest("Saldo insuficiente.");
        }

        product.Balance -= quantity;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Results.Ok(product);
    }
    catch
    {
        await transaction.RollbackAsync();
        return Results.Problem("Erro ao atualizar saldo.");
    }
});

// POST /api/products/suggest-description
app.MapPost("/api/products/suggest-description", async (SuggestDescriptionRequest request, IGroqService groqService, ILogger<Program> logger) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Code))
        return Results.BadRequest(new { error = "Código é obrigatório." });

    try
    {
        var prompt = "Você é um assistente de cadastro de produtos. Gere uma descrição curta, clara e objetiva em português para um produto identificado pelo código a seguir. " +
                     "Responda apenas com a descrição sugerida, sem explicações adicionais.\n\nCódigo: " + request.Code;

        var description = await groqService.GenerateAsync(prompt);
        return Results.Ok(new { description });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao sugerir descrição via IA");
        return Results.Problem("Não foi possível sugerir a descrição no momento. Tente novamente mais tarde.", statusCode: 500);
    }
});

// POST /api/products/check-low-stock
app.MapPost("/api/products/check-low-stock", async (CheckLowStockRequest request, IGroqService groqService, ILogger<Program> logger) =>
{
    if (request?.Products is null || request.Products.Count == 0)
        return Results.BadRequest(new { error = "Lista de produtos é obrigatória." });

    try
    {
        var lines = string.Join("\n", request.Products.Select(p =>
            $"- {p.Code} | {p.Description} | saldo: {p.Balance}"));

        var prompt = "Você é um assistente de gestão de estoque. Analise a lista de produtos abaixo e identifique aqueles com saldo baixo (considere baixo quando menor ou igual a 5). " +
                     "Gere um alerta curto e objetivo em português sugerindo reposição apenas dos produtos com saldo baixo. " +
                     "Se nenhum produto estiver com saldo baixo, responda apenas: \"Estoque em níveis adequados.\". " +
                     "Não inclua explicações adicionais.\n\nProdutos:\n" + lines;

        var alert = await groqService.GenerateAsync(prompt);
        return Results.Ok(new { alert });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao verificar estoque baixo via IA");
        return Results.Problem("Não foi possível verificar o estoque no momento. Tente novamente mais tarde.", statusCode: 500);
    }
});

app.Run();

public record SuggestDescriptionRequest(string Code);

public record CheckLowStockProduct(string Code, string Description, int Balance);
public record CheckLowStockRequest(List<CheckLowStockProduct> Products);