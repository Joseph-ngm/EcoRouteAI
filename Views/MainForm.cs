using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoRouteAI.Models;
using EcoRouteAI.Services;
using EcoRouteAI.ViewModels;

namespace EcoRouteAI.Views
{
    public partial class MainForm : Form
    {
        private readonly MainViewModel _vm;
        private CancellationTokenSource? _loadCts;

        private WebView2Wrapper _mapView = null!;
        private Panel _sidePanel = null!;
        private Panel _header = null!;

        private Button _btnLoadOsm = null!;
        private Button _btnCancelLoad = null!;
        private Label _lblGraphInfo = null!;

        private Panel _cardStart = null!;
        private Panel _cardGoal = null!;
        private Label _lblStart = null!;
        private Label _lblGoal = null!;
        private Button _btnClear = null!;

        private RadioButton _rbBalanced = null!, _rbEco = null!, _rbFast = null!, _rbSafe = null!;

        private TrackBar _tbDistance = null!, _tbTraffic = null!, _tbPollution = null!, _tbSafety = null!;
        private Label _lblDistVal = null!, _lblTrafVal = null!, _lblPollVal = null!, _lblSafeVal = null!;

        private Button _btnCalculate = null!;

        private Label _lblStatDist = null!, _lblStatDur = null!, _lblStatCost = null!,
                      _lblStatNodes = null!, _lblStatTime = null!;

        private Panel _statusBar = null!;
        private Label _lblStatus = null!;
        private Panel _loadingDot = null!;

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color C_BG = Color.FromArgb(10, 14, 26);
        private static readonly Color C_PANEL = Color.FromArgb(14, 20, 36);
        private static readonly Color C_SURFACE = Color.FromArgb(22, 30, 50);
        private static readonly Color C_BORDER = Color.FromArgb(40, 55, 85);
        private static readonly Color C_ACCENT = Color.FromArgb(56, 189, 248);
        private static readonly Color C_GREEN = Color.FromArgb(34, 197, 94);
        private static readonly Color C_RED = Color.FromArgb(239, 68, 68);
        private static readonly Color C_AMBER = Color.FromArgb(251, 191, 36);
        private static readonly Color C_PURPLE = Color.FromArgb(167, 139, 250);
        private static readonly Color C_TEXT = Color.FromArgb(220, 235, 255);
        private static readonly Color C_MED = Color.FromArgb(140, 165, 210);
        private static readonly Color C_DIM = Color.FromArgb(70, 95, 140);

        private static readonly Font F_BODY = new Font("Segoe UI", 9f);
        private static readonly Font F_SMALL = new Font("Segoe UI", 8f);
        private static readonly Font F_MONO = new Font("Consolas", 8.5f);
        private static readonly Font F_BTN = new Font("Segoe UI Semibold", 9f);
        private static readonly Font F_TINY = new Font("Segoe UI", 7.5f);
        private static readonly Font F_CAP = new Font("Segoe UI Semibold", 6.5f);

        // Largeur utilisable dans le panneau (304 - 2*12 padding = 280)
        private const int W = 280;

