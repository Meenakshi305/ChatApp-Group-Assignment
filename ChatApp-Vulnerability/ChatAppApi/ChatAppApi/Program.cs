using ChatAppApi.Data;
using ChatAppApi.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithOrigins("http://localhost:3000");
    });
});

builder.Services.AddScoped<ChatWebSocketHandler>();

var app = builder.Build();

app.UseCors();
app.UseWebSockets();

app.Map("/ws", async (HttpContext context, ChatWebSocketHandler handler) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleAsync(socket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapControllers();
app.Run();
