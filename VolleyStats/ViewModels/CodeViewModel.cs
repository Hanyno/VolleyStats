using VolleyStats.Domain;

namespace VolleyStats.ViewModels
{
    public class CodeViewModel
    {
        public Code Code { get; }
        public string Display => Code.RawCode;

        public CodeViewModel(Code code)
        {
            Code = code;
        }
    }
}
