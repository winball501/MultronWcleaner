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
           
            LaunchedFromStartup = e.Args.Any(arg => arg.Equals("-startup", StringComparison.OrdinalIgnoreCase));
 
            string appDirectory = AppContext.BaseDirectory;
            Environment.CurrentDirectory = appDirectory;
 
             
            base.OnStartup(e);
        }
    }

}
