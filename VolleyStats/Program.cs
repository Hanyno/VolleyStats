using Avalonia;
using System;
using System.Text;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using VolleyStats.Data;

namespace VolleyStats
{
    internal sealed class Program
    {
        internal static LibVLC? LibVlc { get; private set; }
        internal static Task LibVlcInitTask { get; private set; } = null!;

        [STAThread]
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            TeamsDatabaseInitializer.EnsureCreated();

            LibVlcInitTask = Task.Run(() =>
            {
                try
                {
                    Core.Initialize();
                    LibVlc = new LibVLC(enableDebugLogs: false, "--quiet", "--verbose=-1");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VLC] Init failed: {ex.Message}");
                }
            });

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
