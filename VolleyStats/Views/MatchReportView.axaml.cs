using Avalonia.Controls;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class MatchReportView : UserControl
    {
        public MatchReportView()
        {
            InitializeComponent();

            DataContextChanged += (_, _) =>
            {
                if (DataContext is MatchReportViewModel vm)
                {
                    vm.RequestSavePath = async options =>
                    {
                        var topLevel = TopLevel.GetTopLevel(this);
                        if (topLevel == null) return null;

                        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
                        return file?.Path.LocalPath;
                    };
                }
            };
        }
    }
}
