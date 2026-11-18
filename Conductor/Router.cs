using Conductor.Domain;
using Path = Conductor.Domain.Path;

namespace Conductor
{
    internal static class Router
    {
        // <summary>
        // Represents the paths that can be taken out of a specific station(Id).
        // </summary>
        public static Dictionary<int, List<Path>> PathsOutOfStation { get; set; }

        
        public static void InitializePathsOutOfStation()
        {
            PathsOutOfStation = new Dictionary<int, List<Path>>
            {
                {
                    0, new List<Path> { new Path(0, 0, 1, false, false) }
                },
                {
                    1, new List<Path> { new Path(1, 1, 2, false, false) }
                },
                {
                    2, new List<Path> { new Path(2, 2, 3, false, false) }
                },
                {
                    3, new List<Path> { new Path(3, 3, 4, false, false) }
                }
            };
        }

        public static Path? GetNextPath(int currentPlatformId, int destinationPlatformId)
        {
            if (PathsOutOfStation == null) return null;

            if (PathsOutOfStation.TryGetValue(currentPlatformId, out List<Path> availablePaths))
            {
                return availablePaths.FirstOrDefault(p => p.DestinationPlataformId == destinationPlatformId && p.IsFree);
            }

            return null;
        }

        public static Path? GetPath(int pathId)
        {
            return PathsOutOfStation.Values.SelectMany(p => p).FirstOrDefault(p => p.Id == pathId);
        }

    }
}
