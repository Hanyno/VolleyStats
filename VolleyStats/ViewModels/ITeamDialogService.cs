using System.Threading.Tasks;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public interface ITeamDialogService
    {
        Task<TeamDialogResult?> ShowTeamDialogAsync(Team team);
    }
}
