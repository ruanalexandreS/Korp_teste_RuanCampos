using Microsoft.EntityFrameworkCore;
using BillingService.Data;
using BillingService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("StockService", client =>
    client.BaseAddress = new Uri("http://localhost:5263"));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

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
    var lastNumber = await db.Invoices.MaxAsync(i => (int?)i.Number) ?? 0;
    invoice.Number = lastNumber + 1;
    invoice.Status = "Aberta";
    db.Invoices.Add(invoice);
    await db.SaveChangesAsync();
    return Results.Created($"/invoices/{invoice.Id}", invoice);
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

    foreach (var item in invoice.Items)
    {
        try
        {
            var response = await client.PutAsync(
                $"/products/{item.ProductId}/balance?quantity={item.Quantity}",
                null);

            if (!response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrEmpty(idempotencyKey))
                    idempotencyCache.TryRemove(idempotencyKey, out _);
                return Results.BadRequest($"Saldo insuficiente para o produto {item.ProductCode}.");
            }
        }
        catch
        {
            if (!string.IsNullOrEmpty(idempotencyKey))
                idempotencyCache.TryRemove(idempotencyKey, out _);
            return Results.Problem("Serviço de estoque indisponível. Tente novamente.");
        }
    }

    invoice.Status = "Fechada";
    await db.SaveChangesAsync();
    return Results.Ok(invoice);
});

app.Run();