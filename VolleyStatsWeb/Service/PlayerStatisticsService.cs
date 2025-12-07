using System.Globalization;
using VolleyStatsWeb.Data;
using VolleyStatsWeb.DTO;
using VolleyStatsWeb.Enums;
using System.Text;
using System.Text.Json;
using System.Net;


namespace VolleyStatsWeb.Service
{
    public class PlayerStatisticsService : IPlayerStatisticsService
    {
        private readonly IPlayerRepository _repository;

        public PlayerStatisticsService(IPlayerRepository repository)
        {
            _repository = repository;
        }

        public PlayerProfileDto? GetPlayerProfile(int id)
        {
            if (id <= 0)
                return null;

            var basic = _repository.GetPlayerBasicInfo(id);
            if (basic == null)
                return null;

            var dto = new PlayerProfileDto
            {
                Id = basic.Id,
                FullName = $"{basic.FirstName} {basic.LastName}".Trim(),
                Age = CalculateAge(basic.BirthDate),
                HeightCm = basic.HeightCm,
                Position = MapPosition(basic.PositionCode),
                TeamName = basic.TeamName,
                JerseyNumber = basic.JerseyNumber
            };

            dto.SeasonName = _repository.GetLatestSeasonNameForPlayer(id) ?? "";

            var lastMatches = _repository
                .GetLastMatchPoints(id, 10)
                .OrderBy(m => m.DateUtc)
                .ToList();

            dto.Last10MatchPointsJson = JsonSerializer.Serialize(lastMatches.Select(m => m.Points));
            dto.Last10MatchLabelsJson = JsonSerializer.Serialize(lastMatches.Select(m => m.DateUtc.ToString("dd.MM.")));

            var seasons = _repository.GetSeasonsForPlayer(id).ToList();

            if (seasons.Count == 0 && !string.IsNullOrWhiteSpace(dto.SeasonName))
            {
                seasons.Add(dto.SeasonName);
            }

            var seasonOptionsSb = new StringBuilder();
            foreach (var season in seasons)
            {
                var selected = season == dto.SeasonName ? " selected" : "";
                var encoded = WebUtility.HtmlEncode(season);
                seasonOptionsSb.Append($"<option value=\"{encoded}\"{selected}>{encoded}</option>");
            }

            dto.SeasonOptionsHtml = seasonOptionsSb.ToString();

            if (!string.IsNullOrWhiteSpace(dto.SeasonName))
            {
                var history = _repository.GetMatchHistoryForPlayerInSeason(id, dto.SeasonName);
                var rowsSb = new StringBuilder();

                foreach (var h in history)
                {
                    var dateText = h.MatchDateUtc.ToString("dd.MM.yyyy");
                    var opponent = h.IsHome ? h.AwayTeamName : h.HomeTeamName;

                    var setsFor = h.IsHome ? h.HomeSetsWon : h.AwaySetsWon;
                    var setsAgainst = h.IsHome ? h.AwaySetsWon : h.HomeSetsWon;
                    var setsTotal = h.HomeSetsWon + h.AwaySetsWon;

                    var resultPrefix = setsFor > setsAgainst ? "Výhra" : "Prohra";
                    var scoreText = $"{setsFor}:{setsAgainst}";
                    var resultText = $"{resultPrefix} {scoreText}";

                    var attackPercent = h.AttackAttempts > 0
                        ? (int)Math.Round(100.0 * h.AttackPoints / (double)h.AttackAttempts)
                        : 0;

                    rowsSb.AppendLine("<tr>");
                    rowsSb.AppendLine($"  <td>{dateText}</td>");
                    rowsSb.AppendLine($"  <td>{WebUtility.HtmlEncode(opponent)}</td>");
                    rowsSb.AppendLine($"  <td>{setsTotal}</td>");
                    rowsSb.AppendLine($"  <td>{h.Points}</td>");
                    rowsSb.AppendLine($"  <td>{attackPercent}%</td>");
                    rowsSb.AppendLine($"  <td>{h.Blocks}</td>");
                    rowsSb.AppendLine($"  <td>{h.Aces}</td>");
                    rowsSb.AppendLine("</tr>");
                }

                dto.MatchHistoryRowsHtml = rowsSb.ToString();
            }
            else
            {
                dto.MatchHistoryRowsHtml =
                    "<tr><td colspan=\"8\">Pro hráčku nebyla nalezena žádná sezóna.</td></tr>";
            }

            var matchesPlayed = _repository.GetMatchesPlayed(id);
            var setsPlayed = _repository.GetSetsPlayed(id);
            var events = _repository.GetAllEventsForPlayer(id);

            int totalPoints = 0;
            int totalAttacks = 0;
            int successfulAttacks = 0;
            int aces = 0;

            foreach (var (skillStr, evalStr) in events)
            {
                var skill = ParseSkill(skillStr);
                var eval = ParseEval(evalStr);

                if (IsPoint(skill, eval))
                    totalPoints++;

                if (skill == BasicSkill.Attack)
                {
                    totalAttacks++;
                    if (eval == EvaluationSymbol.Point)
                        successfulAttacks++;
                }

                if (skill == BasicSkill.Serve && eval == EvaluationSymbol.Point)
                    aces++;
            }

            dto.PointsPerMatch = matchesPlayed > 0
                ? (double)totalPoints / matchesPlayed
                : 0.0;

            dto.AttackSuccessPercent = totalAttacks > 0
                ? 100.0 * successfulAttacks / totalAttacks
                : 0.0;

            dto.AcesPerSet = setsPlayed > 0
                ? (double)aces / setsPlayed
                : 0.0;

            return dto;
        }


