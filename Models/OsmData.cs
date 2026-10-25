using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoRouteAI.Models
{
    public sealed class OsmData
    {
        public List<OsmNode> Nodes { get; init; } = [];
        public List<OsmWay> Ways { get; init; } = [];
    }
}
