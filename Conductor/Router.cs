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

        private static readonly Dictionary<(int Origin, int Destination), IReadOnlyList<int>> routeCache = new();

        public static void InitializePathsOutOfStation()
        {
            PathsOutOfStation = new Dictionary<int, List<Path>>
            {
                //Out of Station S/Depot
                {
                    0, new List<Path> { new Path(0, 0, 1, false, false) }
                },

                //Out of A1
                {
                    1, new List<Path> { new Path(1, 1, 2, false, false) }
                },

                //Out of B1
                {
                    2, new List<Path> { new Path(2, 2, 3, false, false) }
                },

                //Out of C1
                {
                    3, new List<Path> { new Path(3, 3, 4, false, false) }
                },

                //Out of D1 back to D2
                {
                    4, new List<Path> { new Path(4, 4, 8, false, false) }
                },

                //Out of D2 
                {
                    8, new List<Path> { new Path(5, 8, 7, false, false) }
                },

                //Out of C2
                {
                    7, new List<Path> { new Path(6, 7, 6, false, false) }
                },

                //Out of B2
                {
                    6, new List<Path> { new Path(7, 6, 5, false, false) }
                },

                //Out of A2 back to Station S/Depot
                {
                    5, new List<Path> { new Path(8, 5, 0, false, false) }
                }

            };
            routeCache.Clear();
        }


        public static IReadOnlyList<int> FindPlatformsBetween(int originPlatformId, int destinationPlatformId)
        {
            // 1. Casos triviais: já está no destino ou o mapa não foi inicializado
            if (originPlatformId == destinationPlatformId || PathsOutOfStation == null)
                return Array.Empty<int>();

            // 2. Consulta o cache antes de calcular qualquer coisa (O(1))
            var cacheKey = (originPlatformId, destinationPlatformId);
            if (routeCache.TryGetValue(cacheKey, out var cachedRoute))
                return cachedRoute;

            // 3. Exploração em largura (BFS)
            var queue = new Queue<int>();
            var visited = new HashSet<int> { originPlatformId };
            var parent = new Dictionary<int, int>();

            queue.Enqueue(originPlatformId);
            Console.WriteLine($"[ROUTER] Procurando caminho de {originPlatformId} para {destinationPlatformId}...");
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                // Achou o destino: para a busca imediatamente
                if (current == destinationPlatformId)
                    break;

                if (!PathsOutOfStation.TryGetValue(current, out var outgoingPaths))
                    continue;

                foreach (var path in outgoingPaths)
                {
                    int nextPlatform = path.DestinationPlataformId;

                    // visited.Add retorna true apenas se ainda não estava no conjunto
                    if (visited.Add(nextPlatform))
                    {
                        parent[nextPlatform] = current; // guarda de onde viemos
                        queue.Enqueue(nextPlatform);
                    }
                }
            }

            // Se o destino nunca entrou em 'parent', não existe caminho ligando os dois pontos
            if (!parent.ContainsKey(destinationPlatformId))
                return Array.Empty<int>();

            // 4. Reconstrói o caminho seguindo as migalhas do destino até a origem
            var result = new List<int>();
            int step = destinationPlatformId;

            while (step != originPlatformId)
            {
                result.Add(step);
                step = parent[step];
            }

            result.Reverse();

            // 5. Salva no cache para os próximos trens e devolve
            routeCache[cacheKey] = result;
            return result;
        }

        public static Path? GetNextPath(int currentPlatformId, int destinationPlatformId)
        {
            if (PathsOutOfStation == null) return null;

            if (PathsOutOfStation.TryGetValue(currentPlatformId, out List<Path> availablePaths))
            {
                return availablePaths.FirstOrDefault(p => p.DestinationPlataformId == destinationPlatformId);
            }

            return null;
        }

        public static Path? GetPath(int pathId)
        {
            return PathsOutOfStation.Values.SelectMany(p => p).FirstOrDefault(p => p.Id == pathId);
        }

    }
}