        public MainForm()
        {
            _vm = new MainViewModel();
            Build();
            BindViewModel();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUCTION
        // ─────────────────────────────────────────────────────────────────────

        private void Build()
        {
            SuspendLayout();
            Text = "EcoRoute AI";
            Size = new Size(1360, 860);
            MinimumSize = new Size(1080, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C_BG;
            ForeColor = C_TEXT;
            Font = F_BODY;
            DoubleBuffered = true;

            BuildMapView();
            BuildSidePanel();
            BuildStatusBar();

            Controls.Add(_mapView);
            Controls.Add(_sidePanel);
            Controls.Add(_statusBar);

            ResumeLayout(false);
            PerformLayout();
        }

        // ── Status bar ────────────────────────────────────────────────────────

        private void BuildStatusBar()
        {
            _loadingDot = new Panel { Size = new Size(7, 7), BackColor = C_GREEN, Visible = false };
            _loadingDot.Paint += (_, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(new SolidBrush(C_GREEN), 0, 0, 7, 7);
            };

            _lblStatus = new Label
            {
                Text = "Prêt — chargez les routes OSM puis cliquez sur la carte",
                ForeColor = C_MED,
                Font = F_SMALL,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
            };

            _statusBar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.FromArgb(8, 12, 22) };
            _statusBar.Paint += (_, e) => {
                using var p = new Pen(C_BORDER, 1);
                e.Graphics.DrawLine(p, 0, 0, _statusBar.Width, 0);
            };

            var dotWrapper = new Panel { Dock = DockStyle.Left, Width = 26, BackColor = Color.Transparent };
            dotWrapper.Controls.Add(_loadingDot);
            _loadingDot.Location = new Point(9, 10);

            _statusBar.Controls.Add(_lblStatus);
            _statusBar.Controls.Add(dotWrapper);
        }

        // ── Map ───────────────────────────────────────────────────────────────

        private void BuildMapView()
        {
            _mapView = new WebView2Wrapper { Dock = DockStyle.Fill };
            _mapView.MapClicked += OnMapClicked;
        }

        // ── Side panel ────────────────────────────────────────────────────────

        private void BuildSidePanel()
        {
            _sidePanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 304,
                BackColor = C_PANEL,
            };
            _sidePanel.Paint += (_, e) => {
                using var p = new Pen(C_BORDER, 1);
                e.Graphics.DrawLine(p, _sidePanel.Width - 1, 0, _sidePanel.Width - 1, _sidePanel.Height);
            };

            // ── Header ────────────────────────────────────────────────────────
            _header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = C_BG };
            _header.Paint += PaintHeader;

            // ── Scrollable body ───────────────────────────────────────────────
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = C_PANEL,
            };

