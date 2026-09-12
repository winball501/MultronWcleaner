using System.Configuration;
using System.Data;
using System.Windows;

namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool LaunchedFromStartup { get; set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1) Argümanı kontrol et
            LaunchedFromStartup = e.Args.Any(arg => arg.Equals("-startup", StringComparison.OrdinalIgnoreCase));

            // 2) Çalışma dizinini ayarla
            string appDirectory = AppContext.BaseDirectory;
            Environment.CurrentDirectory = appDirectory;

            // 3) Debug log yaz
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MultronWCleaner", "startup_debug.log");

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.AppendAllText(logPath,
                    $"{DateTime.Now}: Args=[{string.Join(",", e.Args)}], LaunchedFromStartup={LaunchedFromStartup}\n");
            }
            catch
            {
                // loglama başarısız olsa da uygulama açılmaya devam etmeli
            }

            // 4) Base çağrısı en son
            base.OnStartup(e);
        }
    }

}
