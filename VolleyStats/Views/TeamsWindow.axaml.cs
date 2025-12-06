using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using VolleyStats.Domain;
using VolleyStats.Services;

namespace VolleyStats.Views
{
    public partial class TeamsWindow : Window
    {
        private bool _isDialogOpen = false;

        private readonly ITeamsService _teamsService;
        private readonly IOfficialStatsService _officialStatsService;
        public ObservableCollection<Team> Teams { get; } = new();

        public TeamsWindow(ITeamsService teamsService, IOfficialStatsService officialStatsService)
        {
            _teamsService = teamsService;
            _officialStatsService = officialStatsService;
            InitializeComponent();
            LoadTeams();
        }

        private void LoadTeams()
        {
            Teams.Clear();
            try
            {
                foreach (var t in _teamsService.GetAllTeamsWithPlayers())
                {
                    Teams.Add(t);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading teams: " + ex);
            }

            TeamsList.ItemsSource = Teams;
        }

        private async void NewButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_isDialogOpen)
                return;

            _isDialogOpen = true;

            var newTeam = new Team();

            var detailWindow = new TeamDetailWindow(newTeam);
            var result = await detailWindow.ShowDialog<bool?>(this);

            _isDialogOpen = false;

            TeamsList.SelectedItem = null;

            if (result == true)
            {
                // Uložíme nový tým pøes service vrstvu
                _teamsService.SaveTeam(newTeam);
                LoadTeams();
            }
        }

        private async void ImportButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import týmu z .sq souboru",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("SQ soubor")
                    {
                        Patterns = ["*.sq"]
                    }
                ]
            });

            if (files == null || files.Count == 0)
                return;

            var file = files[0];

            try
            {
                using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream, Encoding.GetEncoding(1250));

                var content = await reader.ReadToEndAsync();
                var lines = content.Split(Environment.NewLine);

                if (!TryParseTeamFromSq(lines, out var team, out var errorMessage))
                {
                    // TODO: zobrazit chybu uživateli
                    Console.WriteLine($"Chyba parsování: {errorMessage}");
                    return;
                }

                // Uložení týmu do DB pøes service
                _teamsService.SaveTeam(team);

                LoadTeams();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba pøi importu: {ex}");
            }
        }

        private void ExportAllButton_OnClick(object? sender, RoutedEventArgs e)
        {
            // TODO: naèíst všechny týmy z _teamsService a vyexportovat
        }

        private async void TeamsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isDialogOpen)
                return;

            if (TeamsList.SelectedItem is not Team selectedTeam)
                return;

            _isDialogOpen = true;

            var detailWindow = new TeamDetailWindow(selectedTeam);
            var result = await detailWindow.ShowDialog<bool?>(this);

            _isDialogOpen = false;

            TeamsList.SelectedItem = null;

            if (result == true)
            {
                // Uložit zmìny
                _teamsService.SaveTeam(selectedTeam);
                LoadTeams();
            }
            else if (result == false)
            {
                // Smazat tým
                _teamsService.DeleteTeam(selectedTeam.Id);
                LoadTeams();
            }
        }

        /// <summary>
        /// Naparsuje tým z .sq souboru.
        /// lines = celý obsah souboru po øádcích.
        /// </summary>
        private bool TryParseTeamFromSq(
            string[] lines,
            out Team team,
            out string errorMessage)
        {
            team = null!;
            errorMessage = string.Empty;

            try
            {
                if (lines == null || lines.Length < 2)
                {
                    errorMessage = "Soubor neobsahuje dostatek øádkù (chybí hlavièka týmu).";
                    return false;
                }
                var teamLine = lines[1];
                var teamParts = teamLine.Split('\t');

                if (teamParts.Length < 5)
                {
                    errorMessage = "Øádek s týmem nemá oèekávaný formát (ménì než 5 sloupcù).";
                    return false;
                }

                team = new Team
                {
                    TeamCode = teamParts[0].Trim(),
                    Name = teamParts[1].Trim(),
                    CoachName = teamParts[2].Trim(),
                    AssistantCoachName = teamParts[3].Trim(),
                    Abbreviation = teamParts[4].Trim()
                };

                if (team.Players == null)
                    team.Players = new List<Player>();

                for (int i = 2; i < lines.Length; i++)
                {
                    var line = lines[i];

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split('\t');

                    if (parts.Length < 5)
                        continue;

                    var player = new Player();

                    if (int.TryParse(parts[0].Trim(), out var shirtNumber))
                        player.JerseyNumber = shirtNumber;

                    player.ExternalPlayerId = parts[1].Trim();

                    player.LastName = parts[2].Trim();

                    var birthRaw = parts[3].Trim();
                    if (DateTime.TryParseExact(
                            birthRaw,
                            "dd/MM/yyyy",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var birthDate))
                    {
                        player.BirthDate = birthDate;
                    }

                    if (int.TryParse(parts[4].Trim(), out var height))
                        player.HeightCm = height;

                    player.PlayerRole = parts[6].Trim();

                    if (parts.Length > 8)
                        player.FirstName = parts[8].Trim();

                    if (parts.Length > 9 && int.TryParse(parts[9].Trim(), out var positionValue))
                    {
                        player.Position = (Enums.PlayerPost)positionValue;
                    }

                    if (parts.Length > 10)
                        player.NickName = parts[10].Trim();

                    if (parts.Length > 11)
                        player.IsForeign = ParseNullableBool(parts[11]);

                    if (parts.Length > 12)
                        player.TransferredOut = ParseNullableBool(parts[12]);

                    team.Players.Add(player);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Výjimka pøi parsování: {ex.Message}";
                team = null!;
                return false;
            }
        }

        private bool? ParseNullableBool(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim().ToLowerInvariant();

            return raw switch
            {
                "1" or "true" or "t" or "yes" or "y" => true,
                "0" or "false" or "f" or "no" or "n" => false,
                _ => null
            };
        }

        private void UploadButton_OnClick(object? sender, RoutedEventArgs e)
        {
            _officialStatsService.UploadTeams(Teams);
        }
    }
}
