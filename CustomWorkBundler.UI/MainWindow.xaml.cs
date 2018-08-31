using CustomWorkBundler.UI.Properties;
using CustomWorkBundler.UI.ViewModel;
using RedGate.SQLCompare.Engine;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CustomWorkBundler.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
