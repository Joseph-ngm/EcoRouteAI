using System.Text.Json;
using EcoRouteAI.Models;

namespace EcoRouteAI.Services
{
    /// <summary>
    /// Pont de communication C# <=> Leaflet via WebView2 postMessage JSON.
    /// BNF08 : utilise le protocole postMessage JSON standardisé.
    /// </summary>
    public static class MapBridgeService
    {
        // ── Types de messages envoyés à Leaflet 

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // ── Construction des messages JSON 

        /// <summary>Envoie l'itinéraire calculé à afficher sur la carte.</summary>
        public static string BuildDrawRouteMessage(RouteResult result)
        {
            var coords = result.Path.Select(n => new double[] { n.Latitude, n.Longitude });

            return Serialize(new
            {
                type = "drawRoute",
                coordinates = coords,
                stats = new
                {
                    distanceKm = result.TotalDistanceKm,
                    durationMin = result.EstimatedDurationMin,
                    nodesExplored = result.NodesExplored,
                    computationMs = result.ComputationTimeMs,
                    totalCost = result.TotalCost,
                }
            });
        }

        /// <summary>Affiche les arêtes OSM sur la carte avec code couleur par type.</summary>
        public static string BuildDrawRoadsMessage(IEnumerable<(RoadNode From, RoadNode To, string Highway)> roads)
        {
            var segments = roads.Select(r => new
            {
                from = new double[] { r.From.Latitude, r.From.Longitude },
                to = new double[] { r.To.Latitude, r.To.Longitude },
                highway = r.Highway,
                color = HighwayColor(r.Highway),
            });

            return Serialize(new { type = "drawRoads", roads = segments });
        }

        /// <summary>Positionne les marqueurs départ/arrivée sur la carte.</summary>
        public static string BuildMarkersMessage(RoadNode? start, RoadNode? goal)
        {
            return Serialize(new
            {
                type = "setMarkers",
                start = start is null ? null : new double[] { start.Latitude, start.Longitude },
                goal = goal is null ? null : new double[] { goal.Latitude, goal.Longitude },
            });
        }

        /// <summary>Demande à Leaflet d'effacer le tracé et les marqueurs.</summary>
        public static string BuildClearMessage() =>
            Serialize(new { type = "clear" });

        // ── Code couleur par type de route (BNF14) 

        public static string HighwayColor(string highway) => highway switch
        {
            "motorway" or "motorway_link" => "#e8384f",  // rouge
            "trunk" or "trunk_link" => "#f97316",  // orange foncé
            "primary" or "primary_link" => "#f59e0b",  // ambre
            "secondary" or "secondary_link" => "#84cc16",  // vert lime
            "tertiary" or "tertiary_link" => "#22c55e",  // vert
            "residential" => "#60a5fa",  // bleu clair
            "living_street" => "#818cf8",  // indigo
            "service" => "#94a3b8",  // gris
            _ => "#cbd5e1",  // gris clair
        };

        private static string Serialize(object payload) =>
            JsonSerializer.Serialize(payload, JsonOpts);
    }
}