namespace PRC.RaceService.Services;

public interface ISpeedCalculator
{
    double Calculate(double distanceKm, TimeSpan flightDuration);
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
}
