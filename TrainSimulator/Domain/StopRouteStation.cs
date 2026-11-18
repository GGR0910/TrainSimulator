using System;
using System.Collections.Generic;
using System.Text;

namespace TrainSimulator.Domain
{
    internal class StopRouteStation
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public bool EndStation { get; set; }
        public Station Station { get; set; }

        public StopRouteStation(int id, int order, bool endStation, Station station)
        {
            Id = id;
            Order = order;
            EndStation = endStation;
            Station = station;
        }
    }
}
