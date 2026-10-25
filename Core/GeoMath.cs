namespace EcoRouteAI.Core
{
    /// <summary>
    /// Fournit des utilitaires de calcul géospatial.
    /// </summary>
    /// <remarks>
    /// Algorithme : Formule de Haversine (Calcul de la distance du grand cercle).
    /// Rôle dans A* : Garantit que l'heuristique h(n) est admissible, car la distance 
    /// à vol d'oiseau est toujours inférieure ou égale à la distance routière réelle.
    /// 
    /// Complexité Spatiale : O(1) - Aucun stockage de données requis.
    /// </remarks>
    public static class GeoMath
    {
        /// <summary> Rayon moyen de la Terre utilisé pour les calculs (sphère). </summary>
        private const double EarthRadiusKm = 6371.0;

        /// <summary>
        /// Calcule la distance orthodromique entre deux coordonnées GPS.
        /// </summary>
        /// <param name="lat1">Latitude du point de départ en degrés.</param>
        /// <param name="lon1">Longitude du point de départ en degrés.</param>
        /// <param name="lat2">Latitude de destination en degrés.</param>
        /// <param name="lon2">Longitude de destination en degrés.</param>
        /// <returns>La distance en kilomètres entre les deux points.</returns>
        /// <pre>Coordonnées valides (Latitude entre -90 et 90, Longitude entre -180 et 180).</pre>
        /// <post>Retourne une valeur positive ou nulle (0 si les points sont identiques).</post>
        /// <algorithm>
        /// 1. Convertir les différences de coordonnées en radians (Δlat, Δlon).
        /// 2. Appliquer la formule de Haversine :
        ///    a = sin²(Δlat/2) + cos(lat1) * cos(lat2) * sin²(Δlon/2)
        /// 3. Calculer l'angle central c via 2 * atan2(√a, √(1-a)).
        /// 4. Résultat = Rayon de la Terre * c.
        /// </algorithm>
        /// <pseudo-code>
        /// FONCTION HaversineKm(lat1, lon1, lat2, lon2)
        ///    dLat = (lat2 - lat1) * PI / 180
        ///    dLon = (lon2 - lon1) * PI / 180
        ///    a = SIN(dLat/2)^2 + COS(lat1_rad) * COS(lat2_rad) * SIN(dLon/2)^2
        ///    c = 2 * ATAN2(SQRT(a), SQRT(1-a))
        ///    RETOURNER RayonTerre * c
        /// FIN
        /// </pseudo-code>
        /// <complexity>
        /// Temporelle : O(1) - Opérations mathématiques à temps constant.
        /// </complexity>
        public static double HaversineKm(double lat1, double lon1,
                                          double lat2, double lon2)
        {
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        /// <summary>
        /// Convertit un angle de degrés vers radians.
        /// </summary>
        /// <param name="degrees">L'angle en degrés.</param>
        /// <returns>L'angle converti en radians.</returns>
        /// <complexity>
        /// Temporelle : O(1)
        /// </complexity>
        private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
    }
}