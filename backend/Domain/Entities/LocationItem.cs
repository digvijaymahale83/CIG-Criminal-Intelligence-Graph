namespace Domain.Entities;

/// <summary>
/// Represents a canonical physical or geographic location record within an investigation.
/// </summary>
public class LocationItem
{
    public string Id { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string? EntityId { get; set; } // Optional link to EntityItem of Type = "LOCATION"
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "India";
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// Precision indicator: EXACT, BUILDING, STREET, AREA, CITY, DISTRICT, STATE, COUNTRY, APPROXIMATE, UNKNOWN
    /// </summary>
    public string GeocodePrecision { get; set; } = "CITY";

    /// <summary>
    /// Origin of geographic coordinates: SOURCE_DATA, SYNTHETIC_DEMO, GEOCODING_SERVICE, MANUAL_ENTRY
    /// </summary>
    public string Source { get; set; } = "SOURCE_DATA";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Case? Case { get; set; }
    public EntityItem? Entity { get; set; }

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
}
