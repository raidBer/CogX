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

// CORS - Configuration permissive pour le développement
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Accepte toutes les origins en dev
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

// CORS - Doit être AVANT UseHttpsRedirection et UseAuthorization
app.UseCors("DevelopmentCorsPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// SignalR Hubs
app.MapHub<LobbyHub>("/lobbyhub");
app.MapHub<TicTacToeHub>("/tictactoehub");
app.MapHub<SpeedTypingHub>("/speedtypinghub");
app.MapHub<Connect4Hub>("/connect4hub");

app.Run();
