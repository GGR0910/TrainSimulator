
namespace TrainSimulator.Domain
{
    internal class Path
    {
        public int Id {  get; set; }
        public int StationAId { get; set; }
        public int StationBId { get; set; }
        public List<RailPath> RailPaths { get; set; }
        public int FreePathsAmount { get {  return RailPaths.Where(r => !r.IsClosed).Count(); }  }

        public Path(int id, int stationAId, int stationBId, List<RailPath> railPaths)
        {
            Id = id;
            StationAId = stationAId;
            StationBId = stationBId;
            RailPaths = railPaths;
        }
    }
}
