using VolleyStatsWeb.DTO;

namespace VolleyStatsWeb.Html
{
    public static class HtmlRenderer
    {
        public static string RenderPlayerPage(string templatePath, PlayerProfileDto vm)
        {
            var template = File.ReadAllText(templatePath);

            return template
                .Replace("{{PLAYER_NAME}}", vm.FullName)
                .Replace("{{JERSEY_NUMBER}}", vm.JerseyNumber.ToString())
                .Replace("{{TEAM_NAME}}", vm.TeamName)
                .Replace("{{POSITION}}", vm.Position)
                .Replace("{{AGE}}", vm.Age.ToString())
                .Replace("{{HEIGHT_CM}}", vm.HeightCm?.ToString() ?? "-")
                .Replace("{{SEASON_NAME}}", vm.SeasonName)
                .Replace("{{POINTS_PER_MATCH}}", vm.PointsPerMatch.ToString("0.0"))
                .Replace("{{ATTACK_SUCCESS_PERCENT}}", vm.AttackSuccessPercent.ToString("0"))
                .Replace("{{ACES_PER_SET}}", vm.AcesPerSet.ToString("0.0"))
                .Replace("{{LAST10_POINTS}}", vm.Last10MatchPointsJson)
                .Replace("{{LAST10_LABELS}}", vm.Last10MatchLabelsJson)
                .Replace("{{SEASON_OPTIONS}}", vm.SeasonOptionsHtml)
                .Replace("{{MATCH_HISTORY_ROWS}}", vm.MatchHistoryRowsHtml);
        }
    }
}
