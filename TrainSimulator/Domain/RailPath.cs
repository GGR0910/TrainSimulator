using System;
using System.Collections.Generic;
using System.Text;

namespace TrainSimulator.Domain
{
    internal class RailPath
    {
        public int Id {  get; set; }
        public bool IsClosed {  get; set; }

        public RailPath(int id, bool isClosed)
        {
            Id = id;
            IsClosed = isClosed;
        }

    }
}
