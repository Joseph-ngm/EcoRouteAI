using System.Text.Json;
using EcoRouteAI.Models;

namespace EcoRouteAI.Services
{
    /// <summary>
    /// Implémentation du service OSM : requête Overpass API + parsing JSON.
    /// Conformité OSM : timeout 35 s, surface &lt; 0.5 degrés carrés (BNFO7).
    /// Données libres, sans clé API payante (BNFO6).
    /// </summary>
    public sealed class OsmService : IOsmService, IDisposable
    {
        private const string OverpassUrl = "https://overpass-api.de/api/interpreter";
        private const int TimeoutSeconds = 35;
        private const double MaxBboxArea = 0.5;  // degrés carrés (fair-use)

        
        private readonly HttpClient _http;

        public OsmService()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds + 5)
            };

            // userAgent
            _http.DefaultRequestHeaders.Add("User-Agent", "EcoRouteAI/1.0 (Contact: pflorwensky@gmail.com)");

            // Forcer l'acceptation du JSON
            _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        
        }

        /// <inheritdoc/>
        public async Task<OsmData> FetchRoadsAsync(double south, double west,
                                                    double north, double east,
                                                    IProgress<string>? progress = null,
                                                    CancellationToken ct = default)
        {
            // ── Validation fair-use
            double area = (north - south) * (east - west);
            if (area > MaxBboxArea)
                throw new InvalidOperationException(
                    $"Zone trop grande ({area:F3} degrés²). Maximum : {MaxBboxArea} degrés².");

            // ── Construction de la requête Overpass QL 
            string bbox = $"{south},{west},{north},{east}";
            string query = $"""
                [out:json][timeout:{TimeoutSeconds}];
                (
                  way["highway"~"motorway|trunk|primary|secondary|tertiary|
                               residential|living_street|service|unclassified|road"]
                     ({bbox});
                );
                out body;
                >;
                out skel qt;
                """;

            progress?.Report("Envoi de la requête Overpass API…");

            // ── Appel HTTP (BNF11 : erreurs capturées) 
            var content = new FormUrlEncodedContent([new("data", query)]);
            var response = await _http.PostAsync(OverpassUrl, content, ct);
            response.EnsureSuccessStatusCode();

            progress?.Report("Réception des données OSM…");

            var json = await response.Content.ReadAsStringAsync(ct);
            var osmData = ParseOverpassJson(json, progress);

            progress?.Report($"✔ {osmData.Nodes.Count} nœuds | {osmData.Ways.Count} voies chargés.");
            return osmData;
        }

        // ── Parsing JSON Overpass (System.Text.Json) 

        private static OsmData ParseOverpassJson(string json, IProgress<string>? progress)
        {
            progress?.Report("Parsing JSON…");

            using var doc = JsonDocument.Parse(json);
            var elements = doc.RootElement.GetProperty("elements");

            var nodes = new List<OsmNode>();
            var ways = new List<OsmWay>();

            foreach (var el in elements.EnumerateArray())
            {
                string type = el.GetProperty("type").GetString() ?? "";

                if (type == "node")
                {
                    nodes.Add(new OsmNode
                    {
                        Id = el.GetProperty("id").GetInt64(),
                        Lat = el.GetProperty("lat").GetDouble(),
                        Lon = el.GetProperty("lon").GetDouble(),
                    });
                }
                else if (type == "way")
                {
                    var nodeIds = el.GetProperty("nodes")
                                    .EnumerateArray()
                                    .Select(n => n.GetInt64())
                                    .ToList();

                    string highway = "";
                    bool oneWay = false;
                    double? speed = null;

                    if (el.TryGetProperty("tags", out var tags))
                    {
                        if (tags.TryGetProperty("highway", out var hw))
                            highway = hw.GetString() ?? "unclassified";

                        if (tags.TryGetProperty("oneway", out var ow))
                            oneWay = ow.GetString() is "yes" or "1" or "true";

                        if (tags.TryGetProperty("maxspeed", out var ms) &&
                            double.TryParse(ms.GetString(), out double parsed))
                            speed = parsed;
                    }

                    ways.Add(new OsmWay
                    {
                        Id = el.GetProperty("id").GetInt64(),
                        NodeIds = nodeIds,
                        HighwayType = highway,
                        IsOneWay = oneWay,
                        MaxSpeed = speed,
                    });
                }
            }

            return new OsmData { Nodes = nodes, Ways = ways };
        }

        public void Dispose() => _http.Dispose();
    }
}