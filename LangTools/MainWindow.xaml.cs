using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LangTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Logger.Write("Main has started.", Severity.DEBUG);
        }

        private void FileExit_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Shows modal window to manage languages.
        /// </summary>
        private void LanguagesManage_click(object sender, RoutedEventArgs e)
        {
            
            // TODO: 
            LangWindow dialog = new LangWindow();
            // Ensure the alt+tab is working properly
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void HelpAbout_click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format("{0}: {1}",
                App.Current.Properties["appName"],
                CoreAssembly.Version), "About");
        }
    }
}
