using Conductor.Domain;
using Path = Conductor.Domain.Path;
namespace Conductor
{
    internal static class MapTracker
    {
        static List<Plataform> plataforms = new List<Plataform>
        {
            new Plataform(0, "Station S", false),
            new Plataform(1, "Station A", false),
            new Plataform(2, "Station B", false),
            new Plataform(3, "Station C", false),
            new Plataform(4, "Station D", false)
        };

        static List<Route> routes = new List<Route>
        {
            new Route(1, plataforms)
        };

        static List<Train> trains = new List<Train>();

        //Register train and return the train Id
        public static int RegisterTrain()
        {
            var train = new Train(trains.Count + 1, 1, 0);
            train.CurrentPlataformId = 0;
            trains.Add(train);
            return train.Id;
        }

        //Recieve path request and return the path Id if available, otherwise return -1
        public static int GetNextStationPath(int trainId, ref bool freeToExecute)
        {
            var train = trains.FirstOrDefault(t => t.Id == trainId);

            if (train == null) 
                return -1;

            var currentRoute = routes.FirstOrDefault(r => r.Id == train.CurrentRouteId);

            if (currentRoute == null) 
                return -1;

            if (train.CurrentRouteStopOrder >= currentRoute.Stops.Count - 1) 
                return -1;

            var nextStop = currentRoute.Stops[train.CurrentRouteStopOrder + 1];

            var path = Router.GetNextPath(train.CurrentPlataformId ?? 0, nextStop.PlataformId);
            if (path == null) 
                return -1;
            
            freeToExecute = !path.IsOcupied;
            return path.Id;
        }

        public static bool IsStationFreeToArrive(int trainId)
        {
            Train? train = trains.FirstOrDefault(t => t.Id == trainId);
            if (train == null)
                return false;

            Path? path = Router.GetPath(train.CurrentPathId!.Value);
            if (path == null)
                return false;

            Plataform? destinationPlataform = plataforms.FirstOrDefault(p => p.Id == path.DestinationPlataformId);
            if (destinationPlataform == null)
                return false;

            return !destinationPlataform.IsOcupied;
        }

        //Recieve "ArriveatStation" notification and free the path
        public static void ArriveAtStation(int trainId, int pathId)
        {
            Train? train = trains.FirstOrDefault(t => t.Id == trainId);

            if (train == null) 
                return;

            Path? pastPath = Router.GetPath(pathId);
            if (pastPath == null) 
                return;

            pastPath.IsOcupied = false;

            train.CurrentPlataformId = pastPath.DestinationPlataformId;
            train.CurrentPathId = null;
            train.CurrentRouteStopOrder++;
        
            Plataform currentPlataform = plataforms.First(p => p.Id == train.CurrentPlataformId);
            currentPlataform.IsOcupied = true;
        }

        //Recieve "DepartStation" notification and occupy the path
        public static void DepartFromStation(int trainId, int pathId)
        {
            Train? train = trains.FirstOrDefault(t => t.Id == trainId);
            if (train == null)
                return;

            Path? path = Router.GetPath(pathId);
            if (path == null)
                return;

            path.IsOcupied = true;
            train.CurrentPathId = path.Id;

            Plataform currentPlataform = plataforms.First(p => p.Id == train.CurrentPlataformId);
            currentPlataform.IsOcupied = false;
        }

        //Check liberation of the path and return true if the path is free, otherwise return false

    }
}
