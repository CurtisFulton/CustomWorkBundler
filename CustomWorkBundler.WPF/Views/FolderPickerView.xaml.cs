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
    /// Interaction logic for FolderPickerView.xaml
    /// </summary>
    public partial class FolderPickerView : UserControl
    {
        public static readonly DependencyProperty LabelStringProperty = DependencyProperty.Register(nameof(LabelString), typeof(string), typeof(BundleConnectionDetailsView), new UIPropertyMetadata(""));

        public string LabelString {
            get { return (string)GetValue(LabelStringProperty); }
            set { SetValue(LabelStringProperty, value); }
        }
        
        public FolderPickerView()
        {
            InitializeComponent();

            this.Loaded += (sender, args) => {
                this.LabelBox.Text = this.LabelString;
            };
        }
    }
}
