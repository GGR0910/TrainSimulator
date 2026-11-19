namespace Conductor.Domain
{
    internal class Route
    {
        public int Id { get; set; }
        public List<int> Stops { get; set; }
        public Route(int id, List<int> stops)
        {
            Id = id;
            Stops = stops;
        }
    }
}
