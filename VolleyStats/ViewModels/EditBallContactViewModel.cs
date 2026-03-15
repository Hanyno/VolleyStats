using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VolleyStats.Enums;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public enum CodeField { None, Team, Player, Skill, HitType, Evaluation }

    public record CodeOption(string Value, string Description);

    public class EditBallContactViewModel : ObservableObject
    {
        // ── text fields ──────────────────────────────────────────

        private string _teamText = "*";
        public string TeamText
        {
            get => _teamText;
            set
            {
                var last = (value ?? string.Empty).LastOrDefault(c => c is '*' or 'a' or 'A');
                if (last == default && !string.IsNullOrEmpty(value)) { OnPropertyChanged(); return; }
                if (SetProperty(ref _teamText, last == 'A' ? "a" : last == default ? string.Empty : last.ToString())
                    && _activeField == CodeField.Player)
                    RefreshOptions();
            }
        }

        private string _playerText = "00";
        public string PlayerText
        {
            get => _playerText;
            set
            {
                var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
                if (digits.Length > 2) digits = digits[^2..];
                if (SetProperty(ref _playerText, digits)) RefreshSelectedOption();
            }
        }

        private string _skillText = string.Empty;
        public string SkillText
        {
            get => _skillText;
            set
            {
                var last = (value ?? string.Empty).ToUpperInvariant().LastOrDefault(c => "SRABDEF".Contains(c));
                if (last == default && !string.IsNullOrEmpty(value)) { OnPropertyChanged(); return; }
                if (SetProperty(ref _skillText, last == default ? string.Empty : last.ToString())) RefreshSelectedOption();
            }
        }

        private string _hitTypeText = string.Empty;
        public string HitTypeText
        {
            get => _hitTypeText;
            set
            {
                var last = (value ?? string.Empty).ToUpperInvariant().LastOrDefault(c => "HMQTUNO".Contains(c));
                if (last == default && !string.IsNullOrEmpty(value)) { OnPropertyChanged(); return; }
                if (SetProperty(ref _hitTypeText, last == default ? string.Empty : last.ToString())) RefreshSelectedOption();
            }
        }

        private string _evaluationText = string.Empty;
        public string EvaluationText
        {
            get => _evaluationText;
            set
            {
                var last = (value ?? string.Empty).LastOrDefault(c => "=/-!+#".Contains(c));
                if (last == default && !string.IsNullOrEmpty(value)) { OnPropertyChanged(); return; }
                if (SetProperty(ref _evaluationText, last == default ? string.Empty : last.ToString())) RefreshSelectedOption();
            }
        }

        // ── options ──────────────────────────────────────────────

        public ObservableCollection<CodeOption> ActiveOptions { get; } = new();

        private int _selectedOptionIndex = -1;
        public int SelectedOptionIndex
        {
            get => _selectedOptionIndex;
            set
            {
                if (SetProperty(ref _selectedOptionIndex, value) && value >= 0 && value < ActiveOptions.Count)
                    ApplyOption(ActiveOptions[value].Value);
            }
        }

        private CodeField _activeField = CodeField.None;
        public CodeField ActiveField
        {
            get => _activeField;
            set { _activeField = value; RefreshOptions(); }
        }

        private static readonly CodeOption[] _teamOptions =
        [
            new("*", "Home"),
            new("a", "Away"),
        ];

        private static readonly CodeOption[] _skillOptions =
        [
            new("S", "Serve"),
            new("R", "Reception"),
            new("A", "Attack"),
            new("B", "Block"),
            new("D", "Dig"),
            new("E", "Set"),
            new("F", "Free Ball"),
        ];

        private static readonly CodeOption[] _hitTypeOptions =
        [
            new("H", "High"),
            new("M", "Medium"),
            new("Q", "Quick"),
            new("T", "Tense"),
            new("U", "Super"),
            new("N", "Fast"),
            new("O", "Other"),
        ];

        private static readonly CodeOption[] _evaluationOptions =
        [
            new("=", "Error"),
            new("/", "Very Poor or Blocked"),
            new("-", "Poor"),
            new("!", "Insufficient or Covered"),
            new("+", "Positive"),
            new("#", "Point"),
        ];

        private void RefreshOptions()
        {
            ActiveOptions.Clear();
            if (_activeField == CodeField.Player)
            {
                var players = _teamText == "a" ? _awayPlayers : _homePlayers;
                foreach (var p in players.OrderBy(p => p.JerseyNumber))
                {
                    var num = p.JerseyNumber.ToString("D2");
                    var parts = new[] { p.FirstName?.Trim(), p.LastName?.Trim() }
                        .Where(s => !string.IsNullOrEmpty(s));
                    ActiveOptions.Add(new CodeOption(num, string.Join(" ", parts)));
                }
            }
            else
            {
                var opts = _activeField switch
                {
                    CodeField.Team       => _teamOptions,
                    CodeField.Skill      => _skillOptions,
                    CodeField.HitType    => _hitTypeOptions,
                    CodeField.Evaluation => _evaluationOptions,
                    _                    => Array.Empty<CodeOption>()
                };
                foreach (var o in opts)
                    ActiveOptions.Add(o);
            }

            // Set selected index to match current value — set backing field directly
            // to avoid triggering ApplyOption on refresh
            _selectedOptionIndex = FindOptionIndex(GetCurrentFieldValue());
            OnPropertyChanged(nameof(SelectedOptionIndex));
        }

        private string GetCurrentFieldValue() => _activeField switch
        {
            CodeField.Team       => _teamText,
            CodeField.Player     => int.TryParse(_playerText, out var n) ? n.ToString("D2") : _playerText,
            CodeField.Skill      => _skillText,
            CodeField.HitType    => _hitTypeText,
            CodeField.Evaluation => _evaluationText,
            _                    => string.Empty
        };

        private int FindOptionIndex(string value)
        {
            for (int i = 0; i < ActiveOptions.Count; i++)
                if (ActiveOptions[i].Value == value) return i;
            return -1;
        }

        private void RefreshSelectedOption()
        {
            var idx = FindOptionIndex(GetCurrentFieldValue());
            if (_selectedOptionIndex == idx) return;
            _selectedOptionIndex = idx;
            OnPropertyChanged(nameof(SelectedOptionIndex));
        }

        public void ApplyOption(string value)
        {
            switch (_activeField)
            {
                case CodeField.Team:       TeamText       = value; break;
                case CodeField.Player:     PlayerText     = value; break;
                case CodeField.Skill:      SkillText      = value; break;
                case CodeField.HitType:    HitTypeText    = value; break;
                case CodeField.Evaluation: EvaluationText = value; break;
            }
        }

        // ── player lists ─────────────────────────────────────────

        private readonly IReadOnlyList<MatchPlayer> _homePlayers;
        private readonly IReadOnlyList<MatchPlayer> _awayPlayers;

        // ── constructor ──────────────────────────────────────────

        public EditBallContactViewModel(BallContactCode code,
            IReadOnlyList<MatchPlayer>? homePlayers = null,
            IReadOnlyList<MatchPlayer>? awayPlayers = null)
        {
            _homePlayers = homePlayers ?? Array.Empty<MatchPlayer>();
            _awayPlayers = awayPlayers ?? Array.Empty<MatchPlayer>();
            _teamText = code.Team == TeamSide.Away ? "a" : "*";
            _playerText = (code.PlayerNumber ?? 0).ToString("D2");
            _skillText = code.Skill switch
            {
                Skill.Serve     => "S",
                Skill.Reception => "R",
                Skill.Attack    => "A",
                Skill.Block     => "B",
                Skill.Dig       => "D",
                Skill.Set       => "E",
                Skill.FreeBall  => "F",
                _               => string.Empty
            };
            _hitTypeText = code.HitType?.ToString() ?? string.Empty;
            _evaluationText = code.Evaluation switch
            {
                Evaluation.Error                 => "=",
                Evaluation.VeryPoorOrBlocked     => "/",
                Evaluation.Poor                  => "-",
                Evaluation.InsufficientOrCovered => "!",
                Evaluation.Positive              => "+",
                Evaluation.Point                 => "#",
                _                                => string.Empty
            };
        }

        // ── output ───────────────────────────────────────────────

        public string BuildCode()
        {
            var team   = TeamText.Trim().ToLowerInvariant() == "a" ? "a" : "*";
            var player = int.TryParse(PlayerText.Trim(), out var n)
                ? Math.Clamp(n, 0, 99).ToString("D2")
                : "00";
            var skill  = SkillText.Trim().ToUpperInvariant();
            var hit    = HitTypeText.Trim().ToUpperInvariant();
            var eval   = EvaluationText.Trim();
            return $"{team}{player}{skill}{hit}{eval}";
        }
    }
}
