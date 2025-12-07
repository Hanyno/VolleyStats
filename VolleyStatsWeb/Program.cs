using System.IO;
using VolleyStatsWeb.Data;
using VolleyStatsWeb.Html;
using VolleyStatsWeb.Service;

namespace VolleyStatsWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var templatePath = Path.Combine(
                builder.Environment.ContentRootPath,
                "Html",
                "PlayerPageTemplate.html");

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var dbPath = Path.Combine(desktop, "official_stats.db");
            var connectionString = $"Data Source={dbPath}";

            builder.Services.AddSingleton<IPlayerRepository>(
                _ => new PlayerRepository(connectionString));

            builder.Services.AddSingleton<IPlayerStatisticsService, PlayerStatisticsService>();

            var app = builder.Build();

            app.UseStaticFiles();

            app.MapGet("/", () => "Player statistics web is running!");

            app.MapGet("/player/{id:int}", (int id, IPlayerStatisticsService svc) =>
            {
                var profile = svc.GetPlayerProfile(id);

                if (profile == null)
                {
                    return Results.Text("<h1>Player not found</h1>", "text/html");
                }

                var html = HtmlRenderer.RenderPlayerPage(templatePath, profile);

                return Results.Text(html, "text/html");
            });

            app.Run();
        }
    }
}
