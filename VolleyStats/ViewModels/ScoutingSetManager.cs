using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Enums;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    /// <summary>
    /// Single source of truth for the live set state during scouting:
    /// court positions, setter zones, live score, timeouts, substitutions,
    /// rotation, and per-set score history.
    /// </summary>
    public class ScoutingSetManager : ViewModelBase
    {
        // ── Court positions ───────────────────────────────────────────────────
        // Index 0 = position 1 … index 5 = position 6

        private readonly string[] _homePositions = { "1", "2", "3", "4", "5", "6" };
        private readonly string[] _awayPositions = { "1", "2", "3", "4", "5", "6" };

        // ── Active-set state ──────────────────────────────────────────────────

        private bool _isActiveSet;
        public bool IsActiveSet
        {
            get => _isActiveSet;
            private set
            {
                if (SetProperty(ref _isActiveSet, value))
                {
                    OnPropertyChanged(nameof(IsNotActiveSet));
                    NotifyAllCanExecute();
                }
            }
        }

        /// <summary>Convenience inverse for AXAML IsVisible bindings.</summary>
        public bool IsNotActiveSet => !_isActiveSet;

        // ── Live set score ────────────────────────────────────────────────────

        private int _liveHomeScore;
        public int LiveHomeScore
        {
            get => _liveHomeScore;
            private set => SetProperty(ref _liveHomeScore, value);
        }

        private int _liveAwayScore;
        public int LiveAwayScore
        {
            get => _liveAwayScore;
            private set => SetProperty(ref _liveAwayScore, value);
        }

        // ── Set counter ───────────────────────────────────────────────────────

        private int _currentSet = 1;
        public int CurrentSet
        {
            get => _currentSet;
            private set => SetProperty(ref _currentSet, value);
        }

        // ── Serving team ─────────────────────────────────────────────────────

        private bool _isHomeServing;
        public bool IsHomeServing
        {
            get => _isHomeServing;
            private set
            {
                if (SetProperty(ref _isHomeServing, value))
                    OnPropertyChanged(nameof(IsAwayServing));
            }
        }

        public bool IsAwayServing => !_isHomeServing;

        // ── Setter zones (1-6, 0 = unknown) ──────────────────────────────────

        private int _homeSetterZone;
        public int HomeSetterZone
        {
            get => _homeSetterZone;
            private set
            {
                if (SetProperty(ref _homeSetterZone, value))
                    OnPropertyChanged(nameof(HomeSetterJersey));
            }
        }

        private int _awaySetterZone;
        public int AwaySetterZone
        {
            get => _awaySetterZone;
            private set
            {
                if (SetProperty(ref _awaySetterZone, value))
                    OnPropertyChanged(nameof(AwaySetterJersey));
            }
        }

        public string HomeSetterJersey =>
            HomeSetterZone >= 1 && HomeSetterZone <= 6 ? _homePositions[HomeSetterZone - 1] : string.Empty;

        public string AwaySetterJersey =>
            AwaySetterZone >= 1 && AwaySetterZone <= 6 ? _awayPositions[AwaySetterZone - 1] : string.Empty;

        // ── Timeouts ──────────────────────────────────────────────────────────

        private int _homeTimeoutsLeft = 2;
        public int HomeTimeoutsLeft
        {
            get => _homeTimeoutsLeft;
            private set => SetProperty(ref _homeTimeoutsLeft, value);
        }

        private int _awayTimeoutsLeft = 2;
        public int AwayTimeoutsLeft
        {
            get => _awayTimeoutsLeft;
            private set => SetProperty(ref _awayTimeoutsLeft, value);
        }

        // ── Substitutions ─────────────────────────────────────────────────────

        private int _homeSubstitutionsLeft = 6;
        public int HomeSubstitutionsLeft
        {
            get => _homeSubstitutionsLeft;
            private set => SetProperty(ref _homeSubstitutionsLeft, value);
        }

        private int _awaySubstitutionsLeft = 6;
        public int AwaySubstitutionsLeft
        {
            get => _awaySubstitutionsLeft;
            private set => SetProperty(ref _awaySubstitutionsLeft, value);
        }

        // ── Set scores history ────────────────────────────────────────────────

        public ObservableCollection<SetScoreItemViewModel> SetScores { get; } = new();

        // ── Events ────────────────────────────────────────────────────────────

        public event Action<TeamSide>? TimeoutRecorded;
        public event Action<TeamSide>? SubstitutionRequested;
        public event EventHandler?     PositionsChanged;

        // ── Commands ──────────────────────────────────────────────────────────

        public IRelayCommand StartSetCommand        { get; }
        public IRelayCommand HomeTimeoutCommand     { get; }
        public IRelayCommand AwayTimeoutCommand     { get; }
        public IRelayCommand HomeSubstitutionCommand{ get; }
        public IRelayCommand AwaySubstitutionCommand{ get; }
        public IRelayCommand RotateHomeCommand      { get; }
        public IRelayCommand RotateAwayCommand      { get; }
        public IRelayCommand EndSetCommand          { get; }
        public IRelayCommand ToggleServingCommand   { get; }

        public ScoutingSetManager()
        {
            StartSetCommand         = new RelayCommand(StartSet,                  () => !IsActiveSet);
            HomeTimeoutCommand      = new RelayCommand(UseHomeTimeout,            () => IsActiveSet && HomeTimeoutsLeft > 0);
            AwayTimeoutCommand      = new RelayCommand(UseAwayTimeout,            () => IsActiveSet && AwayTimeoutsLeft > 0);
            HomeSubstitutionCommand = new RelayCommand(RequestHomeSubstitution,   () => IsActiveSet && HomeSubstitutionsLeft > 0);
            AwaySubstitutionCommand = new RelayCommand(RequestAwaySubstitution,   () => IsActiveSet && AwaySubstitutionsLeft > 0);
            RotateHomeCommand       = new RelayCommand(RotateHome);
            RotateAwayCommand       = new RelayCommand(RotateAway);
            EndSetCommand           = new RelayCommand(EndSet,                    () => IsActiveSet);
            ToggleServingCommand    = new RelayCommand(ToggleServing,             () => IsActiveSet);
        }

        // ── Set lifecycle ─────────────────────────────────────────────────────

        private void StartSet()
        {
            LiveHomeScore = 0;
            LiveAwayScore = 0;
            IsActiveSet   = true;
        }

        public void StartSetWithLineup(string[] homePos, string[] awayPos, int homeSetterZone, int awaySetterZone, bool isHomeServing)
        {
            for (int i = 0; i < 6; i++)
            {
                _homePositions[i] = homePos[i];
                _awayPositions[i] = awayPos[i];
            }
            HomeSetterZone = homeSetterZone;
            AwaySetterZone = awaySetterZone;
            IsHomeServing  = isHomeServing;
            LiveHomeScore  = 0;
            LiveAwayScore  = 0;
            IsActiveSet    = true;
            PositionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleServing()
        {
            IsHomeServing = !IsHomeServing;
        }

        /// <summary>
        /// Finalises the current set: saves score to history, advances CurrentSet,
        /// resets per-set counters, and marks no active set.
        /// Called both by the EndSetCommand and by MatchDetailViewModel after end-of-set confirmation.
        /// </summary>
        public void EndSet()
        {
            SetScores.Add(new SetScoreItemViewModel(CurrentSet, LiveHomeScore, LiveAwayScore));
            CurrentSet++;
            LiveHomeScore         = 0;
            LiveAwayScore         = 0;
            HomeTimeoutsLeft      = 2;
            AwayTimeoutsLeft      = 2;
            HomeSubstitutionsLeft = 6;
            AwaySubstitutionsLeft = 6;
            IsActiveSet           = false;   // fires NotifyAllCanExecute via property setter
        }

        // ── Score tracking ────────────────────────────────────────────────────

        public void IncrementHomeScore() => LiveHomeScore++;
        public void IncrementAwayScore() => LiveAwayScore++;

        /// <summary>
        /// Returns true if the current live scores satisfy the volleyball set-ending condition.
        /// Sets 1-4: first to 25 with ≥2 lead. Set 5: first to 15 with ≥2 lead.
        /// </summary>
        public bool ShouldEndSet()
        {
            int win = CurrentSet >= 5 ? 15 : 25;
            bool homeWon = LiveHomeScore >= win && LiveHomeScore - LiveAwayScore >= 2;
            bool awayWon = LiveAwayScore >= win && LiveAwayScore - LiveHomeScore >= 2;
            return homeWon || awayWon;
        }

        /// <summary>Returns which team has won the set under current scores, or null if undecided.</summary>
        public TeamSide? GetSetWinner()
        {
            int win = CurrentSet >= 5 ? 15 : 25;
            if (LiveHomeScore >= win && LiveHomeScore - LiveAwayScore >= 2) return TeamSide.Home;
            if (LiveAwayScore >= win && LiveAwayScore - LiveHomeScore >= 2) return TeamSide.Away;
            return null;
        }

        // ── Position accessors ────────────────────────────────────────────────

        public string GetHomePosition(int pos) => pos >= 1 && pos <= 6 ? _homePositions[pos - 1] : pos.ToString();
        public string GetAwayPosition(int pos) => pos >= 1 && pos <= 6 ? _awayPositions[pos - 1] : pos.ToString();

        /// <summary>Returns the 1-based zone of the given jersey number, or 0 if not found.</summary>
        public int FindZone(TeamSide side, string jersey)
        {
            var pos = side == TeamSide.Home ? _homePositions : _awayPositions;
            for (int i = 0; i < pos.Length; i++)
                if (pos[i] == jersey) return i + 1;
            return 0;
        }

        public void SetSetterZone(TeamSide side, int zone)
        {
            if (side == TeamSide.Home) HomeSetterZone = zone;
            else                       AwaySetterZone = zone;
        }

        // ── Timeouts ──────────────────────────────────────────────────────────

        private void UseHomeTimeout()
        {
            HomeTimeoutsLeft--;
            ((RelayCommand)HomeTimeoutCommand).NotifyCanExecuteChanged();
            TimeoutRecorded?.Invoke(TeamSide.Home);
        }

        private void UseAwayTimeout()
        {
            AwayTimeoutsLeft--;
            ((RelayCommand)AwayTimeoutCommand).NotifyCanExecuteChanged();
            TimeoutRecorded?.Invoke(TeamSide.Away);
        }

        // ── Substitutions ─────────────────────────────────────────────────────

        private void RequestHomeSubstitution() => SubstitutionRequested?.Invoke(TeamSide.Home);
        private void RequestAwaySubstitution() => SubstitutionRequested?.Invoke(TeamSide.Away);

        public void RecordSubstitution(TeamSide side)
        {
            if (side == TeamSide.Home && HomeSubstitutionsLeft > 0)
            {
                HomeSubstitutionsLeft--;
                ((RelayCommand)HomeSubstitutionCommand).NotifyCanExecuteChanged();
            }
            else if (side == TeamSide.Away && AwaySubstitutionsLeft > 0)
            {
                AwaySubstitutionsLeft--;
                ((RelayCommand)AwaySubstitutionCommand).NotifyCanExecuteChanged();
            }
        }

        /// <summary>Swaps a jersey in the positions array. Returns true if the setter was swapped out.</summary>
        public bool SubstitutePlayer(TeamSide side, string outJersey, string inJersey)
        {
            var positions  = side == TeamSide.Home ? _homePositions : _awayPositions;
            var setterZone = side == TeamSide.Home ? HomeSetterZone  : AwaySetterZone;
            bool setterOut = false;

            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i] == outJersey)
                {
                    positions[i] = inJersey;
                    if (setterZone >= 1 && setterZone <= 6 && i == setterZone - 1)
                        setterOut = true;
                    break;
                }
            }

            OnPropertyChanged(nameof(HomeSetterJersey));
            OnPropertyChanged(nameof(AwaySetterJersey));
            PositionsChanged?.Invoke(this, EventArgs.Empty);
            return setterOut;
        }

        // ── Rotation ──────────────────────────────────────────────────────────

        private void RotateHome()
        {
            RightShift(_homePositions);
            if (HomeSetterZone >= 1 && HomeSetterZone <= 6)
                HomeSetterZone = (HomeSetterZone % 6) + 1;
            PositionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RotateAway()
        {
            RightShift(_awayPositions);
            if (AwaySetterZone >= 1 && AwaySetterZone <= 6)
                AwaySetterZone = (AwaySetterZone % 6) + 1;
            PositionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private static void RightShift(string[] arr)
        {
            var last = arr[5];
            for (int i = 5; i > 0; i--)
                arr[i] = arr[i - 1];
            arr[0] = last;
        }

        // ── Load from saved match ─────────────────────────────────────────────

        public void LoadFromMatch(List<MatchSet> sets)
        {
            SetScores.Clear();
            for (int i = 0; i < sets.Count; i++)
            {
                var s = sets[i];
                if (s.FinalScore != null)
                    SetScores.Add(new SetScoreItemViewModel(i + 1, s.FinalScore.HomeTeamScore, s.FinalScore.AwayTeamScore));
            }
            CurrentSet  = SetScores.Count + 1;
            IsActiveSet = false;
        }

        /// <summary>
        /// Detects an unfinished set (no **Nset end marker after the last finished set)
        /// and resumes it by reconstructing score, timeouts, substitutions, positions,
        /// and setter zones from the scout codes.
        /// </summary>
        public void ResumeUnfinishedSet(IReadOnlyList<Code> allCodes)
        {
            // Find the last end-set marker
            int lastEndSetIdx = -1;
            for (int i = allCodes.Count - 1; i >= 0; i--)
            {
                if (allCodes[i] is CodeEndSet)
                {
                    lastEndSetIdx = i;
                    break;
                }
            }

            // No codes after the last end-set → nothing to resume
            if (lastEndSetIdx >= allCodes.Count - 1) return;

            // Get positions from the last code with valid (all non-zero) zone data
            string[]? homePositions = null, awayPositions = null;
            for (int i = allCodes.Count - 1; i > lastEndSetIdx; i--)
            {
                var c = allCodes[i];
                if (homePositions == null && c.HomeZones.Length >= 6)
                {
                    bool valid = c.HomeZones.Take(6).All(z => z > 0);
                    if (valid)
                        homePositions = c.HomeZones.Take(6).Select(z => z.ToString()).ToArray();
                }
                if (awayPositions == null && c.AwayZones.Length >= 6)
                {
                    bool valid = c.AwayZones.Take(6).All(z => z > 0);
                    if (valid)
                        awayPositions = c.AwayZones.Take(6).Select(z => z.ToString()).ToArray();
                }
                if (homePositions != null && awayPositions != null) break;
            }

            // If no position data found at all, can't resume
            if (homePositions == null && awayPositions == null) return;

            // Scan unfinished set codes for state
            int homeScore = 0, awayScore = 0;
            int homeTimeouts = 0, awayTimeouts = 0;
            int homeSubs = 0, awaySubs = 0;
            int homeSetterZ = 0, awaySetterZ = 0;
            bool isHomeServing = true;
            bool servingFromScore = false;

            for (int i = lastEndSetIdx + 1; i < allCodes.Count; i++)
            {
                var code = allCodes[i];
                switch (code)
                {
                    case CodeLineUp lu:
                        if (lu.Team == TeamSide.Home && lu.SetterZone.HasValue)
                            homeSetterZ = lu.SetterZone.Value;
                        else if (lu.Team == TeamSide.Away && lu.SetterZone.HasValue)
                            awaySetterZ = lu.SetterZone.Value;
                        break;
                    case CodeRotation rot:
                        if (rot.Team == TeamSide.Home && rot.SetterZone.HasValue)
                            homeSetterZ = rot.SetterZone.Value;
                        else if (rot.Team == TeamSide.Away && rot.SetterZone.HasValue)
                            awaySetterZ = rot.SetterZone.Value;
                        break;
                    case CodeTimeout to:
                        if (to.Team == TeamSide.Home) homeTimeouts++;
                        else awayTimeouts++;
                        break;
                    case CodeSubstitution sub:
                        if (sub.Team == TeamSide.Home) homeSubs++;
                        else awaySubs++;
                        break;
                    case CodeScoreMarker sm:
                        homeScore = sm.HomePoints ?? 0;
                        awayScore = sm.AwayPoints ?? 0;
                        isHomeServing = sm.Team == TeamSide.Home;
                        servingFromScore = true;
                        break;
                    case BallContactCode bc:
                        // Use the first serve code to determine initial serving
                        // (score markers override this later)
                        if (!servingFromScore && bc.Skill == Skill.Serve)
                            isHomeServing = bc.Team == TeamSide.Home;
                        break;
                }
            }

            // Apply positions
            if (homePositions != null)
                for (int i = 0; i < 6; i++) _homePositions[i] = homePositions[i];
            if (awayPositions != null)
                for (int i = 0; i < 6; i++) _awayPositions[i] = awayPositions[i];

            if (homeSetterZ > 0) HomeSetterZone = homeSetterZ;
            if (awaySetterZ > 0) AwaySetterZone = awaySetterZ;

            LiveHomeScore = homeScore;
            LiveAwayScore = awayScore;
            HomeTimeoutsLeft = Math.Max(0, 2 - homeTimeouts);
            AwayTimeoutsLeft = Math.Max(0, 2 - awayTimeouts);
            HomeSubstitutionsLeft = Math.Max(0, 6 - homeSubs);
            AwaySubstitutionsLeft = Math.Max(0, 6 - awaySubs);
            IsHomeServing = isHomeServing;
            IsActiveSet = true;

            PositionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void InitSetterZonesFromCodes(IReadOnlyList<Code> codes)
        {
            int? homeZone = null, awayZone = null;
            foreach (var code in codes)
            {
                if (code is CodeLineUp lu)
                {
                    if (lu.Team == TeamSide.Home && lu.SetterZone.HasValue) homeZone = lu.SetterZone.Value;
                    else if (lu.Team == TeamSide.Away && lu.SetterZone.HasValue) awayZone = lu.SetterZone.Value;
                }
                else if (code is CodeRotation rot)
                {
                    if (rot.Team == TeamSide.Home && rot.SetterZone.HasValue) homeZone = rot.SetterZone.Value;
                    else if (rot.Team == TeamSide.Away && rot.SetterZone.HasValue) awayZone = rot.SetterZone.Value;
                }
            }
            if (homeZone.HasValue) HomeSetterZone = homeZone.Value;
            if (awayZone.HasValue) AwaySetterZone = awayZone.Value;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void NotifyAllCanExecute()
        {
            ((RelayCommand)StartSetCommand).NotifyCanExecuteChanged();
            ((RelayCommand)HomeTimeoutCommand).NotifyCanExecuteChanged();
            ((RelayCommand)AwayTimeoutCommand).NotifyCanExecuteChanged();
            ((RelayCommand)HomeSubstitutionCommand).NotifyCanExecuteChanged();
            ((RelayCommand)AwaySubstitutionCommand).NotifyCanExecuteChanged();
            ((RelayCommand)EndSetCommand).NotifyCanExecuteChanged();
            ((RelayCommand)ToggleServingCommand).NotifyCanExecuteChanged();
        }
    }
}
