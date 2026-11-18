
namespace Conductor.Domain
{
    internal class Train
    {
        public int Id { get; set; }
        public int? CurrentPlataformId { get; set; }
        public int? CurrentPathId { get; set; }
        public int CurrentRouteId { get; set; }
        public int CurrentRouteStopOrder { get; set; }

        public Train(int id, int currentRouteId, int currentRouteStopOrder)
        {
            Id = id;
            CurrentRouteId = currentRouteId;
            CurrentRouteStopOrder = currentRouteStopOrder;
        }
    }
}
