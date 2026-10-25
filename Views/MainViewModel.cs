using System.ComponentModel;
using System.Runtime.CompilerServices;
using EcoRouteAI.Core;
using EcoRouteAI.Models;
using EcoRouteAI.Services;

namespace EcoRouteAI.ViewModels
{
    /// <summary>
    /// ViewModel principal — Architecture MVVM stricte (BNF09).
    /// Découple la logique métier de la vue WinForm.
    /// Testable unitairement sans WinForm (BNF15).
    /// </summary>
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        // ── Services ──────────────────────────────────────────────────────────
        private readonly IOsmService _osmService;

        // ── État ──────────────────────────────────────────────────────────────
        private RoadGraph _graph = new();
        private RoadNode? _startNode;
        private RoadNode? _goalNode;
        private RouteResult? _lastResult;
        private CostWeights _weights = CostWeights.Balanced();
        private string _statusMessage = "Prêt.";
        private bool _isBusy;
        private string _selectedPreset = "Équilibré";

        // ── Propriétés liées à la vue ─────────────────────────────────────────

        public RoadGraph Graph
        {
            get => _graph;
            private set { _graph = value; OnPropertyChanged(); OnPropertyChanged(nameof(GraphInfo)); }
        }

        public string GraphInfo =>
            _graph.NodeCount > 0
            ? $"{_graph.NodeCount} nœuds | {_graph.EdgeCount} arcs chargés"
            : "Aucun réseau chargé.";

        public RoadNode? StartNode
        {
            get => _startNode;
            set { _startNode = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanCalculate)); }
        }

        public RoadNode? GoalNode
        {
            get => _goalNode;
            set { _goalNode = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanCalculate)); }
        }

        public RouteResult? LastResult
        {
            get => _lastResult;
            private set { _lastResult = value; OnPropertyChanged(); }
        }

        public CostWeights Weights
        {
            get => _weights;
            set { _weights = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsReady)); }
        }

        public bool IsReady => !_isBusy;
        public bool CanCalculate => !_isBusy && _startNode is not null && _goalNode is not null
                                             && _graph.NodeCount > 0;

        public string SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                Weights = value switch
                {
                    "Éco" => CostWeights.Eco(),
                    "Rapide" => CostWeights.Fast(),
                    "Sécurisé" => CostWeights.Safe(),
                    _ => CostWeights.Balanced(),
                };
                OnPropertyChanged();
            }
        }

        public static IReadOnlyList<string> Presets { get; } =
            ["Équilibré", "Éco", "Rapide", "Sécurisé"];

        // ── Constructeur 

        public MainViewModel() : this(new OsmService()) { }

        public MainViewModel(IOsmService osmService)
        {
            _osmService = osmService ?? throw new ArgumentNullException(nameof(osmService));
        }

        // ── Commandes (appelées par la vue) 

        /// <summary>US01 — Charge le réseau OSM de la zone visible.</summary>
        public async Task LoadOsmRoutesAsync(double south, double west,
                                              double north, double east,
                                              IProgress<string>? progress = null,
                                              CancellationToken ct = default)
        {
            IsBusy = true;
            try
            {
                StatusMessage = "Chargement OSM en cours…";

                var osmData = await _osmService.FetchRoadsAsync(south, west, north, east, progress, ct);
                Graph = GraphBuilder.Build(osmData, progress);
                StartNode = null;
                GoalNode = null;
                LastResult = null;

                StatusMessage = $"{Graph.NodeCount} nœuds et {Graph.EdgeCount} arcs chargés.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Chargement annulé.";
            }
            catch (Exception ex)
            {
                // BNF11 : erreur capturée, affichée sans crash
                StatusMessage = $"Erreur OSM : {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>US03/US04 — Définit départ ou arrivée au clic carte.</summary>
        public void SetWaypoint(double latitude, double longitude)
        {
            if (_graph.NodeCount == 0) return;

            var nearest = _graph.FindNearestNode(latitude, longitude);
            if (nearest is null) return;

            if (StartNode is null)
            {
                StartNode = nearest;
                StatusMessage = $"Départ : nœud #{nearest.Id}";
            }
            else
            {
                GoalNode = nearest;
                StatusMessage = $"Arrivée : nœud #{nearest.Id} — Prêt à calculer.";
            }
        }

        /// <summary>US05 — Lance le calcul A*.</summary>
        public async Task CalculateRouteAsync()
        {
            if (!CanCalculate) return;

            IsBusy = true;
            try
            {
                StatusMessage = "Calcul A* en cours…";

                var astar = new AStarAlgorithm(_graph, _weights);
                var result = await Task.Run(() => astar.FindPath(StartNode!.Id, GoalNode!.Id));

                LastResult = result;
                StatusMessage = result.IsSuccess
                    ? $" {result.TotalDistanceKm:F2} km | {result.EstimatedDurationMin:F0} min | {result.ComputationTimeMs} ms"
                    : "Aucun chemin trouvé entre ces deux points.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>US19 — Efface le tracé et réinitialise les waypoints.</summary>
        public void ClearRoute()
        {
            StartNode = null;
            GoalNode = null;
            LastResult = null;
            StatusMessage = "Tracé effacé. Cliquez sur la carte pour définir un départ.";
        }

        // ── INotifyPropertyChanged 

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            if (_osmService is IDisposable d) d.Dispose();
        }
    }
}