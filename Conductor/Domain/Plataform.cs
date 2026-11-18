namespace Conductor.Domain
{
    internal class Plataform
    {
        public int Id { get; set; }
        public string StationName { get; set; }
        public bool IsOcupied { get; set; }

        public Plataform(int id, string stationName, bool isOcupied)
        {
            Id = id;
            StationName = stationName;
            IsOcupied = isOcupied;
        }
    }
}
