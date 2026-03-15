using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class EditBallContactWindow : Window
    {
        private TextBox? _lastFocused;
        private TextBox[] _fields = Array.Empty<TextBox>();
        private Dictionary<TextBox, FieldConfig> _fieldConfigs = new();

        private record FieldConfig(Func<char, bool> IsValid, Func<char, char> Normalize, int MaxLen);

        public EditBallContactWindow()
        {
            InitializeComponent();
        }

        public EditBallContactWindow(EditBallContactViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            _fields = new[] { TeamBox, PlayerBox, SkillBox, HitTypeBox, EvalBox };

            _fieldConfigs[TeamBox]    = new(c => c is '*' or 'a' or 'A',                        c => c == 'A' ? 'a' : c,           1);
            _fieldConfigs[PlayerBox]  = new(char.IsDigit,                                         c => c,                            2);
            _fieldConfigs[SkillBox]   = new(c => "SRABDEF".Contains(char.ToUpperInvariant(c)),   char.ToUpperInvariant,             1);
            _fieldConfigs[HitTypeBox] = new(c => "HMQTUNO".Contains(char.ToUpperInvariant(c)),   char.ToUpperInvariant,             1);
            _fieldConfigs[EvalBox]    = new(c => "=/-!+#".Contains(c),                           c => c,                            1);

            WireField(TeamBox,    CodeField.Team);
            WireField(PlayerBox,  CodeField.Player);
            WireField(SkillBox,   CodeField.Skill);
            WireField(HitTypeBox, CodeField.HitType);
            WireField(EvalBox,    CodeField.Evaluation);

            PlayerBox.LostFocus += (_, _) =>
            {
                if (int.TryParse(PlayerBox.Text, out var n))
                    PlayerBox.Text = n.ToString("D2");
            };

            foreach (var box in _fields)
                box.AddHandler(KeyDownEvent, OnFieldKeyDown, RoutingStrategies.Tunnel);

            OptionsList.SelectionChanged += (_, _) => _lastFocused?.Focus();

            Opened += (_, _) =>
            {
                var target = Array.FindLast(_fields, b => !string.IsNullOrEmpty(b.Text)) ?? _fields[0];
                target.Focus();
                target.SelectAll();
            };
        }

        private void WireField(TextBox box, CodeField field)
        {
            box.GotFocus += (_, _) =>
            {
                _lastFocused = box;
                if (DataContext is EditBallContactViewModel vm)
                    vm.ActiveField = field;
            };
        }

        private void OnFieldKeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is not TextBox box) return;
            var idx = Array.IndexOf(_fields, box);

            int selStart = Math.Min(box.SelectionStart, box.SelectionEnd);
            int selEnd   = Math.Max(box.SelectionStart, box.SelectionEnd);
            int selLen   = selEnd - selStart;
            bool hasSelection = selLen > 0;

            // Arrow navigation
            if (e.Key == Key.Right && (hasSelection || box.CaretIndex >= (box.Text?.Length ?? 0)))
            {
                if (idx + 1 < _fields.Length)
                {
                    _fields[idx + 1].Focus();
                    _fields[idx + 1].SelectAll();
                    e.Handled = true;
                }
                return;
            }
            if (e.Key == Key.Left && (hasSelection || box.CaretIndex == 0))
            {
                if (idx - 1 >= 0)
                {
                    _fields[idx - 1].Focus();
                    _fields[idx - 1].SelectAll();
                    e.Handled = true;
                }
                return;
            }

            // Up/Down — navigate options list
            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (DataContext is EditBallContactViewModel vm && vm.ActiveOptions.Count > 0)
                {
                    var next = vm.SelectedOptionIndex + (e.Key == Key.Down ? 1 : -1);
                    next = Math.Clamp(next, 0, vm.ActiveOptions.Count - 1);
                    vm.SelectedOptionIndex = next;
                }
                e.Handled = true;
                return;
            }

            // Character input — intercept before TextBox processes it
            if (e.KeySymbol is { Length: 1 } sym
                && !char.IsControl(sym[0])
                && _fieldConfigs.TryGetValue(box, out var cfg))
            {
                var text = box.Text ?? string.Empty;
                // Block if invalid char OR if at max length with no room to insert
                if (!cfg.IsValid(sym[0]) || text.Length - selLen >= cfg.MaxLen)
                {
                    e.Handled = true;
                    return;
                }

                var ch = cfg.Normalize(sym[0]);
                box.Text = text.Remove(selStart, selLen).Insert(selStart, ch.ToString());
                box.SelectionStart = selStart + 1;
                box.SelectionEnd   = selStart + 1;
                box.CaretIndex     = selStart + 1;
                e.Handled = true;
            }
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(null);

        private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditBallContactViewModel vm)
                Close(vm.BuildCode());
            else
                Close(null);
        }
    }
}
