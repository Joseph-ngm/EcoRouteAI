namespace EcoRouteAI.Models
{
    /// <summary>
    /// Résultat complet retourné par l'algorithme A* après calcul d'itinéraire.
    /// Contient le chemin et les statistiques décomposées (US15-US18).
    /// </summary>
    public class RouteResult
    {
        // ── Chemin
        /// <summary>Liste ordonnée des nœuds formant le chemin optimal.</summary>
        public List<RoadNode> Path { get; init; } = [];

        /// <summary>Liste ordonnée des arcs empruntés.</summary>
        public List<RoadEdge> Edges { get; init; } = [];

        // ── Statistiques décomposées
        public double TotalDistanceKm { get; init; }
        public double TotalTraffic { get; init; }
        public double TotalPollution { get; init; }
        public double TotalSafety { get; init; }
        public double TotalCost { get; init; }

        /// <summary>Durée estimée en minutes (distance / vitesse moyenne).</summary>
        public double EstimatedDurationMin { get; init; }

        // ── Métriques de performance
        public int NodesExplored { get; init; }
        public long ComputationTimeMs { get; init; }

        public bool IsSuccess => Path.Count > 0;

        public override string ToString() =>
            $"Chemin : {Path.Count} nœuds | {TotalDistanceKm:F2} km | " +
            $"{EstimatedDurationMin:F0} min | Coût={TotalCost:F4} | " +
            $"Explorés={NodesExplored} | {ComputationTimeMs} ms";
    }
}