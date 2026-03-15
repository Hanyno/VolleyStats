using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly Func<Task> _navigateBack;
        private readonly KeyboardShortcutsStore _store;

        public ObservableCollection<KeyBindingViewModel> KeyBindings { get; } = new();

        public bool HasBindings => KeyBindings.Count > 0;

        public IAsyncRelayCommand BackCommand { get; }

        public SettingsViewModel(Func<Task> navigateBack, KeyboardShortcutsStore store)
        {
            _navigateBack = navigateBack;
            _store = store;

            BackCommand = new AsyncRelayCommand(_navigateBack);

            KeyBindings.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBindings));

            LoadBindings();
        }

        private void LoadBindings()
        {
            KeyBindings.Clear();
            foreach (var s in _store.Load())
                KeyBindings.Add(new KeyBindingViewModel(s, RemoveBinding));
        }

        public void AddBinding(KeyboardShortcut shortcut)
        {
            KeyBindings.Add(new KeyBindingViewModel(shortcut, RemoveBinding));
            SaveBindings();
        }

        private void RemoveBinding(KeyBindingViewModel vm)
        {
            KeyBindings.Remove(vm);
            SaveBindings();
        }

        private void SaveBindings()
        {
            _store.Save(KeyBindings.Select(vm => vm.Shortcut));
        }
    }
}
