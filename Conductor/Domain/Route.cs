namespace Conductor.Domain
{
    internal class Route
    {
        public int Id { get; set; }
        public List<RoutePlataformStop> Stops { get; set; }
        public Route(int id, List<Plataform> plataforms)
        {
            Id = id;
            Stops = plataforms.Select((p, i) => new RoutePlataformStop { RouteId = id, PlataformId = p.Id, StopOrder = i }).ToList();
        }
    }
}
