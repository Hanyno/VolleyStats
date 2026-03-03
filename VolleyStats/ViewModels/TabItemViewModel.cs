using System;
using CommunityToolkit.Mvvm.Input;

namespace VolleyStats.ViewModels
{
    public partial class TabItemViewModel : ViewModelBase
    {
        private string _header;
        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        private ViewModelBase _content;
        public ViewModelBase Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public IRelayCommand CloseCommand { get; }

        public TabItemViewModel(string header, ViewModelBase content, Action<TabItemViewModel> closeAction)
        {
            _header = header;
            _content = content;
            CloseCommand = new RelayCommand(() => closeAction(this));
        }
    }
}
