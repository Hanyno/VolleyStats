using VolleyStats.Domain;

namespace VolleyStats.ViewModels
{
    public enum TeamDialogResultType
    {
        Cancel,
        Save,
        Delete
    }

    public class TeamDialogResult
    {
        public TeamDialogResultType Result { get; init; }
        public Team? Team { get; init; }
    }
}
