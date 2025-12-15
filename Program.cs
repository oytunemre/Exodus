using FarmazonDemo.Data;
using FarmazonDemo.Services.Carts;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// 1) Controllers
builder.Services.AddControllers();

// 2) FluentValidation auto-validation (API)
builder.Services.AddFluentValidationAutoValidation();

// 3) Validator scan
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICartService, CartService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(db);
}


app.Run();
