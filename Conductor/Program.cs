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
    var (isFree, shouldStop) = MapTracker.IsStationFreeToArrive(trainId);
    Console.WriteLine($"[CONDUCTOR] Estação para Trem {trainId}: Livre={isFree} | Parar={shouldStop}");

    await connection.SendAsync("IsStationFreeToArrive", trainId, isFree, shouldStop);
});

connection.On<int, int>("DepartFromStation", async (trainId, pathId) =>
{
    var handoff = MapTracker.DepartFromStation(trainId, pathId);
    Console.WriteLine($"[CONDUCTOR] Trem {trainId} partiu pelo caminho {pathId}.");

    if (handoff.HasValue)
    {
        Console.WriteLine($"[CONDUCTOR] FILA: Liberando estação para Trem {handoff.Value.TrainId} (Parar={handoff.Value.ShouldStop})!");
        await connection.SendAsync("IsStationFreeToArrive", handoff.Value.TrainId, true, handoff.Value.ShouldStop);
    }
});

connection.On<int, int>("ArriveAtStation", async (trainId, pathId) =>
{
    int? nextTrainId = MapTracker.ArriveAtStation(trainId, pathId);
    Console.WriteLine($"[CONDUCTOR] Trem {trainId} chegou à estação e liberou o caminho {pathId}.");

    if (nextTrainId.HasValue)
    {
        Console.WriteLine($"[CONDUCTOR] FILA: Liberando caminho {pathId} para o Trem {nextTrainId.Value}!");
        await connection.SendAsync("NextStationPath", nextTrainId.Value, pathId, true);
    }
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