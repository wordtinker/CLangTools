using System;
using System.Configuration;
using System.Windows;
using System.IO;
using LangTools.DataAccess;

namespace LangTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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

            // Create DB file
            string dbFileName = Path.Combine(appDir, "lt.db");
            if (!File.Exists(dbFileName))
            {
                try
                {
                    System.Data.SQLite.SQLiteConnection.CreateFile(dbFileName);
                }
                catch (Exception err)
                {
                    MessageBox.Show(string.Format("Can't create DB file.\nError:{0}", err.ToString()));
                    return;
                }
            }


            // Configure logging
            string logFileName = Path.Combine(appDir, "log.txt");
            Logger.ConfigFile = logFileName;
            
            Logger.Write("----The new session has started.----", Severity.DEBUG);
            // Establish DB Connection
            Storage storage = new Storage(dbFileName);
            Current.Properties["storage"] = storage;

            // Start the main window
            MainWindow wnd = new MainWindow();
            wnd.Title = appName;
            wnd.Show();         
        }

        private void HandleException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Write(e.Exception.ToString());
            ReleaseStorage();
        }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            ReleaseStorage();
        }

        private void ReleaseStorage()
        {
            ((Storage)Current.Properties["storage"]).Close();
        }
    }
}
