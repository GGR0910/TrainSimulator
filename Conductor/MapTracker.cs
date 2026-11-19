using Conductor.Domain;
using Path = Conductor.Domain.Path;
namespace Conductor
{
    internal static class MapTracker
    {
        static List<Plataform> plataforms = new List<Plataform>
        {
            new Plataform(0, "Station S", false, true),
            new Plataform(1, "Station A1", false, false),
            new Plataform(2, "Station B1", false, false),
            new Plataform(3, "Station C1", false, false),
            new Plataform(4, "Station D1", false, false),

            new Plataform(5, "Station A2", false, false),
            new Plataform(6, "Station B2", false, false),
            new Plataform(7, "Station C2", false, false),
            new Plataform(8, "Station D2", false, false)

        };

        static List<Route> routes = new List<Route>
        {
            new Route(1, new List<int>(new[] { 1, 2, 3, 4 })),
            new Route(2, new List<int>(new[] { 8, 7, 6, 5 }))
        };

        static List<Train> trains = new List<Train>();

        //Register train and return the train Id
        public static int RegisterTrain(int routeId = 1, int depositId = 0)
        {
            var train = new Train(trains.Count + 1, routeId);
            train.CurrentPlataformId = depositId;

            var route = routes.First(r => r.Id == routeId);
            int cursorPlatformId = depositId;

            // 1. Posicionamento + paradas comerciais da rota
            foreach (int stopId in route.Stops)
            {
                EnqueueLeg(train.Itinerary, cursorPlatformId, stopId, stopAtDestination: true);
                cursorPlatformId = stopId;
            }

            // 2. Recolhimento de volta ao depósito (passa direto por tudo no caminho)
            EnqueueLeg(train.Itinerary, cursorPlatformId, depositId, stopAtDestination: false);

            trains.Add(train);
            return train.Id;
        }

        private static void EnqueueLeg(Queue<ItineraryStep> itinerary, int fromPlatformId, int toPlatformId, bool stopAtDestination)
        {
            var platforms = Router.FindPlatformsBetween(fromPlatformId, toPlatformId);

            for (int i = 0; i < platforms.Count; i++)
            {
                bool isLast = (i == platforms.Count - 1);
                itinerary.Enqueue(new ItineraryStep(platforms[i], isLast && stopAtDestination));
            }
        }
        //Recieve path request and return the path Id if available, otherwise return -1
        public static int GetNextStationPath(int trainId, ref bool freeToExecute)
        {
            Train? train = trains.FirstOrDefault(t => t.Id == trainId);
            if (train == null || train.Itinerary.Count == 0)
                return -1;

            int nextPlataformId = train.Itinerary.Peek().PlataformId;

            Path? path = Router.GetNextPath(train.CurrentPlataformId ?? 0, nextPlataformId);
            if (path == null)
                return -1;

            freeToExecute = !path.IsOcupied;

            if (freeToExecute)
                path.IsOcupied = true;
            else if (!path.WaitingTrains.Contains(trainId))
                path.WaitingTrains.Enqueue(trainId);

            return path.Id;
        }

        public static (bool IsFree, bool ShouldStop) IsStationFreeToArrive(int trainId)
        {
            Train? train = trains.FirstOrDefault(t => t.Id == trainId);
            if (train == null || train.CurrentPathId is null)
                return (false, false);

            Path? path = Router.GetPath(train.CurrentPathId.Value);
            if (path == null)
                return (false, false);

            Plataform? destinationPlataform = plataforms.FirstOrDefault(p => p.Id == path.DestinationPlataformId);
            if (destinationPlataform == null)
                return (false, false);

            if (destinationPlataform.IsDeposit)
                return (true, false);

            bool shouldStop = train.Itinerary.Count > 0 && train.Itinerary.Peek().ShouldStop;
            bool isFree = !destinationPlataform.IsOcupied;

            if (isFree)
                destinationPlataform.IsOcupied = true;
            else if (!destinationPlataform.WaitingTrains.Contains(trainId))
                destinationPlataform.WaitingTrains.Enqueue(trainId);

            return (isFree, shouldStop);
        }
        //Recieve "ArriveatStation" notification and free the path
        public static int? ArriveAtStation(int trainId, int pathId)
        {
            Train? train = trains.FirstOrDefault(t => t.Id == trainId);
            if (train == null) 
                return null;

            Path? pastPath = Router.GetPath(pathId);
            if (pastPath == null) 
                return null;

            pastPath.IsOcupied = false;

            train.CurrentPlataformId = pastPath.DestinationPlataformId;
            train.CurrentPathId = null;

            if (train.Itinerary.Count > 0 && train.Itinerary.Peek().PlataformId == train.CurrentPlataformId)
                train.Itinerary.Dequeue();

            Plataform currentPlataform = plataforms.First(p => p.Id == train.CurrentPlataformId);
            if (!currentPlataform.IsDeposit)
                currentPlataform.IsOcupied = true;


            // Handoff direto do caminho: se alguém espera, mantém ocupado e acorda o primeiro da fila
            if (pastPath.WaitingTrains.Count > 0)
            {
                pastPath.IsOcupied = true;
                return pastPath.WaitingTrains.Dequeue();
            }

            if(!currentPlataform.IsDeposit)
                pastPath.IsOcupied = false;

            return null;
        }

        public static (int TrainId, bool ShouldStop)? DepartFromStation(int trainId, int pathId)
        {
            Train? train = trains.FirstOrDefault(t => t.Id == trainId);
            if (train == null)
                return null;

            train.CurrentPathId = pathId;

            Plataform originPlataform = plataforms.First(p => p.Id == train.CurrentPlataformId);
            if (originPlataform.IsDeposit)
                return null;

            if (originPlataform.WaitingTrains.Count > 0)
            {
                originPlataform.IsOcupied = true;
                int nextTrainId = originPlataform.WaitingTrains.Dequeue();

                Train? nextTrain = trains.FirstOrDefault(t => t.Id == nextTrainId);
                bool shouldStop = nextTrain != null
                    && nextTrain.Itinerary.Count > 0
                    && nextTrain.Itinerary.Peek().ShouldStop;

                return (nextTrainId, shouldStop);
            }

            originPlataform.IsOcupied = false;
            return null;
        }
    }
}
