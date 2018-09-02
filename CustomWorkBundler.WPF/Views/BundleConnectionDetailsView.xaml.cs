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

namespace CustomWorkBundler.WPF.Views
{
    /// <summary>
    /// Interaction logic for BundleConnectionDetailsView.xaml
    /// </summary>
    public partial class BundleConnectionDetailsView : UserControl
    {
        public static readonly DependencyProperty ConnectionDetailsHeaderProperty =  DependencyProperty.Register(nameof(ConnectionDetailsHeader), typeof(string), typeof(BundleConnectionDetailsView), new UIPropertyMetadata(""));

        public string ConnectionDetailsHeader {
            get { return (string)GetValue(ConnectionDetailsHeaderProperty); }
            set { SetValue(ConnectionDetailsHeaderProperty, value); }
        }

        public BundleConnectionDetailsView()
        {
            InitializeComponent();

            this.Loaded += (sender, args) => {
                this.GroupBox.Header = this.ConnectionDetailsHeader;
            };
        }
    }
}
