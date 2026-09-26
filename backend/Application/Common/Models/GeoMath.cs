namespace Application.Common.Models;

/// <summary>
/// Provides pure mathematical geospatial calculations using the exact Haversine formula.
/// </summary>
public static class GeoMath
{
    public const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Computes the great-circle distance between two geographic coordinates in kilometers
    /// using the Haversine formula.
    /// </summary>
    public static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        if (Math.Abs(lat1 - lat2) < 1e-9 && Math.Abs(lon1 - lon2) < 1e-9)
        {
            return 0.0;
        }

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var rLat1 = ToRadians(lat1);
        var rLat2 = ToRadians(lat2);

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Calculates implied average travel velocity in km/h.
    /// </summary>
    public static double CalculateSpeedKmh(double distanceKm, double durationHours)
    {
        if (durationHours <= 0.0001) return 0.0;
        return distanceKm / durationHours;
    }

    /// <summary>
    /// Checks if a transition between two events is physically implausible
    /// (e.g., implied speed > 850 km/h for domestic travel or > 1200 km/h overall).
    /// </summary>
    public static bool IsImplausibleVelocity(double distanceKm, double durationHours, out double impliedSpeedKmh)
    {
        impliedSpeedKmh = CalculateSpeedKmh(distanceKm, durationHours);
        // If distance is meaningful (> 100km) and implied speed exceeds commercial aircraft cruise (> 900 km/h)
        return distanceKm > 100.0 && durationHours > 0.0 && impliedSpeedKmh > 900.0;
    }

    public static bool IsImplausibleVelocity(double distanceKm, double durationHours, double thresholdKmh = 900.0)
    {
        var speed = CalculateSpeedKmh(distanceKm, durationHours);
        return distanceKm > 100.0 && durationHours > 0.0 && speed > thresholdKmh;
    }

    /// <summary>
    /// Validates latitude [-90, 90] and longitude [-180, 180].
    /// </summary>
    public static bool IsValidCoordinate(double latitude, double longitude)
    {
        return !double.IsNaN(latitude) &&
               !double.IsInfinity(latitude) &&
               !double.IsNaN(longitude) &&
               !double.IsInfinity(longitude) &&
               latitude >= -90.0 && latitude <= 90.0 &&
               longitude >= -180.0 && longitude <= 180.0;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
}
