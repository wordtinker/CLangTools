using LangTools.ViewModels;
using System.Windows.Threading;
using System.Windows;
using System;

namespace LangTools.Views
{
    class BaseWindowService : IUIBaseService
    {
        public void BeginInvoke(Action method)
        {
            App.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send,
                method
                );
        }

        public bool Confirm(string message)
        {
            MessageBoxResult result = MessageBox.Show(
                message,
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }

    class MainWindowService : BaseWindowService, IUIMainWindowService
    {
        private MainWindow mainWindow;

        public MainWindowService(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public string CommonDictionaryName
        {
            get
            {
                return Tools.ReadSetting("CommonDictionaryName");
            }
        }

        public string AppDir
        {
            get
            {
                return (string)App.Current.Properties["appDir"];
            }
        }

        public string AppName
        {
            get
            {
                return Tools.ReadSetting("appName");
            }
        }

        public string CorpusDir
        {
            get
            {
                return Tools.ReadSetting("corpus");
            }
        }

        public string DicDir
        {
            get
            {
                return Tools.ReadSetting("dictionaries");
            }
        }

        public string OutDir
        {
            get
            {
                return Tools.ReadSetting("output");
            }
        }

        public void Shutdown()
        {
            App.Current.Shutdown();
        }
    }
}
