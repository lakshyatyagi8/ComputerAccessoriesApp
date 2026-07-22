using Microsoft.EntityFrameworkCore;
using Accessories.Api.Data;
using Accessories.Api.Repositories;

namespace Accessories.Api;
public class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Add database context
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Register repository
        // Add this line right below your AddDbContext line
        builder.Services.AddScoped<IProductRepository, ProductRepository>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Add this to allow your Angular frontend to talk to the API
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngularFrontend",
                policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        var app = builder.Build();
        // Add this right AFTER app.Build() and BEFORE app.MapControllers() or your endpoints
        app.UseCors("AllowAngularFrontend");
        // Auto-create the database when the container spins up
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAngularFrontend");
        app.MapControllers();
        app.Run();
    }
}
