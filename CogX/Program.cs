using Microsoft.EntityFrameworkCore;
using CogX.Data;
using CogX.Hubs;
using CogX.Hubs.Games;
using CogX.Services;
using CogX.Services.Games;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<CogXDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IGameHistoryService, GameHistoryService>();
builder.Services.AddScoped<ITicTacToeService, TicTacToeService>();
builder.Services.AddScoped<ISpeedTypingService, SpeedTypingService>();
builder.Services.AddScoped<IConnect4Service, Connect4Service>();

// SignalR
builder.Services.AddSignalR();

// CORS pour le front-end
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React/Vite
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// SignalR Hubs
app.MapHub<LobbyHub>("/lobbyhub");
app.MapHub<TicTacToeHub>("/tictactoehub");
app.MapHub<SpeedTypingHub>("/speedtypinghub");
app.MapHub<Connect4Hub>("/connect4hub");

app.Run();