        private static int CalculateAge(string? birthDate)
        {
            if (string.IsNullOrWhiteSpace(birthDate))
                return 0;

            if (!DateTime.TryParseExact(
                    birthDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
            {
                return 0;
            }

            var today = DateTime.Today;
            var age = today.Year - dt.Year;
            if (dt.Date > today.AddYears(-age)) age--;
            return age;
        }

        private static string MapPosition(int? code)
        {
            if (code is null)
                return "";

            return ((PlayerPost)code) switch
            {
                PlayerPost.Libero => "Libero",
                PlayerPost.OutsideHitter => "Outside hitter",
                PlayerPost.Opposite => "Opposite",
                PlayerPost.MiddleBlocker => "Middle blocker",
                PlayerPost.Setter => "Setter",
                _ => ""
            };
        }

        private static BasicSkill ParseSkill(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return BasicSkill.Unknown;

            s = s.Trim();

            return s switch
            {
                "Attack" => BasicSkill.Attack,

                "Block" => BasicSkill.Block,

                "Serve" => BasicSkill.Serve,

                "Reception" => BasicSkill.Reception,

                "Dig" => BasicSkill.Dig,

                _ => BasicSkill.Unknown
            };
        }

        private static EvaluationSymbol ParseEval(string eval)
        {
            if (string.IsNullOrWhiteSpace(eval))
                return EvaluationSymbol.Unknown;

            return eval switch
            {
                "Point" => EvaluationSymbol.Point,
                "Positive" => EvaluationSymbol.Positive,
                "Good" => EvaluationSymbol.Good,
                "Poor" => EvaluationSymbol.Poor,
                "Error" => EvaluationSymbol.Error,
                "Over" => EvaluationSymbol.Over,
                _ => EvaluationSymbol.Unknown
            };
        }

        private static bool IsPoint(BasicSkill skill, EvaluationSymbol eval)
        {
            return skill switch
            {
                BasicSkill.Attack => eval == EvaluationSymbol.Point,
                BasicSkill.Block => eval == EvaluationSymbol.Point,
                BasicSkill.Serve => eval == EvaluationSymbol.Point,
                _ => false
            };
        }

    }

}
