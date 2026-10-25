namespace EcoRouteAI.Models
{
    /// <summary>
    /// Représente un nœud du réseau routier OSM (intersection ou point de passage).
    /// Correspond à un sommet dans le graphe orienté pondéré.
    /// </summary>
    public class RoadNode
    {
        // Identité OSM
        public long Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // --- Propriétés environnementales [0.0 – 1.0] ---
        /// <summary>Niveau de pollution ambiant du nœud (0 = propre, 1 = très pollué).</summary>
        public double Pollution { get; set; } = 0.3;

        /// <summary>Score de sécurité du nœud (0 = dangereux, 1 = très sûr).</summary>
        public double Safety { get; set; } = 0.7;

        public RoadNode() { }

        public RoadNode(long id, double latitude, double longitude)
        {
            Id = id;
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString() =>
            $"Node#{Id} ({Latitude:F5}, {Longitude:F5})";
    }
}