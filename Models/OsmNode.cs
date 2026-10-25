using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoRouteAI.Models
{
    public sealed class OsmNode
    {
        public long Id { get; init; }
        public double Lat { get; init; }
        public double Lon { get; init; }
    }
}
