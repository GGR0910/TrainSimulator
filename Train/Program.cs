using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;

var hubUrl = Environment.GetEnvironmentVariable("SIMULATION_HUB_URL")
    ?? "http://localhost:5070/sim-hub";

const int stationsBeforeNextSpawn = 2;
var travelTime = TimeSpan.FromMilliseconds(50);
var responseTimeout = TimeSpan.FromSeconds(10);

await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

// Waiter único para registro (só nasce um trem por vez)
TaskCompletionSource<int>? registerWaiter = null;

// Um waiter por trainId para respostas de caminho (vários trens pedem rota em paralelo)
var pathWaiters = new ConcurrentDictionary<int, TaskCompletionSource<int>>();

// Permite interromper a criação de novos trens com Ctrl+C sem matar os que já estão na malha
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n[SISTEMA] Encerrando geração de novos trens. Aguardando trens ativos concluírem...");
    cts.Cancel();
};

#region Domain Event Handlers
connection.On<int>("TrainRegistered", trainId =>
{
    registerWaiter?.TrySetResult(trainId);
});

connection.On<int, int>("NextStationPath", (trainId, pathId) =>
{
    if (pathWaiters.TryRemove(trainId, out var waiter))
        waiter.TrySetResult(pathId);
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

// Ciclo completo de vida de um trem individual
async Task RunTrainAsync(int trainId, TaskCompletionSource twoStationsSignal)
{
    var completedPaths = new List<int>();
    int stationsAdvanced = 0;

    try
    {
        int nextPathId;
        do
        {
            var waiter = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            pathWaiters[trainId] = waiter;

            Console.WriteLine($"[TREM {trainId}] Solicitando próximo caminho...");
            await connection.SendAsync("GetNextStationPath", trainId);

            try
            {
                nextPathId = await waiter.Task.WaitAsync(responseTimeout);
            }
            catch (TimeoutException)
            {
                pathWaiters.TryRemove(trainId, out _);
                Console.WriteLine($"[TREM {trainId}] ERRO: Timeout aguardando resposta do Conductor.");
                return;
            }

            if (nextPathId != -1)
            {
                Console.WriteLine($"[TREM {trainId}] Caminho {nextPathId} autorizado. Partindo...");
                await connection.SendAsync("DepartFromStation", trainId, nextPathId);

                await Task.Delay(travelTime);

                Console.WriteLine($"[TREM {trainId}] Cheguei à estação pelo caminho {nextPathId}.");
                await connection.SendAsync("ArriveAtStation", trainId, nextPathId);

                completedPaths.Add(nextPathId);
                stationsAdvanced++;

                // Assim que avança 2 estações, libera o laço principal para criar o próximo trem
                if (stationsAdvanced == stationsBeforeNextSpawn)
                {
                    Console.WriteLine($"[SISTEMA] Trem {trainId} avançou {stationsBeforeNextSpawn} estações -> liberando próximo trem!");
                    twoStationsSignal.TrySetResult();
                }
            }
        } while (nextPathId != -1);

        Console.WriteLine($"[TREM {trainId}] Fim da linha! Caminhos: {string.Join(" -> ", completedPaths)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TREM {trainId}] ERRO: {ex.Message}");
    }
    finally
    {
        // Garante que o gerador nunca fique travado se um trem terminar ou falhar antes de 2 estações
        twoStationsSignal.TrySetResult();
    }
}

var activeTrainTasks = new List<Task>();

try
{
    await connection.StartAsync(cts.Token);
    Console.WriteLine($"[SISTEMA] Conectado ao SimulationHub em {hubUrl}. Pressione Ctrl+C para parar de gerar trens.\n");

    while (!cts.Token.IsCancellationRequested)
    {
        // Limpa tarefas de trens já finalizados para não acumular memória
        activeTrainTasks.RemoveAll(t => t.IsCompleted);

        // 1. Registra o novo trem e aguarda o ID gerado pelo Conductor
        registerWaiter = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.WriteLine("[SISTEMA] Solicitando registro de novo trem...");
        await connection.SendAsync("RegisterTrain", cts.Token);

        int newTrainId = await registerWaiter.Task.WaitAsync(responseTimeout, cts.Token);
        Console.WriteLine($"[TREM {newTrainId}] Nasceu e foi registrado com sucesso!");

        // 2. Inicia a viagem em paralelo, sem bloquear esperando o fim da linha
        var twoStationsSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task trainTask = RunTrainAsync(newTrainId, twoStationsSignal);
        activeTrainTasks.Add(trainTask);

        // 3. Pausa apenas até este trem avançar 2 estações (ou Ctrl+C)
        await twoStationsSignal.Task.WaitAsync(cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Saída limpa ao pressionar Ctrl+C
}
catch (TimeoutException)
{
    Console.WriteLine("\n[ERRO] Tempo limite esgotado aguardando o Conductor.");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[ERRO] Falha no gerador de trens: {ex.Message}");
}

if (activeTrainTasks.Count > 0)
{
    await Task.WhenAll(activeTrainTasks);
}

Console.WriteLine("\n[SISTEMA] Todos os trens ativos finalizaram. Pressione ENTER para fechar...");
Console.ReadLine();