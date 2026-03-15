using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public class KeyBindingViewModel : ObservableObject
    {
        public KeyboardShortcut Shortcut { get; }

        public string KeyGesture => Shortcut.KeyGesture;
        public string InsertText => Shortcut.InsertText;

        public IRelayCommand RemoveCommand { get; }

        public KeyBindingViewModel(KeyboardShortcut shortcut, Action<KeyBindingViewModel> onRemove)
        {
            Shortcut = shortcut;
            RemoveCommand = new RelayCommand(() => onRemove(this));
        }
    }
}
