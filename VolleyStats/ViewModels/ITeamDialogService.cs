using System.Threading.Tasks;
using VolleyStats.Domain;

namespace VolleyStats.ViewModels
{
    public interface ITeamDialogService
    {
        Task<TeamDialogResult?> ShowTeamDialogAsync(Team team);
    }
}