            // Use a single vertical StackPanel (FlowLayout)
            var stack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 12),
            };

            stack.Controls.Add(SectionLabel("RÉSEAU OSM"));
            stack.Controls.Add(Gap(4));
            stack.Controls.Add(BuildOsmBlock());

            stack.Controls.Add(Gap(14));
            stack.Controls.Add(SectionLabel("POINTS DE NAVIGATION"));
            stack.Controls.Add(Gap(4));
            stack.Controls.Add(BuildPointsBlock());

            stack.Controls.Add(Gap(14));
            stack.Controls.Add(SectionLabel("MODE DE NAVIGATION"));
            stack.Controls.Add(Gap(4));
            stack.Controls.Add(BuildPresetsBlock());

            stack.Controls.Add(Gap(14));
            stack.Controls.Add(SectionLabel("POIDS PERSONNALISÉS"));
            stack.Controls.Add(Gap(4));
            stack.Controls.Add(BuildWeightsBlock());

            stack.Controls.Add(Gap(16));
            stack.Controls.Add(BuildCalcBtn());

            stack.Controls.Add(Gap(14));
            stack.Controls.Add(SectionLabel("STATISTIQUES"));
            stack.Controls.Add(Gap(4));
            stack.Controls.Add(BuildStatsBlock());

            scroll.Controls.Add(stack);
            _sidePanel.Controls.Add(scroll);
            _sidePanel.Controls.Add(_header);
        }

        private void PaintHeader(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var ctrl = (Control)s!;
            using var ab = new SolidBrush(C_ACCENT);
            g.FillRectangle(ab, 0, 18, 3, 28);
            using var tf = new Font("Segoe UI Semibold", 13f);
            using var tb = new SolidBrush(C_TEXT);
            g.DrawString("EcoRoute AI", tf, tb, 18, 13);
            using var sf = new Font("Segoe UI", 7.5f);
            using var sb = new SolidBrush(C_DIM);
            g.DrawString("Navigation Multi-Critères  ·  A* + OSM", sf, sb, 19, 38);
            using var pen = new Pen(C_BORDER, 1);
            g.DrawLine(pen, 0, ctrl.Height - 1, ctrl.Width, ctrl.Height - 1);
        }

        // ── Section helpers ───────────────────────────────────────────────────

        private Label SectionLabel(string text) => new Label
        {
            Text = text,
            ForeColor = C_DIM,
            Font = F_CAP,
            Width = W,
            Height = 14,
            TextAlign = ContentAlignment.BottomLeft,
        };

        private Panel Gap(int h) => new Panel { Width = W, Height = h, BackColor = Color.Transparent };

        // ── OSM block ────────────────────────────────────────────────────────

        private Panel BuildOsmBlock()
        {
            var block = new Panel { Width = W, Height = 88, BackColor = Color.Transparent };

            _btnLoadOsm = PrimaryBtn("  Charger les routes OSM", C_ACCENT, C_BG);
            _btnLoadOsm.SetBounds(0, 0, W, 34);
            _btnLoadOsm.Click += async (_, _) => await OnLoadOsmAsync();

            _btnCancelLoad = GhostBtn("✕  Annuler le chargement");
            _btnCancelLoad.ForeColor = C_RED;
            _btnCancelLoad.SetBounds(0, 38, W, 26);
            _btnCancelLoad.Enabled = false;
            _btnCancelLoad.Click += (_, _) => _loadCts?.Cancel();

            _lblGraphInfo = new Label
            {
                Text = "Aucun réseau chargé",
                ForeColor = C_DIM,
                Font = F_TINY,

                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            _lblGraphInfo.SetBounds(0, 68, W, 18);

            block.Controls.AddRange(new Control[] { _btnLoadOsm, _btnCancelLoad, _lblGraphInfo });
            return block;
        }

        // ── Points block ─────────────────────────────────────────────────────

        private Panel BuildPointsBlock()
        {
            var block = new Panel { Width = W, Height = 100, BackColor = Color.Transparent };

            _cardStart = PointCard("A", "Départ  : —", C_GREEN, out _lblStart);
            _cardStart.SetBounds(0, 0, W, 32);

            _cardGoal = PointCard("B", "Arrivée : —", C_RED, out _lblGoal);
            _cardGoal.SetBounds(0, 36, W, 32);

            _btnClear = GhostBtn("🗑  Effacer le tracé et les points");
            _btnClear.SetBounds(0, 72, W, 26);
            _btnClear.Click += (_, _) => {
                _vm.ClearRoute();
                _ = _mapView.PostMessageAsync("{\"type\":\"clear\"}");
            };

            block.Controls.AddRange(new Control[] { _cardStart, _cardGoal, _btnClear });
            return block;
        }

        private Panel PointCard(string letter, string text, Color accent, out Label lbl)
        {
            var card = new Panel { BackColor = C_SURFACE };
            card.Paint += (_, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var ab = new SolidBrush(accent);
                g.FillRectangle(ab, 0, 0, 3, card.Height);
                using var pen = new Pen(C_BORDER, 1);
                g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using var lf = new Font("Segoe UI Semibold", 8f);
                using var lb = new SolidBrush(accent);
                g.DrawString(letter, lf, lb, 8, (card.Height - 14) / 2);
            };

            lbl = new Label
            {
                Text = text,
                ForeColor = C_MED,
                Font = F_SMALL,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            lbl.SetBounds(26, 0, card.Width - 28, card.Height);
            card.Controls.Add(lbl);
            return card;
        }

        // ── Presets block ─────────────────────────────────────────────────────

        private Panel BuildPresetsBlock()
        {
            var block = new Panel { Width = W, Height = 58, BackColor = Color.Transparent };

            var defs = new[] {
                ("⚖", "Équilibré", C_ACCENT),
                ("🌿", "Éco",       C_GREEN),
                ("⚡", "Rapide",    C_AMBER),
                ("🛡", "Sécurisé",  C_PURPLE),
            };

            int btnW = (W - 6) / 4;   // 4 buttons + 3×2px gaps
            RadioButton[] rbs = new RadioButton[4];

            for (int i = 0; i < 4; i++)
            {
                var (icon, name, color) = defs[i];
                var rb = new RadioButton
                {
                    Text = $"{icon}  {name}",
                    Checked = i == 0,
                    Tag = name,
                    Cursor = Cursors.Hand,
                    Appearance = Appearance.Button,
                    FlatStyle = FlatStyle.Flat,
                    Font = F_TINY,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = i == 0 ? color : C_MED,
                    BackColor = i == 0 ? Color.FromArgb(24, color.R, color.G, color.B) : C_SURFACE,
                };
                rb.FlatAppearance.BorderColor = i == 0 ? color : C_BORDER;
                rb.FlatAppearance.BorderSize = 1;
                rb.FlatAppearance.CheckedBackColor = Color.FromArgb(28, color.R, color.G, color.B);
                rb.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, color.R, color.G, color.B);
                rb.SetBounds(i * (btnW + 2), 0, btnW, 58);

                var col = color; // capture for lambda
                rb.CheckedChanged += (s, e) => {
                    if (s is RadioButton r)
                    {
                        r.ForeColor = r.Checked ? col : C_MED;
                        r.BackColor = r.Checked ? Color.FromArgb(24, col.R, col.G, col.B) : C_SURFACE;
                        r.FlatAppearance.BorderColor = r.Checked ? col : C_BORDER;
                    }
                };
                rb.CheckedChanged += OnPresetChanged;
                rbs[i] = rb;
                block.Controls.Add(rb);
            }

            _rbBalanced = rbs[0]; _rbEco = rbs[1]; _rbFast = rbs[2]; _rbSafe = rbs[3];
            return block;
        }

        // ── Weights block ─────────────────────────────────────────────────────

        private Panel BuildWeightsBlock()
        {
            // 4 rows × 36px each = 144px total
            var block = new Panel { Width = W, Height = 144, BackColor = Color.Transparent };

            var rows = new[] { "Distance", "Trafic", "Pollution", "Sécurité" };
            TrackBar[] tbs = new TrackBar[4];
            Label[] vals = new Label[4];

            for (int i = 0; i < 4; i++)
            {
                int y = i * 36;

                var nameLbl = new Label
                {
                    Text = rows[i],
                    ForeColor = C_MED,
                    Font = F_TINY,
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoSize = false,
                };
                nameLbl.SetBounds(0, y + 2, 68, 30);

                var tb = new TrackBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 25,
                    TickFrequency = 50,
                    TickStyle = TickStyle.None,
                    BackColor = C_SURFACE,
                    AutoSize = false,
                };
                tb.SetBounds(72, y, W - 72 - 44, 36);

                var valLbl = new Label
                {
                    Text = "25%",
                    ForeColor = C_ACCENT,
                    Font = F_TINY,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false,
                };
                valLbl.SetBounds(W - 42, y + 2, 42, 30);

                tb.ValueChanged += OnWeightChanged;
                tbs[i] = tb;
                vals[i] = valLbl;

                block.Controls.AddRange(new Control[] { nameLbl, tb, valLbl });
            }

            _tbDistance = tbs[0]; _tbTraffic = tbs[1]; _tbPollution = tbs[2]; _tbSafety = tbs[3];
            _lblDistVal = vals[0]; _lblTrafVal = vals[1]; _lblPollVal = vals[2]; _lblSafeVal = vals[3];

            SyncTrackBarsFromVm();
            return block;
        }

        // ── Calc button ───────────────────────────────────────────────────────

        private Button BuildCalcBtn()
        {
            _btnCalculate = new Button
            {
                Text = "▶   Calculer l'itinéraire A*",
                Width = W,
                Height = 44,
                BackColor = C_ACCENT,
                ForeColor = C_BG,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10.5f),
                Cursor = Cursors.Hand,
                Enabled = false,
            };
            _btnCalculate.FlatAppearance.BorderSize = 0;
            _btnCalculate.FlatAppearance.MouseOverBackColor = Color.FromArgb(125, 211, 252);
            _btnCalculate.FlatAppearance.MouseDownBackColor = Color.FromArgb(14, 165, 233);
            _btnCalculate.Click += async (_, _) => await OnCalculateAsync();
            return _btnCalculate;
        }

        // ── Stats block ───────────────────────────────────────────────────────

        private Panel BuildStatsBlock()
        {
            // 5 rows × 24px = 120px
            var block = new Panel { Width = W, Height = 120, BackColor = C_SURFACE };
            block.Paint += (_, e) => {
                using var pen = new Pen(C_BORDER, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, block.Width - 1, block.Height - 1);
            };

            string[] keys = { "Distance", "Durée estimée", "Coût total", "Nœuds explorés", "Temps calcul" };
            Label[] valLabels = new Label[5];

            for (int i = 0; i < 5; i++)
            {
                int y = i * 24;

                // Separator line between rows
                if (i > 0)
                {
                    var sep = new Panel { BackColor = C_BORDER, Height = 1 };
                    sep.SetBounds(8, y, W - 16, 1);
                    block.Controls.Add(sep);
                }

                var kLbl = new Label
                {
                    Text = keys[i],
                    ForeColor = C_DIM,
                    Font = F_TINY,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = false,
                };
                kLbl.SetBounds(10, y, 120, 24);

                var vLbl = new Label
                {
                    Text = "—",
                    ForeColor = C_MED,
                    Font = F_MONO,
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoSize = false,
                };
                vLbl.SetBounds(130, y, W - 140, 24);

                valLabels[i] = vLbl;
                block.Controls.AddRange(new Control[] { kLbl, vLbl });
            }

            _lblStatDist = valLabels[0]; _lblStatDur = valLabels[1];
            _lblStatCost = valLabels[2]; _lblStatNodes = valLabels[3];
            _lblStatTime = valLabels[4];
            return block;
        }

        // ─────────────────────────────────────────────────────────────────────
        // BINDING
        // ─────────────────────────────────────────────────────────────────────

        private void BindViewModel()
        {
            _vm.PropertyChanged += (_, e) =>
            {
                if (InvokeRequired) { Invoke(() => BindViewModel()); return; }
                switch (e.PropertyName)
                {
                    case nameof(_vm.StatusMessage):
                        _lblStatus.Text = _vm.StatusMessage;
                        break;

                    case nameof(_vm.IsBusy):
                        _loadingDot.Visible = _vm.IsBusy;
                        _btnLoadOsm.Enabled = !_vm.IsBusy;
                        _btnCancelLoad.Enabled = _vm.IsBusy;
                        _btnCalculate.Enabled = _vm.CanCalculate;
                        _ = _mapView.PostMessageAsync(
                            $"{{\"type\":\"setLoading\",\"show\":{(_vm.IsBusy ? "true" : "false")}}}");
                        break;

                    case nameof(_vm.GraphInfo):
                        _lblGraphInfo.Text = _vm.GraphInfo;
                        _lblGraphInfo.ForeColor = _vm.Graph.NodeCount > 0 ? C_GREEN : C_DIM;
                        break;

                    case nameof(_vm.StartNode):
                        _lblStart.Text = _vm.StartNode is null ? "Départ  : —" : $"Départ  : nœud #{_vm.StartNode.Id}";
                        _lblStart.ForeColor = _vm.StartNode is null ? C_MED : C_GREEN;
                        _btnCalculate.Enabled = _vm.CanCalculate;
                        _ = _mapView.PostMessageAsync(
                            MapBridgeService.BuildMarkersMessage(_vm.StartNode, _vm.GoalNode));
                        break;

                    case nameof(_vm.GoalNode):
                        _lblGoal.Text = _vm.GoalNode is null ? "Arrivée : —" : $"Arrivée : nœud #{_vm.GoalNode.Id}";
                        _lblGoal.ForeColor = _vm.GoalNode is null ? C_MED : C_RED;
                        _btnCalculate.Enabled = _vm.CanCalculate;
                        _ = _mapView.PostMessageAsync(
                            MapBridgeService.BuildMarkersMessage(_vm.StartNode, _vm.GoalNode));
                        break;

                    case nameof(_vm.LastResult):
                        UpdateStats(_vm.LastResult);
                        if (_vm.LastResult?.IsSuccess == true)
                            _ = _mapView.PostMessageAsync(
                                MapBridgeService.BuildDrawRouteMessage(_vm.LastResult));
                        break;

                    case nameof(_vm.Weights):
                        SyncTrackBarsFromVm();
                        break;
                }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // HANDLERS
        // ─────────────────────────────────────────────────────────────────────

        private async Task OnLoadOsmAsync()
        {
            _loadCts = new CancellationTokenSource();
            var bounds = await _mapView.GetBoundsAsync();
            if (bounds is null) { _lblStatus.Text = "Impossible de lire les limites de la carte."; return; }

            var progress = new Progress<string>(msg => _lblStatus.Text = msg);
            await _vm.LoadOsmRoutesAsync(bounds.Value.south, bounds.Value.west,
                                          bounds.Value.north, bounds.Value.east,
                                          progress, _loadCts.Token);

            if (_vm.Graph.NodeCount > 0)
            {
                var roadsMsg = BuildRoadsMessage();
                if (roadsMsg is not null)
                    await _mapView.PostMessageAsync(roadsMsg);
            }
        }

        private async Task OnCalculateAsync() => await _vm.CalculateRouteAsync();

        private void OnMapClicked(double lat, double lon) => _vm.SetWaypoint(lat, lon);

        private void OnPresetChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton { Checked: true } rb)
                _vm.SelectedPreset = rb.Tag?.ToString() ?? "Équilibré";
        }

        private void OnWeightChanged(object? sender, EventArgs e)
        {
            int total = _tbDistance.Value + _tbTraffic.Value + _tbPollution.Value + _tbSafety.Value;
            if (total == 0) return;
            _vm.Weights = new CostWeights
            {
                Distance = _tbDistance.Value / (double)total,
                Traffic = _tbTraffic.Value / (double)total,
                Pollution = _tbPollution.Value / (double)total,
                Safety = _tbSafety.Value / (double)total,
            };
            _lblDistVal.Text = $"{_vm.Weights.Distance:P0}";
            _lblTrafVal.Text = $"{_vm.Weights.Traffic:P0}";
            _lblPollVal.Text = $"{_vm.Weights.Pollution:P0}";
            _lblSafeVal.Text = $"{_vm.Weights.Safety:P0}";
        }

        
        // HELPERS
        
        private void SyncTrackBarsFromVm()
        {
            foreach (var tb in new[] { _tbDistance, _tbTraffic, _tbPollution, _tbSafety })
                tb.ValueChanged -= OnWeightChanged;
            _tbDistance.Value = (int)(_vm.Weights.Distance * 100);
            _tbTraffic.Value = (int)(_vm.Weights.Traffic * 100);
            _tbPollution.Value = (int)(_vm.Weights.Pollution * 100);
            _tbSafety.Value = (int)(_vm.Weights.Safety * 100);
            _lblDistVal.Text = $"{_vm.Weights.Distance:P0}";
            _lblTrafVal.Text = $"{_vm.Weights.Traffic:P0}";
            _lblPollVal.Text = $"{_vm.Weights.Pollution:P0}";
            _lblSafeVal.Text = $"{_vm.Weights.Safety:P0}";
            foreach (var tb in new[] { _tbDistance, _tbTraffic, _tbPollution, _tbSafety })
                tb.ValueChanged += OnWeightChanged;
        }

        private void UpdateStats(RouteResult? r)
        {
            bool ok = r is not null && r.IsSuccess;
            _lblStatDist.Text = ok ? $"{r!.TotalDistanceKm:F2} km" : "—";
            _lblStatDur.Text = ok ? $"{r!.EstimatedDurationMin:F0} min" : "—";
            _lblStatCost.Text = ok ? $"{r!.TotalCost:F4}" : "—";
            _lblStatNodes.Text = ok ? $"{r!.NodesExplored}" : "—";
            _lblStatTime.Text = ok ? $"{r!.ComputationTimeMs} ms" : "—";
            var col = ok ? C_ACCENT : C_MED;
            foreach (var l in new[] { _lblStatDist, _lblStatDur, _lblStatCost, _lblStatNodes, _lblStatTime })
                l.ForeColor = col;
        }

        private string? BuildRoadsMessage()
        {
            var nodeMap = _vm.Graph.AllNodes.ToDictionary(n => n.Id);
            var segments = new List<(RoadNode, RoadNode, string)>();
            foreach (var node in _vm.Graph.AllNodes)
                foreach (var edge in _vm.Graph.GetNeighbors(node.Id))
                    if (nodeMap.TryGetValue(edge.ToNodeId, out var to))
                        segments.Add((node, to, edge.HighwayType));
            return segments.Count > 0 ? MapBridgeService.BuildDrawRoadsMessage(segments) : null;
        }

        private Button PrimaryBtn(string text, Color bg, Color fg)
        {
            var b = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = F_BTN,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bg, 0.15f);
            b.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(bg, 0.1f);
            return b;
        }

        private Button GhostBtn(string text)
        {
            var b = new Button
            {
                Text = text,
                BackColor = C_SURFACE,
                ForeColor = C_MED,
                FlatStyle = FlatStyle.Flat,
                Font = F_SMALL,
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderColor = C_BORDER;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 52, 84);
            return b;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _loadCts?.Dispose();
            _vm.Dispose();
            base.OnFormClosed(e);
        }
    }
}