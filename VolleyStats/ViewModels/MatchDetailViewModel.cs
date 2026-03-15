using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Enums;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public partial class MatchDetailViewModel : ViewModelBase
    {
        private readonly string _filePath;
        private readonly Func<Task> _navigateBack;
        private Match? _match;

        public string HomeTeamName { get; private set; } = string.Empty;
        public string AwayTeamName { get; private set; } = string.Empty;
        public string ScoreText    { get; private set; } = "-";

        private string _matchInfoText = string.Empty;
        public string MatchInfoText
        {
            get => _matchInfoText;
            private set => SetProperty(ref _matchInfoText, value);
        }

        public IReadOnlyList<MatchPlayer> HomePlayers => _match?.HomePlayers ?? new List<MatchPlayer>();
        public IReadOnlyList<MatchPlayer> AwayPlayers => _match?.AwayPlayers ?? new List<MatchPlayer>();

        public MatchInfo?     MatchInfo     => _match?.Info;
        public MatchMoreInfo? MatchMoreInfo => _match?.MoreInfo;

        public ObservableCollection<CodeViewModel> Codes { get; } = new();
        public int CodesCount => Codes.Count;

        /// <summary>Manages per-set state: timeouts, substitutions, rotation, set scores.</summary>
        public ScoutingSetManager SetManager { get; } = new();

        /// <summary>Set by the view's code-behind to open the substitution dialog.</summary>
        public Func<bool, Task>? TriggerSubstitutionDialog { get; set; }

        /// <summary>Set by the view's code-behind to open the setter-picker dialog.</summary>
        public Func<bool, Task>? TriggerSetterPickerDialog { get; set; }

        // Video playback
        private string? _videoSourcePath;
        public string? VideoSourcePath
        {
            get => _videoSourcePath;
            private set => SetProperty(ref _videoSourcePath, value);
        }

        private int? _videoSeekSeconds;
        public int? VideoSeekSeconds
        {
            get => _videoSeekSeconds;
            private set => SetProperty(ref _videoSeekSeconds, value);
        }

        private CodeViewModel? _selectedCode;
        public CodeViewModel? SelectedCode
        {
            get => _selectedCode;
            set
            {
                if (SetProperty(ref _selectedCode, value))
                {
                    UpdateCourtPositions(value?.Code);
                    UpdateVideoState(value?.Code);
                }
            }
        }

        // Home court positions (jersey numbers shown on court)
        public string HomePos1 { get; private set; } = "1";
        public string HomePos2 { get; private set; } = "2";
        public string HomePos3 { get; private set; } = "3";
        public string HomePos4 { get; private set; } = "4";
        public string HomePos5 { get; private set; } = "5";
        public string HomePos6 { get; private set; } = "6";

        // Away court positions (jersey numbers shown on court)
        public string AwayPos1 { get; private set; } = "1";
        public string AwayPos2 { get; private set; } = "2";
        public string AwayPos3 { get; private set; } = "3";
        public string AwayPos4 { get; private set; } = "4";
        public string AwayPos5 { get; private set; } = "5";
        public string AwayPos6 { get; private set; } = "6";

        private string _newCodeText = string.Empty;
        public string NewCodeText
        {
            get => _newCodeText;
            set => SetProperty(ref _newCodeText, value);
        }

        public IAsyncRelayCommand AddHomePointCommand { get; }
        public IAsyncRelayCommand AddAwayPointCommand { get; }
        public IAsyncRelayCommand BackCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }

        /// <summary>Set by the view's code-behind to show the end-set confirmation dialog.</summary>
        public Func<TeamSide, Task<bool>>? TriggerEndSetDialog { get; set; }

        /// <summary>Raised after initialization completes so the view can scroll to the last code.</summary>
        public event EventHandler? InitializationCompleted;

        public MatchDetailViewModel(string filePath, Func<Task> navigateBack)
        {
            _filePath     = filePath;
            _navigateBack = navigateBack;

            AddHomePointCommand = new AsyncRelayCommand(AddHomePointAsync, () => SetManager.IsActiveSet);
            AddAwayPointCommand = new AsyncRelayCommand(AddAwayPointAsync, () => SetManager.IsActiveSet);

            BackCommand = new AsyncRelayCommand(_navigateBack);
            SaveCommand = new AsyncRelayCommand(SaveAsync);

            // Notify point commands when IsActiveSet changes
            SetManager.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ScoutingSetManager.IsActiveSet))
                {
                    AddHomePointCommand.NotifyCanExecuteChanged();
                    AddAwayPointCommand.NotifyCanExecuteChanged();
                }
            };

            // When the user rotates (or sub changes position) and no code is selected, refresh court
            SetManager.PositionsChanged += (_, _) =>
            {
                if (_selectedCode == null)
                    UpdateCourtPositions(null);
            };

            // Timeout button pressed → add timeout code to scout list
            SetManager.TimeoutRecorded += side => AddTimeoutCode(side);

            // Substitution button pressed → hand off to the view to open dialog
            SetManager.SubstitutionRequested += side =>
            {
                _ = TriggerSubstitutionDialog?.Invoke(side == TeamSide.Home);
            };
        }

        // ── Initialisation ───────────────────────────────────────────────────

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                var parser = new DvwFileParser();
                _match = parser.ParseDvwFile(_filePath);
            });

            if (_match == null) return;

            HomeTeamName = _match.HomeTeam?.Name ?? string.Empty;
            AwayTeamName = _match.AwayTeam?.Name ?? string.Empty;
            ScoreText    = $"{_match.HomeTeam?.SetsWon ?? 0} - {_match.AwayTeam?.SetsWon ?? 0}";

            OnPropertyChanged(nameof(HomeTeamName));
            OnPropertyChanged(nameof(AwayTeamName));
            OnPropertyChanged(nameof(ScoreText));
            OnPropertyChanged(nameof(HomePlayers));
            OnPropertyChanged(nameof(AwayPlayers));
            OnPropertyChanged(nameof(MatchInfo));
            OnPropertyChanged(nameof(MatchMoreInfo));

            RefreshMatchInfoDisplay();

            if (_match.Sets != null)
                SetManager.LoadFromMatch(_match.Sets);

            if (_match.ScoutCodes != null)
            {
                SetManager.ResumeUnfinishedSet(_match.ScoutCodes);
                SetManager.InitSetterZonesFromCodes(_match.ScoutCodes);
            }

            Codes.Clear();
            if (_match.ScoutCodes != null)
            {
                foreach (var code in _match.ScoutCodes)
                    Codes.Add(new CodeViewModel(code));
            }
            OnPropertyChanged(nameof(CodesCount));

            if (_match.VideoPaths != null && _match.VideoPaths.Count > 0)
                VideoSourcePath = ResolveVideoPath(_match.VideoPaths[0]);

            InitializationCompleted?.Invoke(this, EventArgs.Empty);
        }

        // ── Save ─────────────────────────────────────────────────────────────

        private async Task SaveAsync()
        {
            await Task.Run(() =>
            {
                var rawLines = new List<string>();
                foreach (var cvm in Codes)
                    rawLines.Add(BuildSaveLineFromCode(cvm));

                var parser = new DvwFileParser();
                parser.SaveScoutCodes(_filePath, rawLines);
            });
        }

        /// <summary>
        /// Reconstructs a full 26-field DVW line from a CodeViewModel for saving.
        /// Uses the edited RawCode if changed, preserves all other fields from the Code object,
        /// and fills write time if missing.
        /// </summary>
        private static string BuildSaveLineFromCode(CodeViewModel cvm)
        {
            var code = cvm.Code;
            var parts = new string[26];
            for (int i = 0; i < 26; i++) parts[i] = "";

            // [0] code — use the potentially edited RawCode
            parts[0] = cvm.RawCode;

            // [1] sp, [2] pr
            if (code.Sp != '\0') parts[1] = code.Sp.ToString();
            if (code.Pr != '\0') parts[2] = code.Pr.ToString();

            // [3] empty

            // [4] start coordinate
            if (code.Start != null)
                parts[4] = $"{code.Start.Value.X:00}{code.Start.Value.Y:00}";

            // [5] middle coordinate
            if (code.Middle != null)
                parts[5] = $"{code.Middle.Value.X:00}{code.Middle.Value.Y:00}";

            // [6] end coordinate
            if (code.End != null)
                parts[6] = $"{code.End.Value.X:00}{code.End.Value.Y:00}";

            // [7] write time
            if (code.RecordedAt.HasValue)
                parts[7] = code.RecordedAt.Value.ToString();
            else
                parts[7] = DateTime.Now.ToString("HH.mm.ss");

            // [8] set number
            if (code.SetNumber.HasValue)
                parts[8] = code.SetNumber.Value.ToString();

            // [9] home setter zone
            if (code.HomeSetterZone.HasValue)
                parts[9] = code.HomeSetterZone.Value.ToString();

            // [10] away setter zone
            if (code.AwaySetterZone.HasValue)
                parts[10] = code.AwaySetterZone.Value.ToString();

            // [11] video file
            if (code.VideoFile.HasValue)
                parts[11] = code.VideoFile.Value.ToString();

            // [12] video second
            if (code.VideoSecond.HasValue)
                parts[12] = code.VideoSecond.Value.ToString();

            // [13] empty

            // [14-19] home zones
            for (int i = 0; i < code.HomeZones.Length && i < 6; i++)
                parts[14 + i] = code.HomeZones[i].ToString();

            // [20-25] away zones
            for (int i = 0; i < code.AwayZones.Length && i < 6; i++)
                parts[20 + i] = code.AwayZones[i].ToString();

            return string.Join(";", parts);
        }

        /// <summary>Re-reads MatchInfo/MatchMoreInfo into the display string after editing.</summary>
        public void RefreshMatchInfoDisplay()
        {
            var info = _match?.Info;
            if (info == null) return;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.League)) parts.Add(info.League);
            if (!string.IsNullOrWhiteSpace(info.Phase))  parts.Add(info.Phase);
            if (info.Date != default)                     parts.Add(info.Date.ToString("dd.MM.yyyy"));

            MatchInfoText = string.Join("  |  ", parts);
        }

        // ── Point scoring ─────────────────────────────────────────────────────

        private async Task AddHomePointAsync()
        {
            SetManager.IncrementHomeScore();
            if (SetManager.ShouldEndSet())
                await HandlePossibleSetEnd(TeamSide.Home);
        }

        private async Task AddAwayPointAsync()
        {
            SetManager.IncrementAwayScore();
            if (SetManager.ShouldEndSet())
                await HandlePossibleSetEnd(TeamSide.Away);
        }

        private async Task HandlePossibleSetEnd(TeamSide winner)
        {
            if (TriggerEndSetDialog == null) return;

            var prefix = winner == TeamSide.Home ? "*" : "a";
            InsertCode($"{prefix}p{SetManager.LiveHomeScore}:{SetManager.LiveAwayScore}");

            bool confirmed = await TriggerEndSetDialog(winner);
            if (confirmed)
            {
                InsertCode($"**{SetManager.CurrentSet}set");
                SetManager.EndSet();
            }
        }

        // ── Code deletion ─────────────────────────────────────────────────────

        public void DeleteCode(CodeViewModel code)
        {
            Codes.Remove(code);
            OnPropertyChanged(nameof(CodesCount));
        }

        // ── Code insertion ────────────────────────────────────────────────────

        /// <summary>Appends a timeout scout code (*T / aT) to the codes list.</summary>
        public void AddTimeoutCode(TeamSide side)
        {
            var prefix  = side == TeamSide.Home ? "*" : "a";
            var rawCode = $"{prefix}T";
            InsertCode(rawCode);
        }

        /// <summary>
        /// Appends a substitution scout code, decrements the counter, updates court positions,
        /// and — if the setter was substituted — opens the setter-picker dialog.
        /// </summary>
        public async Task AddSubstitutionCodeAsync(bool isHome, string outJersey, string inJersey)
        {
            var side    = isHome ? TeamSide.Home : TeamSide.Away;
            var prefix  = isHome ? "*" : "a";
            var rawCode = $"{prefix}c{outJersey}:{inJersey}";
            InsertCode(rawCode);

            SetManager.RecordSubstitution(side);
            bool setterWasOut = SetManager.SubstitutePlayer(side, outJersey, inJersey);

            if (setterWasOut && TriggerSetterPickerDialog != null)
                await TriggerSetterPickerDialog(isHome);
        }

        /// <summary>
        /// Inserts the 4 DVW lineup codes (*P>LUp, *z>LUp, aP>LUp, az>LUp) with full zone data.
        /// </summary>
        public void InsertLineUpCodes(string[] homePos, string[] awayPos, int homeSetterZone, int awaySetterZone)
        {
            string homeSetter = homeSetterZone >= 1 && homeSetterZone <= 6 ? homePos[homeSetterZone - 1] : "00";
            string awaySetter = awaySetterZone >= 1 && awaySetterZone <= 6 ? awayPos[awaySetterZone - 1] : "00";

            var homePlayerCode = $"*P{int.Parse(homeSetter):00}>LUp";
            var homeZoneCode   = $"*z{homeSetterZone}>LUp";
            var awayPlayerCode = $"aP{int.Parse(awaySetter):00}>LUp";
            var awayZoneCode   = $"az{awaySetterZone}>LUp";

            InsertCodeWithZones(homePlayerCode, homePos, awayPos, homeSetterZone, awaySetterZone);
            InsertCodeWithZones(homeZoneCode, homePos, awayPos, homeSetterZone, awaySetterZone);
            InsertCodeWithZones(awayPlayerCode, homePos, awayPos, homeSetterZone, awaySetterZone);
            InsertCodeWithZones(awayZoneCode, homePos, awayPos, homeSetterZone, awaySetterZone);
        }

        private void InsertCodeWithZones(string rawCode, string[] homePos, string[] awayPos, int homeSetterZone, int awaySetterZone)
        {
            var rawLine = BuildFullRawLine(rawCode, homePos, awayPos, homeSetterZone, awaySetterZone);
            var code    = new Code(rawLine);
            Codes.Add(new CodeViewModel(code));
            OnPropertyChanged(nameof(CodesCount));
        }

        private void InsertCode(string rawCode)
        {
            var rawLine = BuildScoutRawLine(rawCode);
            var code    = CodeClassifier.ParseSingleLine(rawLine);
            Codes.Add(new CodeViewModel(code));
            OnPropertyChanged(nameof(CodesCount));
        }

        /// <summary>
        /// Builds a full 26-field DVW raw line using current SetManager state.
        /// Format: code;sp;pr;;start;middle;end;writeTime;set;hSetterZ;aSetterZ;vidFile;vidSec;;h1-h6;a1-a6;
        /// </summary>
        private string BuildScoutRawLine(string rawCode)
        {
            return BuildDvwLine(rawCode, null, null, null, null, null);
        }

        private string BuildFullRawLine(string rawCode, string[] homePos, string[] awayPos, int homeSetterZone, int awaySetterZone)
        {
            return BuildDvwLine(rawCode, homePos, awayPos, homeSetterZone, awaySetterZone, null);
        }

        private string BuildDvwLine(string rawCode, string[]? homePos, string[]? awayPos,
            int? homeSetterZone, int? awaySetterZone, Code? sourceCode)
        {
            var parts = new string[26];
            for (int i = 0; i < 26; i++) parts[i] = "";

            // [0] code
            parts[0] = rawCode;

            // [1] sp, [2] pr — leave empty for new codes
            if (sourceCode != null)
            {
                if (sourceCode.Sp != '\0') parts[1] = sourceCode.Sp.ToString();
                if (sourceCode.Pr != '\0') parts[2] = sourceCode.Pr.ToString();
            }

            // [3] empty

            // [4] start, [5] middle, [6] end coordinates
            if (sourceCode?.Start != null)
                parts[4] = $"{sourceCode.Start.Value.X:00}{sourceCode.Start.Value.Y:00}";
            if (sourceCode?.Middle != null)
                parts[5] = $"{sourceCode.Middle.Value.X:00}{sourceCode.Middle.Value.Y:00}";
            if (sourceCode?.End != null)
                parts[6] = $"{sourceCode.End.Value.X:00}{sourceCode.End.Value.Y:00}";

            // [7] write time — current system time hh.mm.ss
            parts[7] = DateTime.Now.ToString("HH.mm.ss");

            // [8] set number
            parts[8] = SetManager.CurrentSet.ToString();

            // [9] home setter zone, [10] away setter zone
            parts[9]  = (homeSetterZone ?? SetManager.HomeSetterZone).ToString();
            parts[10] = (awaySetterZone ?? SetManager.AwaySetterZone).ToString();

            // [11] video file, [12] video second — leave empty
            // [13] empty

            // [14-19] home zones, [20-25] away zones
            if (homePos != null)
            {
                for (int i = 0; i < 6; i++)
                    parts[14 + i] = homePos[i];
            }
            else
            {
                for (int i = 0; i < 6; i++)
                    parts[14 + i] = SetManager.GetHomePosition(i + 1);
            }

            if (awayPos != null)
            {
                for (int i = 0; i < 6; i++)
                    parts[20 + i] = awayPos[i];
            }
            else
            {
                for (int i = 0; i < 6; i++)
                    parts[20 + i] = SetManager.GetAwayPosition(i + 1);
            }

            return string.Join(";", parts);
        }

        // ── Video ─────────────────────────────────────────────────────────────

        public void SeekToSelectedCode() => UpdateVideoState(_selectedCode?.Code);

        public void ChangeVideoPath(string path)
        {
            VideoSourcePath = path;
        }

        private void UpdateVideoState(Code? code)
        {
            if (code == null) return;

            if (code.VideoFile.HasValue && _match?.VideoPaths != null)
            {
                var idx = code.VideoFile.Value - 1;
                if (idx >= 0 && idx < _match.VideoPaths.Count)
                    VideoSourcePath = ResolveVideoPath(_match.VideoPaths[idx]);
            }

            if (code.VideoSecond.HasValue)
                VideoSeekSeconds = code.VideoSecond.Value;
        }

        private string? ResolveVideoPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return null;
            if (Path.IsPathRooted(rawPath) && File.Exists(rawPath)) return rawPath;
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null)
            {
                var combined = Path.Combine(dir, rawPath);
                if (File.Exists(combined)) return combined;
            }
            return rawPath;
        }

        // ── Court positions ───────────────────────────────────────────────────

        private void UpdateCourtPositions(Code? code)
        {
            if (code == null || code.HomeZones.Length == 0)
            {
                HomePos1 = SetManager.GetHomePosition(1);
                HomePos2 = SetManager.GetHomePosition(2);
                HomePos3 = SetManager.GetHomePosition(3);
                HomePos4 = SetManager.GetHomePosition(4);
                HomePos5 = SetManager.GetHomePosition(5);
                HomePos6 = SetManager.GetHomePosition(6);
            }
            else
            {
                HomePos1 = ZoneText(code.HomeZones, 0);
                HomePos2 = ZoneText(code.HomeZones, 1);
                HomePos3 = ZoneText(code.HomeZones, 2);
                HomePos4 = ZoneText(code.HomeZones, 3);
                HomePos5 = ZoneText(code.HomeZones, 4);
                HomePos6 = ZoneText(code.HomeZones, 5);
            }

            if (code == null || code.AwayZones.Length == 0)
            {
                AwayPos1 = SetManager.GetAwayPosition(1);
                AwayPos2 = SetManager.GetAwayPosition(2);
                AwayPos3 = SetManager.GetAwayPosition(3);
                AwayPos4 = SetManager.GetAwayPosition(4);
                AwayPos5 = SetManager.GetAwayPosition(5);
                AwayPos6 = SetManager.GetAwayPosition(6);
            }
            else
            {
                AwayPos1 = ZoneText(code.AwayZones, 0);
                AwayPos2 = ZoneText(code.AwayZones, 1);
                AwayPos3 = ZoneText(code.AwayZones, 2);
                AwayPos4 = ZoneText(code.AwayZones, 3);
                AwayPos5 = ZoneText(code.AwayZones, 4);
                AwayPos6 = ZoneText(code.AwayZones, 5);
            }

            OnPropertyChanged(nameof(HomePos1)); OnPropertyChanged(nameof(HomePos2));
            OnPropertyChanged(nameof(HomePos3)); OnPropertyChanged(nameof(HomePos4));
            OnPropertyChanged(nameof(HomePos5)); OnPropertyChanged(nameof(HomePos6));
            OnPropertyChanged(nameof(AwayPos1)); OnPropertyChanged(nameof(AwayPos2));
            OnPropertyChanged(nameof(AwayPos3)); OnPropertyChanged(nameof(AwayPos4));
            OnPropertyChanged(nameof(AwayPos5)); OnPropertyChanged(nameof(AwayPos6));
        }

        private static string ZoneText(int[] zones, int index)
        {
            if (index >= zones.Length) return (index + 1).ToString();
            var v = zones[index];
            return v > 0 ? v.ToString() : (index + 1).ToString();
        }
    }
}
