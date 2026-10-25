using EcoRouteAI.Models;

namespace EcoRouteAI.Core
{
    /// <summary>
    /// Représente un état de recherche (wrapper) pour un nœud du graphe durant l'exécution de A*.
    /// </summary>
    /// <remarks>
    /// Cette classe encapsule les fonctions de coût nécessaires à l'algorithme :
    /// - g(n) : Coût cumulé réel.
    /// - h(n) : Estimation heuristique.
    /// - f(n) : Priorité totale.
    /// 
    /// Complexité Spatiale : O(1) par instance, mais O(V) au total dans le cadre de l'algorithme.
    /// </remarks>
    internal sealed class PathNode : IComparable<PathNode>
    {
        /// <summary> Le nœud routier réel associé à cet état. </summary>
        public RoadNode Node { get; }

        /// <summary> Référence vers le nœud parent pour permettre la remontée du chemin. </summary>
        public PathNode? Parent { get; }

        /// <summary> L'arc (route) emprunté pour arriver à ce nœud depuis le parent. </summary>
        public RoadEdge? Edge { get; }

        /// <summary> 
        /// Coût réel g(n) : Somme des poids des arcs depuis le départ jusqu'à ce nœud. 
        /// </summary>
        public double GCost { get; }

        /// <summary> 
        /// Coût heuristique h(n) : Estimation (souvent Haversine) du coût restant vers la cible. 
        /// </summary>
        public double HCost { get; }

        /// <summary> 
        /// Coût total f(n) : Priorité utilisée par la file de priorité.
        /// $f(n) = g(n) + h(n)$
        /// </summary>
        public double FCost => GCost + HCost;

        /// <summary>
        /// Initialise une nouvelle instance de l'état de recherche.
        /// </summary>
        /// <pre>node != null, gCost >= 0, hCost >= 0.</pre>
        /// <post>Une instance immuable de l'état est créée.</post>
        /// <complexity>O(1)</complexity>
        public PathNode(RoadNode node, PathNode? parent, RoadEdge? edge,
                        double gCost, double hCost)
        {
            Node = node;
            Parent = parent;
            Edge = edge;
            GCost = gCost;
            HCost = hCost;
        }

        /// <summary>
        /// Reconstruit la séquence de nœuds du départ jusqu'à l'instance actuelle.
        /// </summary>
        /// <returns>Une liste ordonnée de PathNodes formant le chemin optimal.</returns>
        /// <pre>L'instance actuelle doit être le nœud destination (Goal).</pre>
        /// <post>Retourne une liste où List[0] est le départ et List[Last] est la destination.</post>
        /// <pseudo-code>
        /// 1. Créer une liste vide 'chemin'.
        /// 2. courant = cette_instance.
        /// 3. TANT QUE courant n'est pas nul :
        ///     a. Ajouter courant à 'chemin'.
        ///     b. courant = courant.Parent.
        /// 4. Inverser 'chemin' pour avoir l'ordre chronologique.
        /// 5. Retourner 'chemin'.
        /// </pseudo-code>
        /// <complexity>
        /// Temporelle : O(L) où L est la longueur du chemin (nombre de parents).
        /// Spatiale : O(L) pour stocker la liste résultante.
        /// </complexity>
        public List<PathNode> ReconstructPath()
        {
            var path = new List<PathNode>();
            var current = this;

            while (current is not null)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Compare deux nœuds basés sur leur coût total f(n).
        /// Requis pour le fonctionnement du Min-Heap dans la PriorityQueue.
        /// </summary>
        /// <complexity>O(1)</complexity>
        public int CompareTo(PathNode? other) =>
            FCost.CompareTo(other?.FCost ?? double.MaxValue);
    }
}