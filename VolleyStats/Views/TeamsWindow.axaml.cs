using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VolleyStats.Data;
using VolleyStats.Domain;

namespace VolleyStats.Views
{
    public partial class TeamsWindow : Window
    {
        private bool _isDialogOpen = false;

        private readonly TeamsRepository _repository = new();

        public TeamsWindow()
        {
            InitializeComponent();
            LoadTeams();
        }

        private void LoadTeams()
        {
            List<Team> teams;
            try
            {
                teams = _repository.GetAllTeamsWithPlayers();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading teams: " + ex);
                teams = [];
            }

            TeamsList.ItemsSource = teams;
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
                _repository.SaveTeam(newTeam);
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

                // Uložení týmu do DB
                _repository.SaveTeam(team);

                LoadTeams();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba pøi importu: {ex}");
            }
        }



        private void ExportAllButton_OnClick(object? sender, RoutedEventArgs e)
        {
            // TODO: naèíst všechny týmy a vyexportovat
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
                _repository.SaveTeam(selectedTeam);
                LoadTeams();
            }
            else if (result == false)
            {
                _repository.DeleteTeam(selectedTeam.Id);
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

                // 0. øádek ignorujeme
                // 1. øádek = tým: TEAM-CODE \t Jméno_Týmu \t jmeno_coache \t jmeno_asistenta
                //                 \t abbreviation_code \t character_encoding \t jmeno_tymu_v_hexa ...
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

                // pokud má Team kolekci hráèù, ujisti se, že není null
                if (team.Players == null)
                    team.Players = new List<Player>();

                // 2+ øádky = hráèi
                for (int i = 2; i < lines.Length; i++)
                {
                    var line = lines[i];

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split('\t');

                    // bezpeènost – aspoò prvních pár základních sloupcù
                    if (parts.Length < 5)
                        continue; // nebo mùžeš logovat chybu a pokraèovat

                    var player = new Player();

                    // 0: èíslo dresu
                    if (int.TryParse(parts[0].Trim(), out var shirtNumber))
                        player.JerseyNumber = shirtNumber;

                    // 1: id hráèe
                    player.ExternalPlayerId = parts[1].Trim();  // pøizpùsob si názvu property

                    // 2: pøíjmení
                    player.LastName = parts[2].Trim();

                    // 3: datum narození (dd/mm/yyyy)
                    var birthRaw = parts[3].Trim();
                    if (DateTime.TryParseExact(
                            birthRaw,
                            "dd/MM/yyyy",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var birthDate))
                    {
                        // pøizpùsob typu – mùžeš mít DateOnly / DateTime / nullable
                        player.BirthDate = birthDate;
                    }

                    // 4: výška (int)
                    if (int.TryParse(parts[4].Trim(), out var height))
                        player.HeightCm = height;

                    // 5: (prázdné)
                    // 6: char_libero/kapitan (L/C)
                    player.PlayerRole = parts[6].Trim();

                    // 7: (prázdné)
                    // 8: jméno
                    if (parts.Length > 8)
                        player.FirstName = parts[8].Trim();

                    // 9: enum_post (èíslo)
                    if (parts.Length > 9 && int.TryParse(parts[9].Trim(), out var positionValue))
                    {
                        player.Position = (Enums.PlayerPost)positionValue;
                    }

                    // 10: nickname
                    if (parts.Length > 10)
                        player.NickName = parts[10].Trim();

                    // 11: cizinka? (bool / null)
                    if (parts.Length > 11)
                        player.IsForeign = ParseNullableBool(parts[11]);

                    // 12: pøestoupila_pryè? (bool / null)
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
    }
}