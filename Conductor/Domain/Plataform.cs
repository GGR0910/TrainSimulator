namespace Conductor.Domain
{
    internal class Plataform
    {
        public int Id { get; set; }
        public string StationName { get; set; }
        public bool IsOcupied { get; set; }
        public bool IsDeposit { get; set; }
        public Queue<int> WaitingTrains { get; }

        public Plataform(int id, string stationName, bool isOcupied, bool isDeposit)
        {
            Id = id;
            StationName = stationName;
            IsOcupied = isOcupied;
            WaitingTrains = new Queue<int>();
            IsDeposit = isDeposit;
        }
    }
}
