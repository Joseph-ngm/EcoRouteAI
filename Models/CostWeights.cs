namespace EcoRouteAI.Models
{
    /// <summary>
    /// Poids de la fonction de coût multi-critères utilisée par A*.
    /// g(n) = Distance*W_dist + Traffic*W_traffic + Pollution*W_poll + (1-Safety)*W_safety
    /// </summary>
    public class CostWeights
    {
        public double Distance { get; set; } = 0.25;
        public double Traffic { get; set; } = 0.25;
        public double Pollution { get; set; } = 0.25;
        public double Safety { get; set; } = 0.25;

        // ── Préréglages prédéfinis 

        /// <summary>Équilibré : tous les critères au même niveau.</summary>
        public static CostWeights Balanced() => new()
        {
            Distance = 0.25,
            Traffic = 0.25,
            Pollution = 0.25,
            Safety = 0.25
        };

        /// <summary>Éco : minimise la pollution, favorise les routes résidentielles.</summary>
        public static CostWeights Eco() => new()
        {
            Distance = 0.10,
            Traffic = 0.20,
            Pollution = 0.50,
            Safety = 0.20
        };

        /// <summary>Rapide : minimise le trafic, favorise les grandes voies.</summary>
        public static CostWeights Fast() => new()
        {
            Distance = 0.40,
            Traffic = 0.50,
            Pollution = 0.05,
            Safety = 0.05
        };

        /// <summary>Sécurisé : routes résidentielles à basse vitesse.</summary>
        public static CostWeights Safe() => new()
        {
            Distance = 0.15,
            Traffic = 0.15,
            Pollution = 0.10,
            Safety = 0.60
        };

        /// <summary>
        /// Calcule le coût d'un arc selon les poids courants.
        /// Extensible 
        /// </summary>
        public double ComputeEdgeCost(RoadEdge edge, RoadNode toNode)
        {
            double distCost = edge.DistanceKm * Distance;
            double trafficCost = edge.TrafficLevel * Traffic;
            double pollCost = toNode.Pollution * Pollution;
            double safetyCost = (1.0 - toNode.Safety) * Safety;

            return distCost + trafficCost + pollCost + safetyCost;
        }

        public override string ToString() =>
            $"Dist={Distance:P0} | Traffic={Traffic:P0} | Poll={Pollution:P0} | Safety={Safety:P0}";
    }
}