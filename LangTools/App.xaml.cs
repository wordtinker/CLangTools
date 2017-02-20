using System;
using System.Windows;
using LangTools.Shared;
using LangTools.ViewModels;

namespace LangTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Prepares environmental settings for app and starts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            // Get app name from config file
            string appName = Tools.ReadSetting("appName");
            if (string.IsNullOrWhiteSpace(appName))
            {
                MessageBox.Show("Error reading app settings.\nLangTools can't start.");
                return;
            }

            // Get app directory
            string appDir;
            try
            {
                appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            catch (PlatformNotSupportedException)
            {
                MessageBox.Show("Platform is not supported.\nLangTools can't start.");
                return;
            }
            appDir = IOTools.CombinePath(appDir, appName);
            // Save application directory path for later
            Current.Properties["appDir"] = appDir;

            // Create directory if not exist
            if (!IOTools.CreateDirectory(appDir))
            {
                MessageBox.Show(string.Format("Something bad happened in {0} directory.\nLangTools can't start.", appDir));
                return;
            }

            // Make sure VM is ready to start.
            if (!VMBoot.IsReadyToLoad(appDir))
            {
                MessageBox.Show(string.Format("Can't start the app."));
                return;
            }

            Log.Logger.Debug("----The new session has started.----");

            // Start the main window
            MainWindow wnd = new MainWindow();
            wnd.Title = appName;
            wnd.Show();   
        }

        /// <summary>
        /// General exception cather. Logs exceptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Logger.Error(e.Exception.ToString());
        }
    }
}
