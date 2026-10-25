using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoRouteAI.Models
{
    public sealed class OsmWay
    {
        public long Id { get; init; }
        public List<long> NodeIds { get; init; } = [];
        public string HighwayType { get; init; } = "unclassified";
        public bool IsOneWay { get; init; }
        public double? MaxSpeed { get; init; }
    }
}
