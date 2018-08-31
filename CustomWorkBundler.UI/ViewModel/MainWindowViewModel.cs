using CustomWorkBundler.UI.Properties;
using PropertyChanged;
using RedGate.SQLCompare.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CustomWorkBundler.UI.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        public ResourceSelectorViewModel SourceSelector { get; set; }
        public ResourceSelectorViewModel TargetSelector { get; set; }

        public FolderPickerViewModel OutputPicker { get; set; }
        
        public bool IsBundling { get; set; }
        public bool CanCreateBundle => !(String.IsNullOrWhiteSpace(CompanyName) || String.IsNullOrWhiteSpace(BundleName) || IsBundling);
        
        public string CompanyName { get; set; }
        public string[] CompanyNameSuggestions { get; set; }

        public string BundleName { get; set; }
        public string[] BundleNameSuggestions { get; set; }

        public double[] AllBuilds { get; set; }
        public int SelectedBuildIndex { get; set; }
        public double SelectedBuild { get { return AllBuilds[SelectedBuildIndex]; } }

        private string BundlePath => Path.Combine(RootPath, CompanyName, BundleName);
        private string FinalBundleName { get; set; }
        public string Revision { get; set; }

        public string Progress { get; set; }
        private void ProgressChangedEvent(string msg) => this.Progress = msg;

        public string BundleDescription { get; set; }

        public RelayCommand SwitchCommand { get; set; }
        public RelayCommandAsync BundleCommand { get; set; }

        private string RootPath => ConfigurationManager.AppSettings["RootStorageDirectory"];

        public MainWindowViewModel()
        {
            SetupPathSuggestions();

            SourceSelector = new ResourceSelectorViewModel("Source");
            TargetSelector = new ResourceSelectorViewModel("Target");
            
            OutputPicker = new FolderPickerViewModel("Output Directory:");

            AllBuilds = GetAllMexBuilds();
            CompanyNameSuggestions = GetCompanyNameSuggestions();

            SourceSelector.FolderPicker.Suggestions = Settings.Default.PreviousSources;
            TargetSelector.FolderPicker.Suggestions = Settings.Default.PreviousTargets;
            OutputPicker.Suggestions = Settings.Default.PreviousOutputs;
            
            this.PropertyChanged += PropertyChangedListener;
            
            // Commands that the buttons bind to
            SwitchCommand = new RelayCommand(SwitchTargetAndSource);
            BundleCommand = new RelayCommandAsync(CreateBundle);
        }

        private void PropertyChangedListener(object sender, PropertyChangedEventArgs args)
        {
            // Only care about BundleName or CompanyName changes
            if (!(args.PropertyName == nameof(this.CompanyName) || args.PropertyName == nameof(this.BundleName)))
                return;

            if (args.PropertyName == nameof(this.CompanyName)) {
                BundleNameSuggestions = GetBundleNameSuggestions();
            }
            
            if (string.IsNullOrWhiteSpace(this.CompanyName) || string.IsNullOrWhiteSpace(this.BundleName)) {
                // If either the company or bundle name are empty, null out the previous web files/bundles
                SetPreviousBundleValues(null, null);
            } else {
                FinalBundleName = $"{BundleName}_Build{SelectedBuild}_{GetRevisionNumber()}";

                // If the directory doesn't exist, null out the previous bundles and return;
                if (!Directory.Exists(BundlePath)) {
                    SetPreviousBundleValues(null, null);
                    return;
                }

                // Get all directories 
                var previousBundles = new DirectoryInfo(BundlePath).GetDirectories();

                // Select any directory that has "ReleaseFiles" inside it
                var previousBundleWebFiles = previousBundles.Select(dir => dir.GetDirectories().SingleOrDefault(subDir => subDir.Name == ManualUpdateBundler.WebFilesBackup))
                                                            .Where(dir => dir != null).OrderByDescending(dir => dir.CreationTime).ToArray();

                // Select any Directory that has a <FinalBundleName>.snp in it.
                var previousBundleSnapshots = previousBundles.Select(dir => dir.GetFiles().SingleOrDefault(file => file.Name == FinalBundleName + ".snp"))
                                                             .Where(dir => dir != null).OrderByDescending(file => file.CreationTime).ToArray();

                SetPreviousBundleValues(previousBundleWebFiles, previousBundleSnapshots);
            }
        }

        private void SetPreviousBundleValues(DirectoryInfo[] previousWebFiles, FileInfo[] previousSnapshots)
        {
            SourceSelector.AllPreviousBundleFolders = previousWebFiles;
            TargetSelector.AllPreviousBundleFolders = previousWebFiles;

            SourceSelector.AllPreviousBundleDatabases = previousSnapshots;
            TargetSelector.AllPreviousBundleDatabases = previousSnapshots;

            SourceSelector.PreviousBundleFolderIndex = 0;
            SourceSelector.PreviousBundleDatabaseIndex = 0;
        }
        
        private string[] GetBundleNameSuggestions()
        {
            if (String.IsNullOrWhiteSpace(CompanyName) || !Directory.Exists(Path.Combine(RootPath, CompanyName)))
                return null;
            
            var companyDirectory = new DirectoryInfo(Path.Combine(RootPath, CompanyName));

            return companyDirectory.GetDirectories().Select(d => d.Name).ToArray();
        }

        private string[] GetCompanyNameSuggestions()
        {
            var rootDirectory = new DirectoryInfo(RootPath);

            var allCompanies = rootDirectory.GetDirectories().Select(c => c.Name).ToArray();

            return allCompanies;
        }

        private void SetupPathSuggestions()
        {
            // Check all the existing suggestion directories still exist
            Settings.Default.PreviousSources = Settings.Default.PreviousSources?.Where(x => Directory.Exists(x)).ToList();
            Settings.Default.PreviousTargets = Settings.Default.PreviousTargets?.Where(x => Directory.Exists(x)).ToList();
            Settings.Default.PreviousOutputs = Settings.Default.PreviousOutputs?.Where(x => Directory.Exists(x)).ToList();

            // If any of the suggestions are null, we need to create a new list so we can add to it.
            if (Settings.Default.PreviousSources == null)
                Settings.Default.PreviousSources = new List<string>();

            if (Settings.Default.PreviousTargets == null)
                Settings.Default.PreviousTargets = new List<string>();

            if (Settings.Default.PreviousOutputs == null)
                Settings.Default.PreviousOutputs = new List<string>();

            Settings.Default.Save();
        }

        private double[] GetAllMexBuilds()
        {
            var buildString = ConfigurationManager.AppSettings["Builds"];
            double[] builds = null;

            try {
                 builds = buildString.Split(',').Select(s => double.Parse(s)).OrderByDescending(build => build).ToArray();
            } catch (FormatException) {
                MessageBox.Show("One or more of the build numbers are not formatted as a decimal. The build numbers must be in a comma separated string. Check the .exe.config to fix this.", "Format Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return builds;
        }

        public void SwitchTargetAndSource()
        {
            var temp = SourceSelector;
           
            SourceSelector = TargetSelector;
            TargetSelector = temp;

            // Fix the titles
            SourceSelector.Title = "Source";
            TargetSelector.Title = "Target";
        }

        public async Task CreateBundle()
        {
            // Becuase this is async, we want to make sure that we don't run it multiple times
            if (this.IsBundling)
                return;

            try {
                this.IsBundling = true;

                this.Progress = "Setting up...";

                // Start registering both the DB's ASAP as this is one of the biggest bottlenecks
                var sourceDBTask = GetDatabaseFromResourceSelector(this.SourceSelector);
                var targetDBTask = GetDatabaseFromResourceSelector(this.TargetSelector);

                var sourcePath = GetFolderPathFromResourceSelector(this.SourceSelector);
                var targetPath = GetFolderPathFromResourceSelector(this.TargetSelector);
                
                var customOutputPath = this.OutputPicker.FolderPath;

                var bundler = new ManualUpdateBundler(sourcePath, targetPath, BundlePath, SelectedBuild, BundleDescription);
                bundler.OnProgressUpdated += ProgressChangedEvent;
                
                // Naming is hard
                FinalBundleName = $"{BundleName}_Build{SelectedBuild}_{GetRevisionNumber()}";
                var finalBundlePath = Path.Combine(BundlePath, FinalBundleName);

                // Result gets reused so declare it here
                MessageBoxResult result;

                // Give a prompt if the revision already exists.
                if (Directory.Exists(finalBundlePath)) {
                    result = MessageBox.Show("A Bundle with this revision already exists. Do you wish to override it?", "Override Confirmation", MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                this.Progress = "Creating/Clearing Directory...";

                // Ensure the directory exists and remove any files/folders aready in the directory
                Directory.CreateDirectory(finalBundlePath);
                await DirectoryCompare.ClearDirectory(finalBundlePath);

                result = MessageBox.Show("Do you wish to create a backup of the web files? If you do no you won't be able to use this as a previous bundle.", "Backup Confirmation", MessageBoxButton.YesNo);
                var createBackup = (result == MessageBoxResult.Yes);

                this.Progress = "Starting Bundle...";

                await bundler.CreateBundleAsync(FinalBundleName, createBackup, sourceDBTask, targetDBTask, customOutputPath);
                SaveNewPaths(customOutputPath, sourcePath, targetPath);

                // Force update the property so it updates the previous bundle drop downs
                OnPropertyChanged(nameof(this.BundleName));
                this.Progress = $"Finished Bundle! Bundle can be found at: {Environment.NewLine + finalBundlePath}";

                MessageBox.Show("Finished bundling files", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error trying to create the bundle", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                this.IsBundling = false;
            }
        }

        private string GetRevisionNumber()
        {
            // If a revision was specified in the UI, use that.
            if (!string.IsNullOrWhiteSpace(Revision))
                return Revision;

            // Matches with anything that ends with the same build number, an underscore, and a number
            var regex = "_Build" + SelectedBuild + @"_(\d+)$";
            var rx = new Regex(regex);

            if (!Directory.Exists(BundlePath))
                return "1";

            // Get each directory that matches the above regex, get the second match and parse it to a double.
            var autoRevisions = new DirectoryInfo(BundlePath).GetDirectories().Select(dir => rx.Match(dir.Name)).Where(m => m.Success && m.Groups.Count == 2)
                                                                              .Select(m => double.Parse(m.Groups[1].Value))
                                                                              .OrderBy(num => num).ToArray();

            // If there are no revisions currently, set the revision to 1
            if (autoRevisions == null || !autoRevisions.Any())
                return "1";

            // Check to see if any of the numbers are missing from the sequence. EG. [1, 2, 3, 5] would return 4
            for (int i = 1; i < autoRevisions.Length; i++) {
                if (autoRevisions[i - 1] != i) {
                    // Check that we aren't going to try to index a value at -1
                    if (i == 1) {
                        return "1";
                    } else {
                        return (autoRevisions[i - 2] + 1).ToString();
                    }
                }
            }

            // If the sequence was unbroken, find the highest revision all the revisions and add 1 
            var highestRevision = autoRevisions.Last() + 1;
            
            return highestRevision.ToString();
        }

        private string GetFolderPathFromResourceSelector(ResourceSelectorViewModel selector)
        {
            switch (selector.FolderSourceType) {
                case SelectorType.Custom:
                    return selector.FolderPicker.FolderPath;
                case SelectorType.PreviousBuild:
                    return selector.SelectedPreviousBundleFolder?.FullName;
                case SelectorType.Standard:
                    return selector.SelectedStandardBuildFolder?.FullName;
                default:
                    throw new InvalidOperationException("Selector type is invalid for database type");
            }
        }

        private async Task<Database> GetDatabaseFromResourceSelector(ResourceSelectorViewModel selector)
        {
            switch (selector.DatabaseSourceType) {
                case SelectorType.Custom:
                    return await GetDatabaseFromCustomConnection(selector);
                case SelectorType.PreviousBuild:
                    return GetDatabaseFromPreviousSnapshot(selector);
                case SelectorType.Standard:
                    return GetDatabaseFromStandardBuild(selector);
                default:
                    throw new InvalidOperationException("Selector type is invalid for database type");
            }
        }

        private async Task<Database> GetDatabaseFromCustomConnection(ResourceSelectorViewModel selector)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            cts.CancelAfter(ManualUpdateBundler.RegisterDBTimeout);

            return await Task.Run(() => {
                var database = new Database();

                var builder = new SqlConnectionStringBuilder(selector.CustomConnectionViewModel.ConnectionString);
                var connectionProperties = new ConnectionProperties(builder.DataSource, builder.InitialCatalog, builder.UserID, builder.Password);

                database.Register(connectionProperties, SQLCompare.Options);

                return database;
            });
        }

        private Database GetDatabaseFromPreviousSnapshot(ResourceSelectorViewModel selector)
        {
            throw new NotImplementedException();
        }

        private Database GetDatabaseFromStandardBuild(ResourceSelectorViewModel selector)
        {
            var database = new Database();
            database.LoadFromDisk(selector.SelectedBuild.FullName);

            return database;
        }

        private void SaveNewPaths(string outputPath, string sourcePath, string targetPath)
        {
            if (outputPath != null && (!Settings.Default.PreviousOutputs?.Contains(outputPath) ?? false))
                Settings.Default.PreviousOutputs.Add(outputPath);

            if (SourceSelector.FolderSourceType == SelectorType.Custom && sourcePath != null && (!Settings.Default.PreviousSources?.Contains(sourcePath) ?? false))
                Settings.Default.PreviousSources.Add(sourcePath);

            if (TargetSelector.FolderSourceType == SelectorType.Custom && targetPath != null && (!Settings.Default.PreviousTargets?.Contains(targetPath) ?? false))
                Settings.Default.PreviousTargets.Add(targetPath);

            Settings.Default.Save();
        }
    }
}
