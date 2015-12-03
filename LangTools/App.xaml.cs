using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace LangTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            // Get app name from config file
            string appName;
            try
            {
                appName = ConfigurationManager.AppSettings["appName"];
                if (appName == null)
                {
                    MessageBox.Show("Error reading app settings.\nLangTools can't start.");
                    return;
                }
                else
                {
                    // Store app name in global properties
                    Current.Properties["appName"] = appName;
                }
            }
            catch (ConfigurationErrorsException)
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

            // TODO: Test different platforms
            // Store Directory info in global settings
            // TODO: do we use it?
            Current.Properties["dir"] = dir;

            // Create DB file
            string dbFileName = Path.Combine(appDir, "lt.db");
            if (!File.Exists(dbFileName))
            {
                try
                {
                    File.Create(dbFileName);
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
            // Start the main window
            try
            {
                MainWindow wnd = new MainWindow();
                wnd.Title = appName;
                wnd.Show();
            }
            catch (Exception err)
            {
                Logger.Write(err.ToString());
            }
            
        }
    }
}
