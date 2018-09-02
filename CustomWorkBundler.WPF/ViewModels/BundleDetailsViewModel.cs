using CustomWorkBundler.WPF.Commands;
using CustomWorkBundler.WPF.Extensions;
using CustomWorkBundler.WPF.Properties;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CustomWorkBundler.Logic.Events;
using CustomWorkBundler.Logic;
using System.Configuration;
using System.Windows;
using System.ComponentModel;

namespace CustomWorkBundler.WPF.ViewModels
{
    public class BundleDetailsViewModel : BaseViewModel
    {
        public bool IsBundling { get; private set; }
        public bool ValidBundle {
            get {
                if (this.IsBundling)
                    return false;

                if (this.CompanyName.IsEmpty() || this.CustomisationName.IsEmpty())
                    return false;

                // Either both databases have to be null, or both have to have a value to be valid
                if (this.SourceDetails.DatabaseString.HasValue() != this.TargetDetails.DatabaseString.HasValue())
                    return false;

                // If the source has web files, we need a target. (We can have a target without a source though)
                if (this.SourceDetails.WebFilesPath.HasValue() && this.TargetDetails.WebFilesPath.IsEmpty())
                    return false;

                if (this.SourceDetails.DatabaseString.IsEmpty() && this.TargetDetails.DatabaseString.IsEmpty() &&
                    this.SourceDetails.WebFilesPath.IsEmpty() && this.TargetDetails.WebFilesPath.IsEmpty())
                    return false;

                return true;
            }
        }

        //  Bundle Meta Data
        public string CompanyName { get; set; }
        public string[] CompanyNameSuggestions { get; set; }

        public string CustomisationName { get; set; }
        public string[] CustomisationNameSuggestions { get; set; }

        public string Revision { get; set; }

        public double TargetBuild { get; set; }
        public double[] AllBuilds { get; set; }

        public string BundleDescription { get; set; }

        // Custom Output Path
        public FolderPickerViewModel OutputPicker { get; set; } = new FolderPickerViewModel();
        public string OutputPath => this.OutputPicker.Path;
        public List<string> OutputPathSuggestions { set { this.OutputPicker.PathSuggestions = value; } }

        // Souce/Target WebFile and Database details
        public BundleConnectionDetailsViewModel SourceDetails { get; set; } = new BundleConnectionDetailsViewModel();
        public BundleConnectionDetailsViewModel TargetDetails { get; set; } = new BundleConnectionDetailsViewModel();

        private string RootPath => ConfigurationManager.AppSettings["RootStoragePath"];

        // Command to swap the Source and Target details ViewModels
        public ICommand SwapDetailsCommand { get; set; }

        public BundleDetailsViewModel() => Initialize();

        private void Initialize()
        {
            this.PropertyChanged += this.PropertyChangedEvent;
            SourceDetails.PropertyChanged += this.PropertyChangedEvent;
            TargetDetails.PropertyChanged += this.PropertyChangedEvent;

            // Command to swap the source and target details.
            this.SwapDetailsCommand = new RelayCommand(() => {
                var temp = this.SourceDetails;

                this.SourceDetails = this.TargetDetails;
                this.TargetDetails = temp;
            });
            
            // Set up file suggestion
            this.OutputPathSuggestions = Settings.Default.OutputPaths;
            this.SourceDetails.PathSuggestions = Settings.Default.SourcePaths;
            this.TargetDetails.PathSuggestions = Settings.Default.TargetPaths;

            this.CompanyNameSuggestions = General.GetExistingCompanyNames(this.RootPath);

            // TODO: Actually implement this (Talk to Buzz about automating it)
            this.AllBuilds = new double[] { 74, 73 };
            this.TargetBuild = this.AllBuilds[0];
        }

