using CustomWorkBundler.WPF.Properties;
using CustomWorkBundler.WPF.ViewModels;
using System.Windows;

namespace CustomWorkBundler.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Initialize the viewmodel. This is the root of all ViewModels
            this.DataContext = new MainWindowViewModel();

            InitializeComponent();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save any changes that were made to the settings
            Settings.Default.Save();
        }
    }
}
