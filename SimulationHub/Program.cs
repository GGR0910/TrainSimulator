using SimulationHub;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();


app.MapHub<MainSimulationHub>("/sim-hub");

app.MapGet("/", () => "Simulação iniciando. Aguardando conexões.");

app.Run();
