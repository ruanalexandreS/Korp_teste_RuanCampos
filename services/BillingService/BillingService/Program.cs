using Microsoft.EntityFrameworkCore;
using BillingService.Data;
using BillingService.Models;
using BillingService.Services;
using Polly;

AppContext.SetSwitch("System.Net.Sockets.IPAddress.IPv6IsNotSupported", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("StockService", client =>
    client.BaseAddress = new Uri("https://korpstockdb-production.up.railway.app"))
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

builder.Services.AddHttpClient<IGroqService, GroqService>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

var idempotencyCache = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

// GET /invoices
app.MapGet("/invoices", async (AppDbContext db) =>
    await db.Invoices.Include(i => i.Items).ToListAsync());

// GET /invoices/{id}
app.MapGet("/invoices/{id}", async (int id, AppDbContext db) =>
    await db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id)
        is Invoice invoice ? Results.Ok(invoice) : Results.NotFound());

// POST /invoices
app.MapPost("/invoices", async (Invoice invoice, AppDbContext db) =>
{
    await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
    try
    {
        var lastNumber = await db.Invoices.MaxAsync(i => (int?)i.Number) ?? 0;
        invoice.Number = lastNumber + 1;
        invoice.Status = "Aberta";
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Results.Created($"/invoices/{invoice.Id}", invoice);
    }
    catch
    {
        await transaction.RollbackAsync();
        return Results.Problem("Erro ao criar nota fiscal.");
    }
});

// POST /invoices/{id}/print
app.MapPost("/invoices/{id}/print", async (int id, HttpContext ctx, AppDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var idempotencyKey = ctx.Request.Headers["Idempotency-Key"].FirstOrDefault();
    
    if (!string.IsNullOrEmpty(idempotencyKey))
    {
        if (idempotencyCache.ContainsKey(idempotencyKey))
            return Results.Ok("Requisição já processada.");
        idempotencyCache.TryAdd(idempotencyKey, true);
    }

    var invoice = await db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
    if (invoice is null) return Results.NotFound();
    if (invoice.Status != "Aberta") return Results.BadRequest("Nota já foi impressa.");

    var client = httpClientFactory.CreateClient("StockService");
    var debitedItems = new List<(int ProductId, int Quantity)>();

    foreach (var item in invoice.Items)
    {
        try
        {
            var response = await client.PutAsync(
                $"/products/{item.ProductId}/balance?quantity={item.Quantity}",
                null);

            if (!response.IsSuccessStatusCode)
            {
                await RollbackDebitsAsync(client, debitedItems);
                if (!string.IsNullOrEmpty(idempotencyKey))
                    idempotencyCache.TryRemove(idempotencyKey, out _);
                return Results.BadRequest($"Saldo insuficiente para o produto {item.ProductCode}. Operação revertida.");
            }

            debitedItems.Add((item.ProductId, item.Quantity));
        }
        catch
        {
            await RollbackDebitsAsync(client, debitedItems);
            if (!string.IsNullOrEmpty(idempotencyKey))
                idempotencyCache.TryRemove(idempotencyKey, out _);
            return Results.Problem("Serviço de estoque indisponível. Operação revertida.");
        }
    }

    invoice.Status = "Fechada";
    await db.SaveChangesAsync();
    return Results.Ok(invoice);
});

// POST /api/invoices/{id}/summarize
app.MapPost("/api/invoices/{id}/summarize", async (int id, AppDbContext db, IGroqService groqService, ILogger<Program> logger) =>
{
    try
    {
        var invoice = await db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return Results.NotFound();

        var totalItems = invoice.Items.Count;
        var totalQuantity = invoice.Items.Sum(i => i.Quantity);
        var topItem = invoice.Items.OrderByDescending(i => i.Quantity).FirstOrDefault();
        var topItemText = topItem is null
            ? "nenhum"
            : $"{topItem.ProductDescription} ({topItem.ProductCode}) com {topItem.Quantity} unidade(s)";

        var prompt = "Você é um assistente que gera resumos curtos e objetivos de notas fiscais em português. " +
                     "Com base nos dados abaixo, escreva um resumo de no máximo 2 frases, sem explicações adicionais.\n\n" +
                     $"Número da NF: {invoice.Number}\n" +
                     $"Status: {invoice.Status}\n" +
                     $"Quantidade de itens distintos: {totalItems}\n" +
                     $"Quantidade total de unidades: {totalQuantity}\n" +
                     $"Produto com maior quantidade: {topItemText}";

        var summary = await groqService.GenerateAsync(prompt);
        return Results.Ok(new { summary });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao gerar resumo da NF {Id}", id);
        return Results.Problem("Não foi possível gerar o resumo da nota no momento. Tente novamente mais tarde.", statusCode: 500);
    }
});

async Task RollbackDebitsAsync(HttpClient client, List<(int ProductId, int Quantity)> items)
{
    foreach (var (productId, quantity) in items)
    {
        try { await client.PutAsync($"/products/{productId}/balance?quantity={-quantity}", null); }
        catch { /* best-effort rollback */ }
    }
}

app.Run();