using System.Threading.Tasks;

namespace VolleyStats.ViewModels
{
    public interface IFilePickerService
    {
        Task<string?> PickSqFileAsync();
        Task<string?> PickSqSavePathAsync(string defaultFileName);
    }
}
