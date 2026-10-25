using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EcoRouteAI.Views
{
    public sealed class WebView2Wrapper : Panel
    {
        private WebView2 _webView = null!;
        private bool _isReady;
        private readonly ConcurrentQueue<string> _pendingMessages = new();

        public event Action<double, double>? MapClicked;

        public WebView2Wrapper()
        {
            BackColor = Color.FromArgb(10, 14, 26);
            HandleCreated += async (_, _) => await InitAsync();
        }

        private async Task InitAsync()
        {
            _webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_webView);
            await _webView.EnsureCoreWebView2Async();
            _webView.CoreWebView2.Settings.UserAgent = "EcoRouteAI/2.0";
            _webView.CoreWebView2.WebMessageReceived += OnWebMessage;
            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _webView.CoreWebView2.NavigateToString(BuildLeafletHtml());
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;
            _isReady = true;
            while (_pendingMessages.TryDequeue(out var json))
                await ExecuteHandlerAsync(json);
        }

        public async Task PostMessageAsync(string json)
        {
            if (!_isReady) { _pendingMessages.Enqueue(json); return; }
            await ExecuteHandlerAsync(json);
        }

        private async Task ExecuteHandlerAsync(string json)
        {
            if (_webView?.CoreWebView2 is null) return;
            try
            {
                string escaped = json.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$");
                await _webView.ExecuteScriptAsync($"handleCSharpMessage(JSON.parse(`{escaped}`))");
            }
            catch { }
        }

        public async Task<(double south, double west, double north, double east)?> GetBoundsAsync()
        {
            try
            {
                // On demande les limites à Leaflet
                string script = @"
      (function() {
          if (typeof map === 'undefined') return null;
          var b = map.getBounds();
          return JSON.stringify({
              s: b.getSouth(),
              w: b.getWest(),
              n: b.getNorth(),
              e: b.getEast()
          });
      })()";

                var result = await _webView.ExecuteScriptAsync(script);

                // WebView2 renvoie parfois une chaîne JSON entourée de guillemets supplémentaires ou "null"
                if (string.IsNullOrEmpty(result) || result == "null") return null;

                // Nettoyage des guillemets doubles si nécessaire (propre à WebView2)
                if (result.StartsWith("\"") && result.EndsWith("\""))
                {
                    result = System.Text.RegularExpressions.Regex.Unescape(result.Substring(1, result.Length - 2));
                }

                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                return (
                    root.GetProperty("s").GetDouble(),
                    root.GetProperty("w").GetDouble(),
                    root.GetProperty("n").GetDouble(),
                    root.GetProperty("e").GetDouble()
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur GetBounds : {ex.Message}");
                return null;
            }
        }

        private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.WebMessageAsJson);
                var root = doc.RootElement;
                JsonElement payload = root.ValueKind == JsonValueKind.String
                    ? JsonDocument.Parse(root.GetString()!).RootElement : root;
                string type = payload.GetProperty("type").GetString() ?? "";
                if (type == "mapClick")
                {
                    double lat = payload.GetProperty("lat").GetDouble();
                    double lon = payload.GetProperty("lon").GetDouble();
                    if (InvokeRequired) Invoke(() => MapClicked?.Invoke(lat, lon));
                    else MapClicked?.Invoke(lat, lon);
                }
            }
            catch { }
        }

        private static string BuildLeafletHtml() => """
<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="UTF-8"/>
<meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>EcoRoute AI</title>
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }

  html, body {
    height: 100%;
    background: #0a0e1a;
    font-family: 'Segoe UI', system-ui, sans-serif;
    overflow: hidden;
  }

  #map {
    height: 100%;
    width: 100%;
    background: #0a0e1a;
  }

  /* Leaflet overrides — dark premium theme */
  .leaflet-tile-pane { filter: saturate(0.7) brightness(0.85) contrast(1.1); }
  .leaflet-control-zoom {
    border: 1px solid rgba(100,180,255,0.2) !important;
    border-radius: 10px !important;
    overflow: hidden;
    box-shadow: 0 4px 24px rgba(0,0,0,0.5) !important;
    backdrop-filter: blur(10px);
  }
  .leaflet-control-zoom a {
    background: rgba(10,14,26,0.85) !important;
    color: #64b4ff !important;
    border-bottom: 1px solid rgba(100,180,255,0.15) !important;
    font-size: 16px !important;
    line-height: 30px !important;
    width: 30px !important;
    height: 30px !important;
    transition: background 0.2s, color 0.2s;
  }
  .leaflet-control-zoom a:hover {
    background: rgba(100,180,255,0.15) !important;
    color: #fff !important;
  }
  .leaflet-control-attribution {
    background: rgba(10,14,26,0.7) !important;
    color: rgba(255,255,255,0.3) !important;
    font-size: 10px !important;
    backdrop-filter: blur(6px);
    border-radius: 6px 0 0 0 !important;
    padding: 2px 8px !important;
  }
  .leaflet-control-attribution a { color: rgba(100,180,255,0.5) !important; }

  /* Toast notification */
  #toast {
    position: fixed;
    bottom: 24px;
    left: 50%;
    transform: translateX(-50%) translateY(20px);
    background: rgba(10,14,26,0.92);
    border: 1px solid rgba(100,180,255,0.25);
    border-radius: 50px;
    padding: 10px 22px;
    color: #a0c4ff;
    font-size: 12px;
    font-weight: 500;
    letter-spacing: 0.03em;
    pointer-events: none;
    opacity: 0;
    transition: opacity 0.3s ease, transform 0.3s ease;
    backdrop-filter: blur(14px);
    box-shadow: 0 8px 32px rgba(0,0,0,0.4);
    white-space: nowrap;
    z-index: 9999;
  }
  #toast.show {
    opacity: 1;
    transform: translateX(-50%) translateY(0);
  }

  /* Pulse animation for markers */
  @keyframes pulse-green {
    0%   { box-shadow: 0 0 0 0 rgba(34,197,94,0.7), 0 0 8px rgba(34,197,94,0.5); }
    70%  { box-shadow: 0 0 0 12px rgba(34,197,94,0), 0 0 8px rgba(34,197,94,0.5); }
    100% { box-shadow: 0 0 0 0 rgba(34,197,94,0), 0 0 8px rgba(34,197,94,0.5); }
  }
  @keyframes pulse-red {
    0%   { box-shadow: 0 0 0 0 rgba(239,68,68,0.7), 0 0 8px rgba(239,68,68,0.5); }
    70%  { box-shadow: 0 0 0 12px rgba(239,68,68,0), 0 0 8px rgba(239,68,68,0.5); }
    100% { box-shadow: 0 0 0 0 rgba(239,68,68,0), 0 0 8px rgba(239,68,68,0.5); }
  }
  .marker-start {
    width: 16px; height: 16px;
    border-radius: 50%;
    background: #22c55e;
    border: 2.5px solid rgba(255,255,255,0.9);
    animation: pulse-green 2s infinite;
  }
  .marker-goal {
    width: 16px; height: 16px;
    border-radius: 50%;
    background: #ef4444;
    border: 2.5px solid rgba(255,255,255,0.9);
    animation: pulse-red 2s infinite;
  }

  /* Route animation */
  @keyframes dash {
    to { stroke-dashoffset: -30; }
  }

  /* Loading spinner overlay */
  #loading {
    position: fixed;
    inset: 0;
    background: rgba(10,14,26,0.6);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 8000;
    backdrop-filter: blur(4px);
    opacity: 0;
    pointer-events: none;
    transition: opacity 0.3s;
  }
  #loading.show { opacity: 1; pointer-events: all; }
  .spinner {
    width: 40px; height: 40px;
    border: 3px solid rgba(100,180,255,0.15);
    border-top: 3px solid #64b4ff;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }
  @keyframes spin { to { transform: rotate(360deg); } }

  /* Cursor crosshair when placing points */
  #map.placing { cursor: crosshair !important; }
</style>
</head>
<body>
<div id="map"></div>
<div id="toast"></div>
<div id="loading"><div class="spinner"></div></div>

<script>
// ── Map init ──────────────────────────────────────────────────────────────────
window.map = L.map('map', {
  zoomControl: true,
  zoomAnimation: true,
  fadeAnimation: true,
  markerZoomAnimation: true,
  preferCanvas: true        // Performance: canvas renderer for roads
}).setView([45.5017, -73.5673], 14);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  attribution: '© OpenStreetMap',
  maxZoom: 19,
  className: 'map-tiles',
}).addTo(map);

// ── Renderer canvas (fast for thousands of polylines) ─────────────────────────
const canvasRenderer = L.canvas({ padding: 0.5, tolerance: 5 });

// ── State ─────────────────────────────────────────────────────────────────────
let routeLayerGroup = null;
let roadsLayerGroup = L.layerGroup().addTo(map);
let startMarker = null;
let goalMarker  = null;
let isPlacing   = false;

// ── Icons ─────────────────────────────────────────────────────────────────────
const startIcon = L.divIcon({
  html: '<div class="marker-start"></div>',
  iconSize: [16, 16], iconAnchor: [8, 8], className: '',
});
const goalIcon = L.divIcon({
  html: '<div class="marker-goal"></div>',
  iconSize: [16, 16], iconAnchor: [8, 8], className: '',
});

// ── Toast helper ──────────────────────────────────────────────────────────────
let toastTimer = null;
function showToast(msg, duration = 2500) {
  const el = document.getElementById('toast');
  el.textContent = msg;
  el.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove('show'), duration);
}

// ── Click handler ─────────────────────────────────────────────────────────────
map.on('click', function(e) {
  window.chrome.webview.postMessage(JSON.stringify({
    type: 'mapClick',
    lat: e.latlng.lat,
    lon: e.latlng.lng
  }));
});

// ── C# message handler (called via ExecuteScriptAsync) ────────────────────────
function handleCSharpMessage(msg) {
  if (!msg || !msg.type) return;

  switch (msg.type) {

    case 'drawRoads': {
      roadsLayerGroup.clearLayers();
      const roads = msg.roads || [];
      // Batch by highway type for efficiency
      const byType = {};
      roads.forEach(r => {
        const k = r.highway || 'default';
        if (!byType[k]) byType[k] = [];
        byType[k].push(r);
      });
      Object.entries(byType).forEach(([type, segs]) => {
        segs.forEach(r => {
          // Shadow stroke
          L.polyline([r.from, r.to], {
            color: 'rgba(0,0,0,0.35)',
            weight: getRoadWeight(type) + 1.5,
            opacity: 1,
            renderer: canvasRenderer,
            interactive: false,
          }).addTo(roadsLayerGroup);
          // Color stroke
          L.polyline([r.from, r.to], {
            color: r.color,
            weight: getRoadWeight(type),
            opacity: getOpacity(type),
            renderer: canvasRenderer,
            interactive: false,
          }).addTo(roadsLayerGroup);
        });
      });
      showToast('Réseau routier chargé ✓');
      break;
    }

    case 'drawRoute': {
      if (routeLayerGroup) {
        map.removeLayer(routeLayerGroup);
        routeLayerGroup = null;
      }
      if (!msg.coordinates || msg.coordinates.length === 0) break;

      const coords = msg.coordinates;

      // Outer glow
      const glow = L.polyline(coords, {
        color: 'rgba(56,189,248,0.15)',
        weight: 18,
        opacity: 1,
        lineCap: 'round',
        lineJoin: 'round',
      });
      // Shadow
      const shadow = L.polyline(coords, {
        color: 'rgba(0,0,0,0.4)',
        weight: 9,
        opacity: 1,
        lineCap: 'round',
        lineJoin: 'round',
      });
      // Main route
      const route = L.polyline(coords, {
        color: '#38bdf8',
        weight: 5,
        opacity: 1,
        lineCap: 'round',
        lineJoin: 'round',
      });
      // Animated dashes overlay
      const dashes = L.polyline(coords, {
        color: 'rgba(255,255,255,0.6)',
        weight: 2,
        opacity: 1,
        dashArray: '8, 16',
        lineCap: 'round',
        className: 'route-dashes',
      });

      routeLayerGroup = L.layerGroup([glow, shadow, route, dashes]).addTo(map);

      // Fit with smooth animation
      try {
        const bounds = L.latLngBounds(coords);
        map.flyToBounds(bounds, { padding: [60, 60], duration: 1.2, easeLinearity: 0.25 });
      } catch(ex) {}

      if (msg.stats) {
        const d = (msg.stats.distanceKm || 0).toFixed(2);
        const t = Math.round(msg.stats.durationMin || 0);
        showToast(`${d} km · ${t} min`, 4000);
      }
      break;
    }

    case 'setMarkers': {
      if (startMarker) { map.removeLayer(startMarker); startMarker = null; }
      if (goalMarker)  { map.removeLayer(goalMarker);  goalMarker  = null; }

      if (msg.start) {
        startMarker = L.marker(msg.start, { icon: startIcon, zIndexOffset: 1000 }).addTo(map);
        showToast('Point de départ défini', 1800);
      }
      if (msg.goal) {
        goalMarker = L.marker(msg.goal, { icon: goalIcon, zIndexOffset: 1000 }).addTo(map);
        showToast('Point d\'arrivée défini', 1800);
      }
      break;
    }

    case 'clear': {
      if (routeLayerGroup) { map.removeLayer(routeLayerGroup); routeLayerGroup = null; }
      if (startMarker)     { map.removeLayer(startMarker);     startMarker = null; }
      if (goalMarker)      { map.removeLayer(goalMarker);       goalMarker  = null; }
      roadsLayerGroup.clearLayers();
      showToast('Carte réinitialisée');
      break;
    }

    case 'setLoading': {
      const el = document.getElementById('loading');
      if (msg.show) el.classList.add('show');
      else          el.classList.remove('show');
      break;
    }

    case 'setCursor': {
      const el = document.getElementById('map');
      if (msg.crosshair) el.classList.add('placing');
      else               el.classList.remove('placing');
      break;
    }
  }
}

// ── Road styling helpers ──────────────────────────────────────────────────────
function getRoadWeight(type) {
  const w = { motorway:3.5, trunk:3, primary:2.5, secondary:2, tertiary:1.5,
              residential:1.2, living_street:1, service:1 };
  return w[type] || 1.2;
}
function getOpacity(type) {
  const o = { motorway:0.95, trunk:0.9, primary:0.85, secondary:0.8,
              tertiary:0.75, residential:0.65, living_street:0.6, service:0.55 };
  return o[type] || 0.65;
}

// ── Bbox API ──────────────────────────────────────────────────────────────────
window.getMapBounds = function() {
  var b = map.getBounds();
  return JSON.stringify({
    south: b.getSouth(), west: b.getWest(),
    north: b.getNorth(), east: b.getEast()
  });
};
</script>
</body>
</html>
""";
    }

    public sealed class MapBounds
    {
        public double South { get; set; }
        public double West { get; set; }
        public double North { get; set; }
        public double East { get; set; }
    }
}
