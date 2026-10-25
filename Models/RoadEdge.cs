namespace EcoRouteAI.Models
{
    /// <summary>
    /// Représente un arc (segment de route) reliant deux nœuds OSM.
    /// Porte les données de distance, type de route et trafic simulé.
    /// </summary>
    public class RoadEdge
    {
        public long FromNodeId { get; set; }
        public long ToNodeId { get; set; }

        /// <summary>Longueur du segment en kilomètres (calculée via Haversine).</summary>
        public double DistanceKm { get; set; }

        /// <summary>Type de route OSM (motorway, primary, residential, …).</summary>
        public string HighwayType { get; set; } = "unclassified";

        /// <summary>Vitesse maximale en km/h (dérivée du highway type si absente du tag OSM).</summary>
        public double MaxSpeedKmh { get; set; } = 50.0;

        /// <summary>Niveau de trafic simulé [0.0 – 1.0].</summary>
        public double TrafficLevel { get; set; } = 0.3;

        /// <summary>Indique si la rue est à sens unique.</summary>
        public bool IsOneWay { get; set; } = false;

        // ── Vitesses par défaut selon le type OSM 
        private static readonly Dictionary<string, double> DefaultSpeeds = new()
        {
            ["motorway"] = 110,
            ["motorway_link"] = 80,
            ["trunk"] = 90,
            ["trunk_link"] = 70,
            ["primary"] = 70,
            ["primary_link"] = 60,
            ["secondary"] = 60,
            ["secondary_link"] = 50,
            ["tertiary"] = 50,
            ["tertiary_link"] = 40,
            ["residential"] = 30,
            ["living_street"] = 10,
            ["service"] = 20,
            ["unclassified"] = 40,
            ["road"] = 40,
        };

        /// <summary>Retourne la vitesse par défaut du type de route.</summary>
        public static double GetDefaultSpeed(string highwayType) =>
            DefaultSpeeds.TryGetValue(highwayType, out var speed) ? speed : 40.0;

        public override string ToString() =>
            $"Edge {FromNodeId}→{ToNodeId} | {DistanceKm:F3} km | {HighwayType} | trafic={TrafficLevel:P0}";
    }
}