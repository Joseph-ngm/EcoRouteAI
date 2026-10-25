using EcoRouteAI.Models;

namespace EcoRouteAI.Services
{
    /// <summary>
    /// Contrat du service de chargement du réseau routier OSM.
    /// Séparation interface/implémentation pour testabilité (BNF15).
    /// </summary>
    public interface IOsmService
    {
        /// <summary>
        /// Télécharge et parse les données OSM via Overpass API pour la bbox fournie.
        /// </summary>
        /// <param name="south">Latitude sud.</param>
        /// <param name="west">Longitude ouest.</param>
        /// <param name="north">Latitude nord.</param>
        /// <param name="east">Longitude est.</param>
        /// <param name="progress">Canal de progression (BNF12).</param>
        /// <param name="ct">Token d'annulation.</param>
        Task<OsmData> FetchRoadsAsync(double south, double west,
                                      double north, double east,
                                      IProgress<string>? progress = null,
                                      CancellationToken ct = default);
    }

    
}