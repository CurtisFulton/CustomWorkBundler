using CustomWorkBundler.UI.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace CustomWorkBundler.UI
{
    /// <summary>
    /// Interaction logic for FolderPicker.xaml
    /// </summary>
    public partial class FolderPicker : UserControl
    {
        public FolderPicker()
        {
            InitializeComponent();
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog {
                Title = "Select Root Directory",
                AllowNonFileSystemItems = false,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                IsFolderPicker = true,
                EnsurePathExists = true,
                EnsureFileExists = true,
                ShowPlacesList = true,
                InitialDirectory = Path.Text
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                Path.Text = dialog.FileName;

                // Update the viewmodel
                var viewModel = (FolderPickerViewModel)DataContext;
                viewModel.FolderPath = Path.Text;
            }
        }
    }

}
