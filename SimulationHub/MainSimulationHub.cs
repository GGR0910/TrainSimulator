using Microsoft.AspNetCore.SignalR;

namespace SimulationHub
{
    public class MainSimulationHub : Hub
    {
        // ==========================================
        // Trem → Hub → Conductor
        // ==========================================
        public async Task RegisterTrain()
        {
            await Clients.Others.SendAsync("RegisterNewTrain");
        }

        public async Task GetNextStationPath(int trainId)
        {
            await Clients.Others.SendAsync("GetNextStationPath", trainId);
        }

        public async Task DepartFromStation(int trainId, int pathId)
        {
            await Clients.Others.SendAsync("DepartFromStation", trainId, pathId);
        }

        public async Task ArriveAtStation(int trainId, int pathId)
        {
            await Clients.Others.SendAsync("ArriveAtStation", trainId, pathId);
        }

        public async Task CheckIsStationFreeToArrive(int trainId)
        {
            await Clients.Others.SendAsync("CheckIsStationFreeToArrive", trainId);
        }

        // ==========================================
        // Conductor → Hub → Trem
        // ==========================================
        public async Task TrainRegistered(int trainId)
        {
            await Clients.Others.SendAsync("TrainRegistered", trainId);
        }

        public async Task NextStationPath(int trainId, int nextPathId, bool freeToExecute)
        {
            await Clients.Others.SendAsync("NextStationPath", trainId, nextPathId, freeToExecute);
        }

        public async Task IsStationFreeToArrive(int trainId, bool isFree, bool shouldStop)
        {
            await Clients.Others.SendAsync("IsStationFreeToArrive", trainId, isFree, shouldStop);
        }

    }
}