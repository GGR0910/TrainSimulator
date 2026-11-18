namespace TrainSimulator.Domain
{
    internal class Train
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Route CurrentRoute { get; set; }
        public Station CurrentStation { get; set; }

        public Train(int id, string name, Route route)
        {
            Id = id;
            Name = name;
            CurrentRoute = route;
        }

        public void StartRoute()
        {
            CurrentStation = CurrentRoute.Stations.First(s => s.Order == 0);
        }

        public bool AdvanceToNextStation()
        {
            if (CurrentRoute == null)
            {
                throw new InvalidOperationException("Train is not assigned to a route.");
            }

            Station nextStation = CurrentRoute.GetNextStation(CurrentStation);
            if (nextStation != null)
            {
                CurrentStation = nextStation;
                return true;
            }

            return false; // No next station available
        }

        public void ReturnLocation()
        {
            Console.WriteLine($"Train following route {CurrentRoute.Name} at station {CurrentStation.Name}.");
        }
    }
}
