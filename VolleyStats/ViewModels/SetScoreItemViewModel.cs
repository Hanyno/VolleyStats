namespace VolleyStats.ViewModels
{
    public class SetScoreItemViewModel
    {
        public int SetNumber { get; }
        public int HomeScore { get; }
        public int AwayScore { get; }
        public string ScoreText => $"{HomeScore}–{AwayScore}";

        public SetScoreItemViewModel(int setNumber, int homeScore, int awayScore)
        {
            SetNumber = setNumber;
            HomeScore = homeScore;
            AwayScore = awayScore;
        }
    }
}
