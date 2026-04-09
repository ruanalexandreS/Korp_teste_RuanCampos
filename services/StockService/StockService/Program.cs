using Microsoft.EntityFrameworkCore;
using StockService.Data;
using StockService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();

// GET /products
app.MapGet("/products", async (AppDbContext db) =>
    await db.Products.ToListAsync());

// GET /products/{id}
app.MapGet("/products/{id}", async (int id, AppDbContext db) =>
    await db.Products.FindAsync(id) is Product p ? Results.Ok(p) : Results.NotFound());

// POST /products
app.MapPost("/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
});

// PUT /products/{id}/balance
app.MapPut("/products/{id}/balance", async (int id, int quantity, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();
    if (product.Balance < quantity) return Results.BadRequest("Saldo insuficiente.");
    product.Balance -= quantity;
    await db.SaveChangesAsync();
    return Results.Ok(product);
});

app.Run();