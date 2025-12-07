namespace VolleyStatsWeb.Data
{
    public interface IPlayerRepository
    {
        PlayerBasicInfo? GetPlayerBasicInfo(int playerId);
        string? GetLatestSeasonNameForPlayer(int playerId);
        int GetMatchesPlayed(int playerId);
        int GetSetsPlayed(int playerId);
        List<(string Skill, string Eval)> GetAllEventsForPlayer(int playerId);
        IReadOnlyList<PlayerMatchPoints> GetLastMatchPoints(int playerId, int limit);
        IReadOnlyList<string> GetSeasonsForPlayer(int playerId);

        IReadOnlyList<PlayerMatchHistoryRow> GetMatchHistoryForPlayerInSeason(
            int playerId,
            string seasonName);
    }
}
