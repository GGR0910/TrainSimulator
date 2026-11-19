using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

var hubUrl = Environment.GetEnvironmentVariable("SIMULATION_HUB_URL")
    ?? "http://localhost:5070/sim-hub";

var halfTravelTime = TimeSpan.FromMilliseconds(1500); // 1.5s + 1.5s = 3s por trecho
var stationDwellTime = TimeSpan.FromSeconds(2);       // Tempo de embarque nas paradas comerciais
var registerTimeout = TimeSpan.FromSeconds(10);

await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

int myTrainId = -1;
var tripLog = new List<(int PathId, bool Stopped)>();

var registerWaiter = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
TaskCompletionSource<int>? pathWaiter = null;
TaskCompletionSource<bool>? stationWaiter = null;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n[SISTEMA] Encerrando simulação...");
    cts.Cancel();
};

#region Domain Event Handlers
connection.On<int>("TrainRegistered", trainId =>
{
    registerWaiter.TrySetResult(trainId);
});

connection.On<int, int, bool>("NextStationPath", (trainId, nextPathId, freeToExecute) =>
{
    if (trainId != myTrainId)
        return;

    if (nextPathId == -1 || freeToExecute)
    {
        pathWaiter?.TrySetResult(nextPathId);
        return;
    }

    Console.WriteLine($"[TREM {myTrainId}] Caminho {nextPathId} ocupado! Aguardando na fila do servidor...");
});

connection.On<int, bool, bool>("IsStationFreeToArrive", (trainId, isFree, shouldStop) =>
{
    if (trainId != myTrainId)
        return;

    if (isFree)
    {
        stationWaiter?.TrySetResult(shouldStop);
        return;
    }

    Console.WriteLine($"[TREM {myTrainId}] Estação ocupada! Aguardando na fila do servidor...");
});
#endregion

#region SignalR Event Handlers
connection.Reconnecting += exception =>
{
    Console.WriteLine($"[REDE] Conexão perdida. Tentando reconectar: {exception?.Message}");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"[REDE] Conectado novamente ao Hub. ID: {connectionId}");
    return Task.CompletedTask;
};

connection.Closed += exception =>
{
    Console.WriteLine($"[REDE] Conexão com o Hub encerrada: {exception?.Message}");
    return Task.CompletedTask;
};
#endregion

var stopwatch = Stopwatch.StartNew();

try
{
    await connection.StartAsync(cts.Token);
    Console.WriteLine($"[SISTEMA] Conectado ao SimulationHub em {hubUrl}.\n");

    // 1. Registro do trem único
    Console.WriteLine("[TREM] Solicitando registro ao Conductor...");
    await connection.SendAsync("RegisterTrain", cts.Token);
    myTrainId = await registerWaiter.Task.WaitAsync(registerTimeout, cts.Token);
    Console.WriteLine($"[TREM {myTrainId}] Nasceu e foi registrado com sucesso!\n");

    // 2. Ciclo completo até o Conductor devolver -1 (de volta ao depósito)
    while (!cts.Token.IsCancellationRequested)
    {
        // Portão 1: Pede o próximo caminho
        pathWaiter = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.WriteLine($"[TREM {myTrainId}] Solicitando próximo caminho...");
        await connection.SendAsync("GetNextStationPath", myTrainId, cts.Token);

        int nextPathId = await pathWaiter.Task.WaitAsync(cts.Token);
        pathWaiter = null;

        if (nextPathId == -1)
            break;

        // Parte da estação e percorre a primeira metade do trajeto
        Console.WriteLine($"[TREM {myTrainId}] Caminho {nextPathId} livre! Partindo...");
        await connection.SendAsync("DepartFromStation", myTrainId, nextPathId, cts.Token);
        await Task.Delay(halfTravelTime, cts.Token);

        // Portão 2: Meio do caminho -> verifica se a estação está livre e se deve parar
        stationWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.WriteLine($"[TREM {myTrainId}] Meio do caminho {nextPathId}. Verificando estação...");
        await connection.SendAsync("CheckIsStationFreeToArrive", myTrainId, cts.Token);

        bool shouldStop = await stationWaiter.Task.WaitAsync(cts.Token);
        stationWaiter = null;

        // Conclui a entrada na estação e libera o caminho
        Console.WriteLine($"[TREM {myTrainId}] Estação livre! Concluindo entrada pelo caminho {nextPathId}...");
        await Task.Delay(halfTravelTime, cts.Token);
        await connection.SendAsync("ArriveAtStation", myTrainId, nextPathId, cts.Token);

        tripLog.Add((nextPathId, shouldStop));

        if (shouldStop)
        {
            Console.WriteLine($"[TREM {myTrainId}] Parada comercial (embarque/desembarque por {stationDwellTime.TotalSeconds:0}s)...");
            await Task.Delay(stationDwellTime, cts.Token);
        }
        else
        {
            Console.WriteLine($"[TREM {myTrainId}] Passando direto pela estação...");
        }
    }

    stopwatch.Stop();

    int totalStops = tripLog.Count(s => s.Stopped);
    int totalPassThroughs = tripLog.Count - totalStops;
    string formattedRoute = tripLog.Count > 0
        ? string.Join(" -> ", tripLog.Select(s => $"{s.PathId}({(s.Stopped ? "parou" : "direto")})"))
        : "Nenhum";

    Console.WriteLine("\n================ RESUMO DA VIAGEM ================");
    Console.WriteLine($"Trem ID:          {myTrainId}");
    Console.WriteLine($"Trechos totais:   {tripLog.Count}");
    Console.WriteLine($"Paradas:          {totalStops} | Passagens diretas: {totalPassThroughs}");
    Console.WriteLine($"Tempo total:      {stopwatch.Elapsed.TotalSeconds:0.0}s");
    Console.WriteLine($"Trajeto:          {formattedRoute}");
    Console.WriteLine("==================================================");
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n[SISTEMA] Simulação interrompida pelo usuário.");
}
catch (TimeoutException)
{
    Console.WriteLine("\n[ERRO] Tempo limite esgotado aguardando registro no Conductor.");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[ERRO] Falha na simulação: {ex.Message}");
}

Console.WriteLine("\nPressione ENTER para fechar...");
Console.ReadLine();