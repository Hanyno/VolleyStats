using Avalonia.Controls;
using System;
using VolleyStats.Domain;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class TeamDetailWindow : Window
    {
        public TeamDetailWindow()
        {
            InitializeComponent();
        }

        public TeamDetailWindow(Team team)
        {
            InitializeComponent();

            var vm = new TeamDetailViewModel(team);
            vm.CloseRequested += (_, result) => Close(result);
            DataContext = vm;
        }
    }
}