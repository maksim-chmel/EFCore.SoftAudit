using EFCore.SoftAudit;
using Microsoft.EntityFrameworkCore;
using SampleApi;
using SampleApi.Data;
using SampleApi.DTO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSoftAudit<AppDbContext>(options =>
    options.UseSqlite("Data Source=sample.db"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapPost("/orders", async (AppDbContext db, CreateOrderRequest request) =>
{
    var order = new Order();
    order.Name = request.Name;
    order.Quantity = request.Quantity;
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}",order);
    
});
app.MapGet("/orders", async (AppDbContext db) =>
{
   var listOrders = await db.Orders.ToListAsync();
   return Results.Ok(listOrders);
    
});
app.MapDelete("/orders/{id}", async (AppDbContext db , int id) =>
{
  var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
  if (order == null) return Results.NotFound();
  db.Orders.Remove(order);
  await db.SaveChangesAsync();
  return Results.NoContent();
  
    
});
app.Run();