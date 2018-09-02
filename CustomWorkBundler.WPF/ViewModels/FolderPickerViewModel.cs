using CustomWorkBundler.WPF.Commands;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace CustomWorkBundler.WPF.ViewModels
{
    public class FolderPickerViewModel : BaseViewModel
    {
        public string Path { get; set; }
        public List<string> PathSuggestions { get; set; }

        public ICommand BrowserFoldersCommand { get; set; }

        public FolderPickerViewModel()
        {
            this.BrowserFoldersCommand = new RelayCommand(this.OpenFolderBrowserDialog);
            this.PathSuggestions = new List<string>();
        }

        private void OpenFolderBrowserDialog()
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
                InitialDirectory = this.Path
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                this.Path = dialog.FileName;
            }
        }
    }
}