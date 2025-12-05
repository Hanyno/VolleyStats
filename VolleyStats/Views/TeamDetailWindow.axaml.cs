using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VolleyStats.Domain;

namespace VolleyStats.Views
{
    public partial class TeamDetailWindow : Window
    {
        private readonly Team _team = new();
        public ObservableCollection<Player> Players { get; } = [];


        public TeamDetailWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public TeamDetailWindow(Team team)
        {
            InitializeComponent();

            _team = team;

            TeamCodeTextBox.Text = team.TeamCode;
            NameTextBox.Text = team.Name;
            CoachTextBox.Text = team.CoachName;
            AbbreviationTextBox.Text = team.Abbreviation;
            AssistantCoachTextBox.Text = team.AssistantCoachName;

            if (team.Name == string.Empty && team.TeamCode == string.Empty && team.CoachName == string.Empty)
            {
                Title = "New Team";
            }
            else
            {
                Title = $"Edit Team - {team.Name}";
            }

            foreach (var p in team.Players)
                Players.Add(ClonePlayer(p));

            DataContext = this;
        }

        private static Player ClonePlayer(Player p)
        {
            return new Player
            {
                Id = p.Id,
                TeamId = p.TeamId,
                JerseyNumber = p.JerseyNumber,
                ExternalPlayerId = p.ExternalPlayerId,
                LastName = p.LastName,
                FirstName = p.FirstName,
                BirthDate = p.BirthDate,
                HeightCm = p.HeightCm,
                Position = p.Position,
                PlayerRole = p.PlayerRole,
                NickName = p.NickName,
                IsForeign = p.IsForeign,
                TransferredOut = p.TransferredOut
            };
        }

        private void AddPlayer_OnClick(object? sender, RoutedEventArgs e)
        {
            var newPlayer = new Player
            {
                TeamId = _team.Id,
                JerseyNumber = 0,
                LastName = "",
                FirstName = ""
            };

            Players.Add(newPlayer);
        }

        private void RemovePlayer_OnClick(object? sender, RoutedEventArgs e)
        {
            if (PlayersGrid.SelectedItem is Player p)
            {
                Players.Remove(p);
            }
        }

        private void OkButton_OnClick(object? sender, RoutedEventArgs e)
        {
            _team.TeamCode = TeamCodeTextBox.Text ?? "";
            _team.Name = NameTextBox.Text ?? "";
            _team.CoachName = CoachTextBox.Text ?? "";
            _team.AssistantCoachName = AssistantCoachTextBox.Text ?? "";
            _team.Abbreviation = AbbreviationTextBox.Text ?? "";

            _team.Players = [.. Players];

            Close(true);
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void ExportButton_OnClick(object? sender, RoutedEventArgs e)
        {
            // TODO: Pøidat export týmu do .sq souboru
        }
    }
}