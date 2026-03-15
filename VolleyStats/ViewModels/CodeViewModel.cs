using CommunityToolkit.Mvvm.ComponentModel;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public class CodeViewModel : ObservableObject
    {
        public Code Code { get; }

        private string _rawCode;
        public string RawCode
        {
            get => _rawCode;
            set => SetProperty(ref _rawCode, value);
        }

        public CodeViewModel(Code code)
        {
            Code = code;
            _rawCode = code.RawCode;
        }
    }
}
