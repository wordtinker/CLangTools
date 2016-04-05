using System;
using System.Configuration;
using System.Windows;
using System.IO;
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
        /// Loads settings from configuaration file.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns></returns>
        private bool LoadConfig(out string applicationName)
        {
            try
            {
                applicationName = ConfigurationManager.AppSettings["appName"];
                string dicDir = ConfigurationManager.AppSettings["dictionaries"];
                string corpusDir = ConfigurationManager.AppSettings["corpus"];
                string outputDir = ConfigurationManager.AppSettings["output"];

                if (applicationName == null || applicationName == "" ||
                    dicDir == null || dicDir == "" ||
                    corpusDir == null || corpusDir == "" ||
                    outputDir == null || outputDir == "")
                {
                    return false;
                }
                else
                {
                    // Store app name in global properties
                    Current.Properties["appName"] = applicationName;
                    Current.Properties["dicDir"] = dicDir;
                    Current.Properties["corpusDir"] = corpusDir;
                    Current.Properties["outputDir"] = outputDir;
                    return true;
                }
            }
            catch (ConfigurationErrorsException)
            {
                applicationName = null;
                return false;
            }
        }

        /// <summary>
        /// Prepares environmental settings for app and starts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            // Get app name from config file
            string appName;
            if (!LoadConfig(out appName))
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
            appDir = Path.Combine(appDir, appName);
            Current.Properties["appDir"] = appDir;

            // Create directory if not exist
            DirectoryInfo dir;
            try
            {
                dir = Directory.CreateDirectory(appDir);
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Something bad happened in {0} directory.\nLangTools can't start.\nError:{1}",
                    appDir, err.ToString()));
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
