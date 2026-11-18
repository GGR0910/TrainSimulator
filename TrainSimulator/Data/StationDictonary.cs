
using TrainSimulator.Domain;

namespace TrainSimulator.Data
{
    internal static class StationDictonary
    {
        public static Dictionary<int, Station> Stations = new Dictionary<int, Station>();

        public static void InitializeStations()
        {
            Stations.Clear();
            Stations.Add(1, new Station("A", true));
            Stations.Add(2, new Station("B", true));
            Stations.Add(3, new Station("C", true));
            Stations.Add(4, new Station("D", true));
            Stations.Add(5, new Station("E", true));
        }

        public static Station GetStation(int id)
        {
            return Stations[id];
        }


    }
}
