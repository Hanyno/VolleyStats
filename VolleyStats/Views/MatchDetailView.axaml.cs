using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using VolleyStats.Data;
using VolleyStats.Models;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class MatchDetailView : UserControl
    {
        private readonly KeyboardShortcutsStore _shortcutsStore = new();
        private List<KeyboardShortcut>? _shortcuts;

        public MatchDetailView()
        {
            InitializeComponent();

            CodesList.DoubleTapped += (_, _) => OpenEditDialog();
            CodesList.KeyDown      += CodesListOnKeyDown;
            CodesList.Tapped       += (_, _) => ViewModel?.SeekToSelectedCode();

            ScoutCodeBox.AddHandler(KeyDownEvent, OnScoutCodeBoxKeyDown, RoutingStrategies.Tunnel);

            // Wire up the substitution dialog whenever the DataContext is assigned
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is MatchDetailViewModel vm)
            {
                vm.TriggerSubstitutionDialog = isHome => OpenSubstitutionAsync(isHome);
                vm.TriggerSetterPickerDialog  = isHome => OpenSetterPickerAsync(isHome);
                vm.TriggerEndSetDialog        = side   => ShowEndSetConfirmAsync(side);
            }
        }

        private void OnScoutCodeBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is not TextBox box) return;

            if (e.Key is Key.LeftCtrl or Key.RightCtrl or
                         Key.LeftShift or Key.RightShift or
                         Key.LeftAlt or Key.RightAlt or
                         Key.LWin or Key.RWin or
                         Key.Tab or Key.Escape or Key.Back or Key.Delete or
                         Key.Left or Key.Right or Key.Up or Key.Down or
                         Key.Home or Key.End or Key.Enter or Key.Return)
                return;

            var parts = new List<string>();
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))     parts.Add("Alt");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))   parts.Add("Shift");
            parts.Add(e.Key.ToString());
            var gesture = string.Join("+", parts);

            _shortcuts ??= _shortcutsStore.Load();
            var match = _shortcuts.FirstOrDefault(s => s.KeyGesture == gesture);

            string? insertText = null;
            if (match != null)
            {
                insertText = match.InsertText;
            }
            else if (e.KeySymbol is { Length: 1 } sym && !char.IsControl(sym[0]))
            {
                insertText = char.ToUpperInvariant(sym[0]).ToString();
            }

            if (insertText == null) return;

            var text     = box.Text ?? string.Empty;
            int selStart = Math.Min(box.SelectionStart, box.SelectionEnd);
            int selEnd   = Math.Max(box.SelectionStart, box.SelectionEnd);
            int selLen   = selEnd - selStart;

            box.Text       = text.Remove(selStart, selLen).Insert(selStart, insertText);
            box.CaretIndex = selStart + insertText.Length;
            e.Handled      = true;
        }

        private MatchDetailViewModel? ViewModel => DataContext as MatchDetailViewModel;
        private Window? GetParentWindow() => TopLevel.GetTopLevel(this) as Window;

        // ── Scout code editing ────────────────────────────────────────────────

        private void CodesListOnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) OpenEditDialog();
        }

        private async void OpenEditDialog()
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null || ViewModel?.SelectedCode == null) return;

            var selected = ViewModel.SelectedCode;
            if (selected.Code is not BallContactCode ballContact) return;

            var vm     = new EditBallContactViewModel(ballContact, ViewModel.HomePlayers, ViewModel.AwayPlayers);
            var dialog = new EditBallContactWindow(vm);
            var result = await dialog.ShowDialog<string?>(parentWindow);

            if (result != null)
                selected.RawCode = result;
        }

        // ── Match info editing ────────────────────────────────────────────────

        private async void EditInfoButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null || ViewModel?.MatchInfo == null) return;

            var dialog = new EditMatchInfoWindow(ViewModel.MatchInfo, ViewModel.MatchMoreInfo);
            var saved  = await dialog.ShowDialog<bool?>(parentWindow);

            if (saved == true)
                ViewModel.RefreshMatchInfoDisplay();
        }

        // ── Substitution dialog ───────────────────────────────────────────────

        private async Task OpenSubstitutionAsync(bool isHome)
        {
            var parentWindow = GetParentWindow();
            var vm = ViewModel;
            if (parentWindow == null || vm == null) return;

            var players        = isHome ? vm.HomePlayers : vm.AwayPlayers;
            var teamLabel      = isHome ? vm.HomeTeamName : vm.AwayTeamName;
            var courtPositions = isHome
                ? new[] { vm.HomePos1, vm.HomePos2, vm.HomePos3, vm.HomePos4, vm.HomePos5, vm.HomePos6 }
                : new[] { vm.AwayPos1, vm.AwayPos2, vm.AwayPos3, vm.AwayPos4, vm.AwayPos5, vm.AwayPos6 };

            var subVm  = new SubstitutionViewModel(teamLabel, courtPositions, players);
            var dialog = new SubstitutionWindow(subVm);

            var result = await dialog.ShowDialog<(string outJersey, string inJersey)?>(parentWindow);
            if (result.HasValue)
                await vm.AddSubstitutionCodeAsync(isHome, result.Value.outJersey, result.Value.inJersey);
        }

        private async Task<bool> ShowEndSetConfirmAsync(VolleyStats.Enums.TeamSide winner)
        {
            var parentWindow = GetParentWindow();
            var vm = ViewModel;
            if (parentWindow == null || vm == null) return false;

            var dialog = new EndSetConfirmWindow(
                vm.SetManager.CurrentSet,
                vm.HomeTeamName,
                vm.AwayTeamName,
                vm.SetManager.LiveHomeScore,
                vm.SetManager.LiveAwayScore);

            var result = await dialog.ShowDialog<bool?>(parentWindow);
            return result == true;
        }

        private async Task OpenSetterPickerAsync(bool isHome)
        {
            var parentWindow = GetParentWindow();
            var vm = ViewModel;
            if (parentWindow == null || vm == null) return;

            var players    = isHome ? vm.HomePlayers : vm.AwayPlayers;
            var teamLabel  = isHome ? vm.HomeTeamName : vm.AwayTeamName;

            // Court positions AFTER the substitution has already been applied
            var courtPositions = isHome
                ? new[] { vm.HomePos1, vm.HomePos2, vm.HomePos3, vm.HomePos4, vm.HomePos5, vm.HomePos6 }
                : new[] { vm.AwayPos1, vm.AwayPos2, vm.AwayPos3, vm.AwayPos4, vm.AwayPos5, vm.AwayPos6 };

            var pickerVm = new SetterPickerViewModel(teamLabel, courtPositions, players);
            var dialog   = new SetterPickerWindow(pickerVm);

            var selectedJersey = await dialog.ShowDialog<string?>(parentWindow);
            if (selectedJersey != null)
            {
                var side = isHome ? VolleyStats.Enums.TeamSide.Home : VolleyStats.Enums.TeamSide.Away;
                int zone = vm.SetManager.FindZone(side, selectedJersey);
                if (zone > 0)
                    vm.SetManager.SetSetterZone(side, zone);
            }
        }
    }
}
