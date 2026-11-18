namespace Conductor.Domain
{
    internal class Path
    {
        public int Id { get; set; }
        public int SourcePlataformId { get; set; }
        public int DestinationPlataformId { get; set; }
        public bool IsOcupied { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsFree => !IsOcupied && !IsBlocked;

        public Path(int id, int sourcePlataformId, int destinationPlataformId, bool isOcupied, bool isBlocked)
        {
            Id = id;
            SourcePlataformId = sourcePlataformId;
            DestinationPlataformId = destinationPlataformId;
            IsOcupied = isOcupied;
            IsBlocked = isBlocked;
        }
    }
}
