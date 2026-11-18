
using TrainSimulator.Domain;
using Path = TrainSimulator.Domain.Path;

namespace TrainSimulator.Data
{
    internal static class PathList
    {
        public static List<Path> Paths = new List<Path>();

        public static void InitializeStations()
        {
            List<RailPath> paths = new List<RailPath>() 
            { 
                new RailPath(1, false),
                new RailPath(2, false),
            };

            Paths.Clear();
            Paths.Add(new Path(1,1,2, paths));
            Paths.Add(new Path(2, 2, 3,paths));
            Paths.Add(new Path(3, 3, 4, paths));
            Paths.Add(new Path(4, 4, 5, paths));
        }

        public static Path? GetPath(int stationSourceId, int stationDestinationId)
        {
            return Paths.FirstOrDefault(p => (p.StationAId == stationSourceId && p.StationBId == stationDestinationId) || (p.StationAId == stationDestinationId && p.StationBId == stationSourceId));
        }
    }
}
