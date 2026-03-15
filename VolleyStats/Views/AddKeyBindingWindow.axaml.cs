using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using VolleyStats.Models;

namespace VolleyStats.Views
{
    public partial class AddKeyBindingWindow : Window
    {
        private string? _capturedGesture;

        public AddKeyBindingWindow()
        {
            InitializeComponent();
            KeyCaptureBox.AddHandler(KeyDownEvent, OnKeyCaptureKeyDown, RoutingStrategies.Tunnel);
        }

        private void OnKeyCaptureKeyDown(object? sender, KeyEventArgs e)
        {
            // Ignore standalone modifier keys
            if (e.Key is Key.LeftCtrl or Key.RightCtrl or
                         Key.LeftShift or Key.RightShift or
                         Key.LeftAlt or Key.RightAlt or
                         Key.LWin or Key.RWin or
                         Key.Tab or Key.Escape)
                return;

            var parts = new List<string>();
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
            parts.Add(e.Key.ToString());

            _capturedGesture = string.Join("+", parts);
            KeyCaptureBox.Text = _capturedGesture;
            e.Handled = true;
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(null);

        private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_capturedGesture)) return;
            var insertText = InsertTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(insertText)) return;
            Close(new KeyboardShortcut { KeyGesture = _capturedGesture, InsertText = insertText });
        }
    }
}
