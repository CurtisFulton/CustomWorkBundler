using CustomWorkBundler.Logic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace CustomWorkBundler.WPF.ViewModels
{
    public class BundleConnectionDetailsViewModel : BaseViewModel
    {
        // Final getters 
        public string WebFilesPath {
            get {
                switch (this.WebFilesSelectionType) {
                    case SelectionType.Standard:
                        return this.SelectedStandardWebFiles?.FullName;
                    case SelectionType.Previous:
                        return Path.Combine(this.SelectedPreviousWebFiles?.FullName, PackageBuilder.WebFilesRelease);
                    case SelectionType.Custom:
                        return this.CustomPath;
                    case SelectionType.None:
                        return string.Empty;
                    default:
                        throw new InvalidOperationException("Error getting Web Files Path");
                }
            }
        }
        public string DatabaseString {
            get {
                switch (this.DatabaseSelectionType) {
                    case SelectionType.Standard:
                        return this.SelectedStandardDatabase?.FullName;
                    case SelectionType.Previous:
                        return this.SelectedPreviousDatabase?.FullName;
                    case SelectionType.Custom:
                        return this.ConnectionStringDetails.ConnectionString;
                    case SelectionType.None:
                        return string.Empty;
                    default:
                        throw new InvalidOperationException("Error getting Web Files Path");
                }
            }
        }

        #region Web Files Details

        // Selection Type. Determines which of the following variables to use for the final web files path
        public SelectionType WebFilesSelectionType { get; set; } = SelectionType.Standard;

        // Standard Web fIles
        public int SelectedStandardWebFilesIndex { get; set; }
        public DirectoryInfo[] StandardWebFiles { get; set; }
        public DirectoryInfo SelectedStandardWebFiles {
            get {
                if (this.StandardWebFiles == null || this.StandardWebFiles.Length == 0)
                    return null;
                return this.StandardWebFiles[this.SelectedStandardWebFilesIndex];
            }
        }

        // Previous web Files 
        public int SelectedPreviousWebFilesIndex { get; set; }
        public DirectoryInfo[] PreviousWebFiles { get; set; }
        public DirectoryInfo SelectedPreviousWebFiles {
            get {
                if (this.PreviousWebFiles == null || this.PreviousWebFiles.Length == 0)
                    return null;
                return this.PreviousWebFiles[this.SelectedPreviousWebFilesIndex];
            }
        }

        // Folder Select Path
        public FolderPickerViewModel FolderSelector { get; set; } = new FolderPickerViewModel();
        public string CustomPath => this.FolderSelector.Path;
        public List<string> PathSuggestions { set { this.FolderSelector.PathSuggestions = value; } }

        #endregion

        #region Database Details 

        // Selection Type. Determines which of the following variables to use for the database string
        public SelectionType DatabaseSelectionType { get; set; } = SelectionType.Custom;

        // Standard Database
        public int SelectedStandardDatabaseIndex { get; set; }
        public FileInfo[] StandardDatabases { get; set; }
        public FileInfo SelectedStandardDatabase {
            get {
                if (this.StandardDatabases == null || this.StandardDatabases.Length == 0)
                    return null;
                return this.StandardDatabases[this.SelectedStandardDatabaseIndex];
            }
        }

        // Previous Database
        public int SelectedPreviousDatabaseIndex { get; set; }
        public FileInfo[] PreviousDatabases { get; set; }
        public FileInfo SelectedPreviousDatabase {
            get {
                if (this.PreviousDatabases == null || this.PreviousDatabases.Length == 0)
                    return null;
                return this.PreviousDatabases[this.SelectedPreviousDatabaseIndex];
            }
        }

        // Connection String Database
        public ConnectionStringViewModel ConnectionStringDetails { get; set; } = new ConnectionStringViewModel();

        #endregion

        public BundleConnectionDetailsViewModel()
        {
            this.Initialize();
            this.ConnectionStringDetails.PropertyChanged += this.PropertyChangedEvent;
        }

        private void Initialize()
        {
            var rootDir = new DirectoryInfo(@"D:\Programming\C#\CustomWorkBundler - Updated\Bundler Testing");

            this.PropertyChanged += this.PropertyChangedEvent;
            this.FolderSelector.PropertyChanged += this.PropertyChangedEvent;
        }

        private void PropertyChangedEvent(object sender, PropertyChangedEventArgs args)
        {
            // Propagate changes up
            if (args.PropertyName == nameof(this.ConnectionStringDetails.ConnectionString))
                this.OnPropertyChanged(nameof(this.ConnectionStringDetails));

            if (args.PropertyName == nameof(this.FolderSelector.Path))
                this.OnPropertyChanged(this.WebFilesPath);

            // Only care about when the PreviousWebfiles or PreviousDatabases change
            if (args.PropertyName != nameof(this.PreviousWebFiles) && args.PropertyName != nameof(this.PreviousDatabases))
                return;

            if (this.SelectedPreviousWebFiles == null || this.PreviousWebFiles == null || !this.PreviousWebFiles.Contains(this.SelectedPreviousWebFiles)) {
                this.SelectedPreviousWebFilesIndex = 0;
            }

            if (this.SelectedPreviousDatabase == null || this.PreviousDatabases == null || !this.PreviousDatabases.Contains(this.SelectedPreviousDatabase)) {
                this.SelectedPreviousDatabaseIndex = 0;
            }
        }
    }

    public enum SelectionType
    {
        Standard,
        Previous,
        Custom,
        None
    }
}