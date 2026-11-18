namespace TrainSimulator.Domain
{
    internal class Station
    {
        public string Name { get; set; }
        public bool IsDoubleBed {  get; set; }

        public Station(string name, bool isDoubleBed)
        {
            Name = name;
            IsDoubleBed = isDoubleBed;
        }
    }
}
