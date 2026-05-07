using PigeonRacing.Application.Common.Interfaces;

namespace PigeonRacing.Infrastructure.Services;

public class VelocityCalculator : IVelocityCalculator
{
    private const double EarthRadiusKm = 6371.0;

    public double Calculate(double distanceKm, TimeSpan flightDuration)
    {
        if (flightDuration.TotalMinutes <= 0)
            throw new ArgumentException("Flight duration must be positive.");

        var distanceMeters = distanceKm * 1000.0;
        return distanceMeters / flightDuration.TotalMinutes;
    }

    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
