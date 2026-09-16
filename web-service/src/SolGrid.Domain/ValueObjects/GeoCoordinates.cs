/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: GeoCoordinates.cs
 * Description: Represents a validated GPS position for a solar station.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Domain.ValueObjects;

public sealed record GeoCoordinates
{
    public const double MinimumLatitude = -90d;
    public const double MaximumLatitude = 90d;
    public const double MinimumLongitude = -180d;
    public const double MaximumLongitude = 180d;

    private const double EarthRadiusKilometers = 6371.0088d;

    private GeoCoordinates(double latitude, double longitude)
    {
        // Store an already validated GPS position.
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static GeoCoordinates Create(double latitude, double longitude)
    {
        // Reject coordinates that cannot describe a real position on earth.
        if (!IsValidLatitude(latitude))
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                latitude,
                "Latitude must be between -90 and 90 degrees.");
        }

        if (!IsValidLongitude(longitude))
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                longitude,
                "Longitude must be between -180 and 180 degrees.");
        }

        return new GeoCoordinates(latitude, longitude);
    }

    public static bool IsValidLatitude(double latitude)
    {
        // Guard against NaN and infinities because range comparisons are not a complete check.
        return !double.IsNaN(latitude)
            && !double.IsInfinity(latitude)
            && latitude >= MinimumLatitude
            && latitude <= MaximumLatitude;
    }

    public static bool IsValidLongitude(double longitude)
    {
        // Guard against NaN and infinities because range comparisons are not a complete check.
        return !double.IsNaN(longitude)
            && !double.IsInfinity(longitude)
            && longitude >= MinimumLongitude
            && longitude <= MaximumLongitude;
    }

    public double DistanceInKilometersTo(GeoCoordinates other)
    {
        // Approximate great-circle distance so callers can rank nearby stations.
        ArgumentNullException.ThrowIfNull(other);

        var latitudeDelta = ToRadians(other.Latitude - Latitude);
        var longitudeDelta = ToRadians(other.Longitude - Longitude);
        var startLatitude = ToRadians(Latitude);
        var endLatitude = ToRadians(other.Latitude);

        var haversine = (Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2))
            + (Math.Cos(startLatitude) * Math.Cos(endLatitude)
                * Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2));

        var centralAngle = 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));

        return EarthRadiusKilometers * centralAngle;
    }

    private static double ToRadians(double degrees)
    {
        // Convert degrees to radians for trigonometric distance maths.
        return degrees * Math.PI / 180d;
    }
}
