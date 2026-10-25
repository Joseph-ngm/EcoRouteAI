using System.Diagnostics;
using EcoRouteAI.Models;

namespace EcoRouteAI.Core
{
    /// <summary>
    /// Implémentation de l'algorithme A* (A-Star) multi-critères.
    /// </summary>
    /// <remarks>
    /// Principe : Recherche informée minimisant $f(n) = g(n) + h(n)$.
    /// - g(n) : Coût cumulé (Distance + Trafic + Sécurité).
    /// - h(n) : Estimation heuristique (Distance Haversine).
    ///
    /// Complexité Temporelle : $O(E \log V)$ grâce à l'utilisation d'une PriorityQueue (Tas Binaire).
    /// Complexité Spatiale : $O(V + E)$ pour stocker le graphe et l'état des nœuds explorés.
    /// </remarks>
    public sealed class AStarAlgorithm
    {
        private readonly RoadGraph _graph;
        private readonly CostWeights _weights;

        /// <summary>
        /// Initialise l'algorithme avec le graphe et les poids de pondération.
        /// </summary>
        /// <pre>graph != null, weights != null.</pre>
        public AStarAlgorithm(RoadGraph graph, CostWeights weights)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _weights = weights ?? throw new ArgumentNullException(nameof(weights));
        }

        /// <summary>
        /// Calcule le chemin optimal entre deux nœuds OSM.
        /// </summary>
        /// <param name="startId">ID de départ.</param>
        /// <param name="goalId">ID de destination.</param>
        /// <returns>Un <see cref="RouteResult"/> détaillant le trajet et les statistiques.</returns>
        /// <post>Retourne un chemin si le graphe est connexe, sinon un échec.</post>
        public RouteResult FindPath(long startId, long goalId)
        {
            var sw = Stopwatch.StartNew();

            // Validation des points d'entrée 
            if (!_graph.TryGetNode(startId, out var startNode) || startNode is null)
                return FailedResult(sw, 0);

            if (!_graph.TryGetNode(goalId, out var goalNode) || goalNode is null)
                return FailedResult(sw, 0);

            // Initialisation des structures A* 

            // OpenSet : File de priorité stockant les nœuds à explorer, triés par f(n).
            var openSet = new PriorityQueue<PathNode, double>();
          

            // gCosts : Table de hachage stockant le meilleur score g connu pour chaque ID.
            var gCosts = new Dictionary<long, double>();

            // ClosedSet : Ensemble des nœuds dont l'exploration est finalisée.
            var visited = new HashSet<long>();
            int explored = 0;

            var initial = new PathNode(startNode, null, null,
                                       gCost: 0,
                                       hCost: Heuristic(startNode, goalNode));

            openSet.Enqueue(initial, initial.FCost);
            gCosts[startId] = 0;

            // Boucle de recherche 
            while (openSet.Count > 0)
            {
                // Extraction du nœud ayant le plus petit f(n) - O(log V)
                var current = openSet.Dequeue();
                explored++;

                if (visited.Contains(current.Node.Id))
                    continue;

                visited.Add(current.Node.Id);

                // Condition de succès : l'objectif est le nœud le plus prioritaire
                if (current.Node.Id == goalId)
                {
                    sw.Stop();
                    return BuildResult(current, sw.ElapsedMilliseconds, explored);
                }

                // Expansion : Analyse des arcs adjacents
                foreach (var edge in _graph.GetNeighbors(current.Node.Id))
                {
                    if (visited.Contains(edge.ToNodeId))
                        continue;

                    if (!_graph.TryGetNode(edge.ToNodeId, out var neighborNode) || neighborNode is null)
                        continue;

                    // Calcul du coût g(n) = g(parent) + coût_arc (intégrant trafic/écologie)
                    double tentativeG = current.GCost + _weights.ComputeEdgeCost(edge, neighborNode);

                    // Si ce nouveau chemin vers le voisin est moins bon que celui déjà connu
                    if (gCosts.TryGetValue(edge.ToNodeId, out double bestG) && tentativeG >= bestG)
                        continue;

                    // Mise à jour du meilleur chemin connu vers ce voisin
                    gCosts[edge.ToNodeId] = tentativeG;

                    double h = Heuristic(neighborNode, goalNode);
                    var pathNode = new PathNode(neighborNode, current, edge, tentativeG, h);

                    // Insertion dans la file pour exploration future - O(log V)
                    openSet.Enqueue(pathNode, pathNode.FCost);
                }
            }

            return FailedResult(sw, explored);
        }

        /// <summary>
        /// Calcule l'estimation h(n) via la distance de Haversine.
        /// </summary>
        /// <remarks>
        /// Pour que A* soit optimal, h(n) doit être admissible ($h(n) \le h^*(n)$).
        /// Comme on multiplie la distance minimale (vol d'oiseau) par le poids 
        /// de distance, on ne surestime jamais le coût réel du trajet.
        /// </remarks>
        private double Heuristic(RoadNode from, RoadNode to)
        {
            double distKm = GeoMath.HaversineKm(from.Latitude, from.Longitude,
                                                  to.Latitude, to.Longitude);
            return distKm * _weights.Distance;
        }

        /// <summary>
        /// Agrège les données du chemin final pour le compte-rendu utilisateur.
        /// </summary>
        private static RouteResult BuildResult(PathNode goal, long elapsedMs, int explored)
        {
            var pathNodes = goal.ReconstructPath();

            var nodes = pathNodes.Select(p => p.Node).ToList();
            var edges = pathNodes.Skip(1).Select(p => p.Edge!).ToList();

            double totalDist = edges.Sum(e => e.DistanceKm);
            double totalTraf = edges.Sum(e => e.TrafficLevel);
            double totalPoll = nodes.Sum(n => n.Pollution);
            double totalSafety = nodes.Sum(n => n.Safety);
            double totalCost = goal.GCost;

            // Estimation de la durée (vitesse moyenne arbitraire par défaut : 50 km/h)
            double avgSpeed = edges.Count > 0
                                 ? edges.Average(e => RoadEdge.GetDefaultSpeed(e.HighwayType))
                                 : 50.0;
            double durationMin = (totalDist / avgSpeed) * 60.0;

            return new RouteResult
            {
                Path = nodes,
                Edges = edges,
                TotalDistanceKm = totalDist,
                TotalTraffic = totalTraf,
                TotalPollution = totalPoll,
                TotalSafety = totalSafety,
                TotalCost = totalCost,
                EstimatedDurationMin = durationMin,
                NodesExplored = explored,
                ComputationTimeMs = elapsedMs,
            };
        }

        // En cas d'échec (nœud introuvable ou graphe non connexe), retourne un résultat partiel avec les statistiques d'exploration.
        private static RouteResult FailedResult(Stopwatch sw, int explored)
        {
            sw.Stop();
            return new RouteResult
            {
                NodesExplored = explored,
                ComputationTimeMs = sw.ElapsedMilliseconds,
            };
        }
    }
}