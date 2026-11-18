using Conductor;
using Microsoft.AspNetCore.SignalR.Client;

Router.InitializePathsOutOfStation();

var hubUrl = Environment.GetEnvironmentVariable("SIMULATION_HUB_URL")
    ?? "http://localhost:5070/sim-hub";

await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

#region Domain Event Handlers
connection.On("RegisterNewTrain", async () =>
{
    int trainId = MapTracker.RegisterTrain();
    Console.WriteLine($"[CONDUCTOR] Novo trem registrado. ID: {trainId}");

    await connection.SendAsync("TrainRegistered", trainId);
});

connection.On<int>("GetNextStationPath", async trainId =>
{
    bool freeToExecute = false;
    int nextPathId = MapTracker.GetNextStationPath(trainId, ref freeToExecute);
    Console.WriteLine($"[CONDUCTOR] Trem {trainId} pediu caminho. Retorno: {nextPathId}");

    await connection.SendAsync("NextStationPath", trainId, nextPathId, freeToExecute);
});

connection.On<int>("CheckIsStationFreeToArrive", async trainId =>
{
    bool isFree = MapTracker.IsStationFreeToArrive(trainId);
    Console.WriteLine($"[CONDUCTOR] Verificando se a estação está livre para o trem {trainId}. Retorno: {isFree}");

    await connection.SendAsync("IsStationFreeToArrive", trainId, isFree);
});

connection.On<int, int>("DepartFromStation", (trainId, pathId) =>
{
    MapTracker.DepartFromStation(trainId, pathId);
    Console.WriteLine($"[CONDUCTOR] Trem {trainId} partiu pelo caminho {pathId}.");
});

connection.On<int, int>("ArriveAtStation", (trainId, pathId) =>
{
    MapTracker.ArriveAtStation(trainId, pathId);
    Console.WriteLine($"[CONDUCTOR] Trem {trainId} chegou à estação e liberou o caminho {pathId}.");
});
#endregion

#region SignalR Event Handlers
connection.Reconnecting += exception =>
{
    Console.WriteLine($"Conexão perdida. Tentando reconectar: {exception?.Message}");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"Conectado novamente ao Hub. ID: {connectionId}");
    return Task.CompletedTask;
};

connection.Closed += exception =>
{
    Console.WriteLine($"Conexão com o Hub encerrada: {exception?.Message}");
    return Task.CompletedTask;
};
#endregion

await connection.StartAsync();
Console.WriteLine($"Conectado ao SimulationHub em {hubUrl}. Aguardando notificações...");

await Task.Delay(Timeout.Infinite);