        public async Task StartBundleAsync()
        {
            this.UpdateProgress("Saving Settings");
            SavePathsToUserSettings();

            try {
                // Setup package builder
                var packageBuilder = new PackageBuilder(
                        rootPath: this.RootPath,
                        companyName: this.CompanyName,
                        customisationName: this.CustomisationName,
                        buildNumber: this.TargetBuild,
                        revision: this.Revision,
                        bundleDescription: this.BundleDescription
                );

                // Add the Databases to the package builder
                if (this.SourceDetails.DatabaseString.HasValue() && this.TargetDetails.DatabaseString.HasValue()) 
                    packageBuilder.RegisterDatabase(this.SourceDetails.DatabaseString, this.TargetDetails.DatabaseString);
                
                // Add the web files to the package builder
                if (this.TargetDetails.WebFilesPath.HasValue())
                    packageBuilder.RegisterWebFiles(this.SourceDetails.WebFilesPath, this.TargetDetails.WebFilesPath);

                // Start the actual bundle creation
                await packageBuilder.CreateBundleAsync(false, this.OutputPath);

                this.UpdateProgress("Finished Bundling!");
            } catch (Exception ex) {
                this.UpdateProgress("An Error Occured...");
                this.ShowException(ex);
            }
        }

        private void SavePathsToUserSettings()
        {
            // Cache for simplicity
            var outputPaths = Settings.Default.OutputPaths;
            var sourcePaths = Settings.Default.SourcePaths;
            var targetPaths = Settings.Default.TargetPaths;

            // Check the path exists, and has not already been added
            if (Directory.Exists(this.OutputPath) && !outputPaths.Contains(this.OutputPath))
                outputPaths.Add(this.OutputPath);

            if (Directory.Exists(this.OutputPath) && !sourcePaths.Contains(this.SourceDetails.CustomPath))
                sourcePaths.Add(this.SourceDetails.CustomPath);

            if (Directory.Exists(this.OutputPath) && !targetPaths.Contains(this.TargetDetails.CustomPath))
                targetPaths.Add(this.TargetDetails.CustomPath);
            
            // Force update suggestions
            this.OutputPathSuggestions = outputPaths;
            this.SourceDetails.PathSuggestions = sourcePaths;
            this.TargetDetails.PathSuggestions = targetPaths;
        }

        private void UpdateProgress(string message) => new ProgressChangedEvent(message).Raise(this);

        private void ShowException(Exception ex)
        {
            // Format the message so the Message is at the top
            var errorMessage = ex.Message;
            // Add Exception type after 2 new lines
            errorMessage += $". {Environment.NewLine + Environment.NewLine + ex.GetType()}.";
            // Add the stack trace.
            errorMessage += Environment.NewLine + ex.StackTrace.ToString();

            MessageBox.Show(errorMessage, "An Error Occured", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PropertyChangedEvent(object sender, PropertyChangedEventArgs args)
        {
            // If anything else other than ValidBundle changes, then update ValidBundle
            if (args.PropertyName != nameof(this.ValidBundle))
                this.OnPropertyChanged(nameof(this.ValidBundle));

            if (args.PropertyName != nameof(this.CompanyName) && args.PropertyName != nameof(this.CustomisationName))
                return;

            if (args.PropertyName == nameof(this.CompanyName) && this.CompanyName.HasValue()) {
                // Need to store this because it gets set to null when the CustomisationNameSuggestions changes
                var previousName = this.CustomisationName;

                this.CustomisationNameSuggestions = General.GetExistingCustomisations(this.RootPath, this.CompanyName);
                this.CustomisationName = previousName;
            }

            // Set the previous bundle WebFiles and Databases for the drop downs.
            var bundlePath = General.GetBundlePath(this.RootPath, this.CompanyName, this.CustomisationName);

            if (this.CompanyName.IsEmpty() || this.CustomisationName.IsEmpty() || bundlePath.IsEmpty() || !Directory.Exists(bundlePath)) {
                this.SetPreviousBundleValues(null, null);
                return;
            }

            var previousBundleWebFiles = General.GetPreviousWebFiles(bundlePath);
            var previousBundleSnapshots = General.GetPreviousDatabases(bundlePath);

            this.SetPreviousBundleValues(previousBundleWebFiles, previousBundleSnapshots);
        }
        
        private void SetPreviousBundleValues(DirectoryInfo[] previousWebFiles, FileInfo[] previousDatabases)
        {
            this.SourceDetails.PreviousWebFiles = previousWebFiles;
            this.TargetDetails.PreviousWebFiles = previousWebFiles;
            
            this.SourceDetails.PreviousDatabases = previousDatabases;
            this.TargetDetails.PreviousDatabases = previousDatabases;
        }
    }
}