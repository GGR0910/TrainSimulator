
namespace Conductor.Domain
{
    public record ItineraryStep(int PlataformId, bool ShouldStop);

    internal class Train
    {
        public int Id { get; set; }
        public int? CurrentPlataformId { get; set; }
        public int? CurrentPathId { get; set; }
        public int CurrentRouteId { get; set; }
        public Queue<ItineraryStep> Itinerary { get; set; } = new Queue<ItineraryStep>();

        public Train(int id, int currentRouteId)
        {
            Id = id;
            CurrentRouteId = currentRouteId;
        }
    }
}
