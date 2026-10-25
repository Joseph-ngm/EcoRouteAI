using EcoRouteAI.Models;

namespace EcoRouteAI.Core
{
    /// <summary>
    /// Représente le réseau routier sous forme de Graphe Orienté Pondéré.
    /// </summary>
    /// <remarks>
    /// Structure : Liste d'adjacence implémentée via des dictionnaires pour un accès rapide par ID.
    /// Invariant : Chaque nœud présent dans <see cref="_adjacency"/> doit également exister dans <see "_nodes"/>.
    /// 
    /// Complexité Spatiale : $O(V + E)$ où V est le nombre de nœuds et E le nombre d'arcs.
    /// </remarks>
    public class RoadGraph
    {
        // Stockage 

        /// <summary> Table de hachage des nœuds : ID -> Objet RoadNode. </summary>
        private readonly Dictionary<long, RoadNode> _nodes = [];

        /// <summary> Table d'adjacence : ID du nœud -> Liste des arcs sortants. </summary>
        private readonly Dictionary<long, List<RoadEdge>> _adjacency = [];

        // Statistiques

        /// <summary> Retourne le nombre total de sommets (nœuds) dans le graphe. </summary>
        /// <complexity>$O(1)$</complexity>
        public int NodeCount => _nodes.Count;

        /// <summary> Calcule le nombre total d'arcs (rues) dans le graphe. </summary>
        /// <complexity>$O(V)$ dans cette implémentation (somme des listes). </complexity>
        public int EdgeCount => _adjacency.Values.Sum(l => l.Count);

        // Ajout de données

        /// <summary>
        /// Ajoute ou met à jour un nœud dans le graphe.
        /// </summary>
        /// <param name="node">Le nœud à insérer.</param>
        /// <pre>node != null.</pre>
        /// <post>Le nœud est indexé et sa liste d'adjacence est initialisée si absente.</post>
        /// <complexity>Temporelle : $O(1)$ en moyenne.</complexity>
        public void AddNode(RoadNode node)
        {
            _nodes[node.Id] = node;
            _adjacency.TryAdd(node.Id, []);
        }

        /// <summary>
        /// Ajoute un arc orienté reliant deux nœuds.
        /// </summary>
        /// <param name="edge">L'arc contenant les IDs source et destination.</param>
        /// <pre>edge != null.</pre>
        /// <post>L'arc est ajouté à la liste d'adjacence du nœud source.</post>
        /// <complexity>Temporelle : $O(1)$ en moyenne.</complexity>
        public void AddEdge(RoadEdge edge)
        {
            if (!_adjacency.ContainsKey(edge.FromNodeId))
                _adjacency[edge.FromNodeId] = [];

            _adjacency[edge.FromNodeId].Add(edge);
        }

        // Accès

        /// <summary> Tente de récupérer un nœud sans lever d'exception. </summary>
        /// <complexity>$O(1)$</complexity>
        public bool TryGetNode(long id, out RoadNode? node) =>
            _nodes.TryGetValue(id, out node);

        /// <summary> Récupère un nœud par son ID ou retourne null. </summary>
        /// <complexity>$O(1)$</complexity>
        public RoadNode? GetNode(long id) =>
            _nodes.GetValueOrDefault(id);

        /// <summary>
        /// Retourne les arcs sortants d'un nœud spécifique.
        /// </summary>
        /// <param name="nodeId">ID du nœud source.</param>
        /// <returns>Une collection d'arcs (voisins) ou une liste vide.</returns>
        /// <complexity>Temporelle : $O(1)$.</complexity>
        public IReadOnlyList<RoadEdge> GetNeighbors(long nodeId) =>
            _adjacency.TryGetValue(nodeId, out var edges) ? edges : [];

        /// <summary> Fournit un itérateur sur tous les nœuds du graphe. </summary>
        public IEnumerable<RoadNode> AllNodes => _nodes.Values;

        // Recherche Spatiale 

        /// <summary>
        /// Effectue une recherche linéaire pour trouver le nœud le plus proche de coordonnées GPS.
        /// </summary>
        /// <param name="latitude">Latitude cible.</param>
        /// <param name="longitude">Longitude cible.</param>
        /// <returns>Le RoadNode le plus proche ou null si le graphe est vide.</returns>
        /// <algorithm>Recherche séquentielle (Brute Force) sur l'ensemble des sommets.</algorithm>
        /// <complexity>
        /// Temporelle : $O(V)$ - parcourt tous les nœuds une fois.
        /// Spatiale : $O(1)$ additionnel.
        /// </complexity>
        public RoadNode? FindNearestNode(double latitude, double longitude)
        {
            RoadNode? nearest = null;
            double minDist = double.MaxValue;

            foreach (var node in _nodes.Values)
            {
                double dist = GeoMath.HaversineKm(latitude, longitude,
                                                   node.Latitude, node.Longitude);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Vide intégralement le graphe pour libérer la mémoire.
        /// </summary>
        /// <post>NodeCount == 0, EdgeCount == 0.</post>
        /// <complexity>Temporelle : $O(V + E)$ pour la suppression des références.</complexity>
        public void Clear()
        {
            _nodes.Clear();
            _adjacency.Clear();
        }

        public override string ToString() =>
            $"RoadGraph : {NodeCount} nœuds | {EdgeCount} arcs";
    }
}