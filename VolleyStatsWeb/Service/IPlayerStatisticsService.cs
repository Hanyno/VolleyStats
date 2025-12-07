using VolleyStatsWeb.DTO;

namespace VolleyStatsWeb.Service
{
    public interface IPlayerStatisticsService
    {
        PlayerProfileDto? GetPlayerProfile(int id);
    }
}
