using System;
using System.Collections.Generic;

namespace EcoRouteAI.Core
{
    /// <summary>
    /// Structure de données : File de Priorité personnalisée.
    /// Implémentation : Tas Binaire (Min-Heap) stocké dans un tableau dynamique (List).
    /// </summary>
    /// <remarks>
    /// Invariant de structure : 
    /// 1. Arbre binaire complet : tous les niveaux sont remplis sauf éventuellement le dernier.
    /// 2. Propriété de Tas-Min : Pour tout nœud i (autre que la racine), Priorité(Parent(i)) <= Priorité(i).
    /// 
    /// Relations d'indexation :
    /// - Parent(i) = (i - 1) / 2
    /// - Enfant_Gauche(i) = 2i + 1
    /// - Enfant_Droit(i) = 2i + 2
    /// 
    /// Complexité Spatiale : O(N) pour stocker les N éléments.
    /// </remarks>
    public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        /// <summary> Support de stockage : Liste gérée comme un tableau pour représenter l'arbre. </summary>
        private readonly List<(TElement Element, TPriority Priority)> _nodes = new();

        /// <summary> Retourne le nombre d'éléments actuellement dans la file. </summary>
        /// <complexity>O(1)</complexity>
        public int Count => _nodes.Count;

        /// <summary>
        /// Ajoute un élément à la file de priorité.
        /// </summary>
        /// <param name="element">Donnée à stocker.</param>
        /// <param name="priority">Valeur de priorité (Min-Heap : plus elle est petite, plus elle est prioritaire).</param>
        /// <pre>Aucune.</pre>
        /// <post>L'élément est inséré à sa place logique ; Count est incrémenté de 1.</post>
        /// <algorithm>
        /// 1. Ajouter l'élément à la fin de la liste (maintient l'arbre complet).
        /// 2. Appeler BubbleUp pour rétablir la propriété de Tas-Min vers le haut.
        /// </algorithm>
        /// <complexity>
        /// Temporelle : O(log N) - l'élément remonte au pire la hauteur de l'arbre.
        /// </complexity>
        public void Enqueue(TElement element, TPriority priority)
        {
            _nodes.Add((element, priority));
            BubbleUp(_nodes.Count - 1);
        }

        /// <summary>
        /// Retire et retourne l'élément ayant la priorité la plus faible (la racine du tas).
        /// </summary>
        /// <returns>L'élément prioritaire.</returns>
        /// <exception cref="InvalidOperationException">Si la file est vide.</exception>
        /// <pre>Count > 0.</pre>
        /// <post>La racine est supprimée ; le dernier élément est repositionné ; Count est décrémenté de 1.</post>
        /// <algorithm>
        /// 1. Sauvegarder la racine (_nodes[0]).
        /// 2. Déplacer le dernier élément à l'index 0.
        /// 3. Supprimer le dernier emplacement.
        /// 4. Appeler BubbleDown pour rétablir la propriété de Tas-Min vers le bas.
        /// </algorithm>
        /// <complexity>
        /// Temporelle : O(log N) - l'élément descend au pire la hauteur de l'arbre.
        /// </complexity>
        public TElement Dequeue()
        {
            if (_nodes.Count == 0)
                throw new InvalidOperationException("La file de priorité est vide.");

            var root = _nodes[0].Element;
            var lastIndex = _nodes.Count - 1;

            // Remplacement de la racine par le dernier élément
            _nodes[0] = _nodes[lastIndex];
            _nodes.RemoveAt(lastIndex);

            // Rétablir l'ordre si des éléments subsistent
            if (_nodes.Count > 0)
            {
                BubbleDown(0);
            }

            return root;
        }

        /// <summary>
        /// Rétablit la propriété de Tas-Min en faisant remonter un élément.
        /// </summary>
        /// <param name="index">Index de l'élément à vérifier.</param>
        /// <pseudo-code>
        /// TANT QUE index > 0 :
        ///   parent = (index - 1) / 2
        ///   SI priorité[index] < priorité[parent] :
        ///     Echanger(index, parent)
        ///     index = parent
        ///   SINON :
        ///     SORTIR (Propriété respectée)
        /// </pseudo-code>
        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;

                // Comparaison : si l'enfant est >= parent, stop.
                if (_nodes[index].Priority.CompareTo(_nodes[parentIndex].Priority) >= 0)
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        /// <summary>
        /// Rétablit la propriété de Tas-Min en faisant descendre un élément.
        /// </summary>
        /// <param name="index">Index de l'élément à vérifier (généralement la racine après un Dequeue).</param>
        /// <pseudo-code>
        /// TANT QUE vrai :
        ///   gauche = 2 * index + 1
        ///   droit = 2 * index + 2
        ///   plusPetit = index
        ///   SI gauche existe ET priorité[gauche] < priorité[plusPetit] : plusPetit = gauche
        ///   SI droit existe ET priorité[droit] < priorité[plusPetit] : plusPetit = droit
        ///   SI plusPetit == index : SORTIR
        ///   Echanger(index, plusPetit)
        ///   index = plusPetit
        /// </pseudo-code>
        private void BubbleDown(int index)
        {
            int lastIndex = _nodes.Count - 1;
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                // Test de l'enfant gauche
                if (leftChild <= lastIndex &&
                    _nodes[leftChild].Priority.CompareTo(_nodes[smallest].Priority) < 0)
                {
                    smallest = leftChild;
                }

                // Test de l'enfant droit
                if (rightChild <= lastIndex &&
                    _nodes[rightChild].Priority.CompareTo(_nodes[smallest].Priority) < 0)
                {
                    smallest = rightChild;
                }

                // Si le parent est déjà le plus petit des trois, l'invariant est respecté
                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        /// <summary>
        /// Permute deux éléments dans la liste de nœuds.
        /// </summary>
        /// <param name="i">Premier index.</param>
        /// <param name="j">Second index.</param>
        /// <complexity>O(1)</complexity>
        private void Swap(int i, int j)
        {
            var temp = _nodes[i];
            _nodes[i] = _nodes[j];
            _nodes[j] = temp;
        }
    }
}