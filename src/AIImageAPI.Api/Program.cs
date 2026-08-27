using AIImageAPI.Core.Services;
using AIImageAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using AIImageAPI.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAIImageInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
        await db.Database.MigrateAsync();

    var matcher = scope.ServiceProvider.GetRequiredService<ICustomerMatchService>();
    await matcher.ReloadAsync();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
