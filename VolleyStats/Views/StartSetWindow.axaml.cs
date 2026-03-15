using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using VolleyStats.Models;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class StartSetWindow : Window
    {
        private TextBox[]? _enterOrder;

        public StartSetWindow()
        {
            InitializeComponent();
        }

        public StartSetWindow(StartSetViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            AddHandler(TextInputEvent, FilterNumericInput, RoutingStrategies.Tunnel);
            AddHandler(KeyDownEvent, OnEnterKeyDown, RoutingStrategies.Tunnel);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);
        }

        private TextBox[] GetEnterOrder() => _enterOrder ??= new[]
        {
            TopP1Box, TopP2Box, TopP3Box, TopP4Box, TopP5Box, TopP6Box, TopSetterBox,
            BottomP1Box, BottomP2Box, BottomP3Box, BottomP4Box, BottomP5Box, BottomP6Box, BottomSetterBox
        };

        // ── Numeric filter ──────────────────────────────────────────────────

        private void FilterNumericInput(object? sender, TextInputEventArgs e)
        {
            if (e.Text != null && !e.Text.All(char.IsDigit))
                e.Handled = true;
        }

        // ── Enter key navigation ────────────────────────────────────────────

        private void OnEnterKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Return) return;

            var order = GetEnterOrder();
            var idx = Array.FindIndex(order, tb => tb.IsFocused);
            if (idx < 0) return;

            if (idx < order.Length - 1)
            {
                order[idx + 1].Focus();
                order[idx + 1].SelectAll();
            }
            e.Handled = true;
        }

        // ── Drag & Drop ─────────────────────────────────────────────────────

        private async void OnPlayerPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
            if (sender is not Control c || c.DataContext is not MatchPlayer player) return;

            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(player.JerseyNumber.ToString()));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy);
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            // Only allow drop on court textboxes
            var target = (e.Source as Visual)?.FindAncestorOfType<TextBox>();
            var hasText = e.DataTransfer.Items.Any(i => i.Formats.Contains(DataFormat.Text));
            if (target != null && hasText &&
                (target.Classes.Contains("court-input") || target.Classes.Contains("setter-input")))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            var textItem = e.DataTransfer.Items.FirstOrDefault(i => i.Formats.Contains(DataFormat.Text));
            if (textItem?.TryGetRaw(DataFormat.Text) is not string jersey) return;

            var target = (e.Source as Visual)?.FindAncestorOfType<TextBox>();
            if (target != null &&
                (target.Classes.Contains("court-input") || target.Classes.Contains("setter-input")))
            {
                target.Text = jersey;
            }
        }

        // ── Dialog buttons ──────────────────────────────────────────────────

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
            => Close(null);

        private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is StartSetViewModel vm && vm.CanConfirm)
                Close(true);
            else
                Close(null);
        }
    }
}
