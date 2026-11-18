namespace TrainSimulator.Domain
{
    internal class Route
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<StopRouteStation> StationsStops { get; set; }

        public Route(int id, string name, List<StopRouteStation> stationsStops)
        {
            Id = id;
            Name = name;
            StationsStops = stationsStops;
        }

        
    }
}
