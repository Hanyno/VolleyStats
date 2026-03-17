using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace VolleyStats.ViewModels
{
    public class AnalysisViewModel : ViewModelBase
    {
        public string AnalysisTeam { get; }
        public int MatchCount { get; }

        public IRelayCommand BackCommand { get; }
        public IAsyncRelayCommand OpenVideoAnalysisCommand { get; }
        public IAsyncRelayCommand OpenDataAnalysisCommand { get; }

        public AnalysisViewModel(
            IReadOnlyList<string> matchFilePaths,
            string analysisTeam,
            Func<Task> navigateBack,
            Func<Task> openVideoAnalysis,
            Func<Task> openDataAnalysis)
        {
            AnalysisTeam = analysisTeam;
            MatchCount = matchFilePaths.Count;

            BackCommand = new AsyncRelayCommand(navigateBack);
            OpenVideoAnalysisCommand = new AsyncRelayCommand(openVideoAnalysis);
            OpenDataAnalysisCommand = new AsyncRelayCommand(openDataAnalysis);
        }
    }
}
