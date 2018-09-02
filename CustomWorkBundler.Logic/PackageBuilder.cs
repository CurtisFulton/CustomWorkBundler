using CustomWorkBundler.Logic.Events;
using CustomWorkBundler.Logic.Extensions;
using RedGate.SQLCompare.Engine;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CustomWorkBundler.Logic
{
    public class PackageBuilder
    {
        public const string WebFilesRelease = "MEXDATA_Release";
        public const string SnapshotFileName = "DatabaseSchema.snp";
        public const string SqlFileName = "SQLScript.sql";

        #region Paths

        private string SourcePath { get; set; }
        private string TargetPath { get; set; }

        private string RootPath { get; set; }

        private string CustomisationPath { get; set; }
        private string OutputPath { get; set; }

        private string FullBackupPath { get; set; }

        #endregion  

        #region Database Data
        
        private Task<Database> SourceDatabaseTask { get; set; }
        private Task<Database> TargetDatabaseTask { get; set; }

        #endregion  

        #region Bundle Meta Data

        private string CompanyName { get; set; }
        private string CustomisationName { get; set; }

        private string BundleName { get; set; }
        private string BundleDescription { get; set; }

        private string Revision { get; set; }
        private double Build { get; set; }
        
        #endregion  

        public PackageBuilder(string rootPath, string companyName, string customisationName, double buildNumber, string revision = "", string bundleDescription = "")
        {
            this.RootPath = rootPath;

            this.CompanyName = companyName;
            this.CustomisationName = customisationName;

            this.CustomisationPath = Path.Combine(this.RootPath, this.CompanyName, this.CustomisationName);

            this.Build = buildNumber; 
            this.Revision = revision.HasValue() ? revision : this.GetRevision();

            this.BundleName = $"{this.CustomisationName}_Build{this.Build}_{this.Revision}";
            this.BundleDescription = bundleDescription;

            this.OutputPath = Path.Combine(this.CustomisationPath, this.BundleName);
            this.ValidatePackage();
        }

        public async Task CreateBundleAsync(bool createFullBackup, string customOutputPath = null)
        {
            this.UpdateProgress("Starting Bundle");

            Task clearDirectoryTask = ClearDirectoryAsync(this.OutputPath);

            Task backupTask = null;
            bool shouldtaskBackup = createFullBackup && this.FullBackupPath.HasValue();

            // Start the cloning task if we want to take a backup and we have a backup path. 
            if (shouldtaskBackup) {
                backupTask = CloneDirectoryAsync(this.SourcePath, this.FullBackupPath);
            }

            this.UpdateProgress("Finding Differences");
            var directoryComparer = new DirectoryCompare(this.SourcePath, this.TargetPath);
            var differences = await directoryComparer.GetDifferencesAsync(true);

            this.UpdateProgress("Waiting For Databases To Register");
            Database sourceDatabase = this.SourceDatabaseTask?.Result;
            Database targetDatabase = this.TargetDatabaseTask?.Result;

            this.UpdateProgress("Comparing Databases");
            string databaseUpdateScript = null;

            using (var databaseComparer = new DatabaseCompare(sourceDatabase, targetDatabase)) {
                databaseUpdateScript = await databaseComparer.GenerateUpdateScriptAsync();

                // If there is any kind of SQL change, output the files
                if (databaseUpdateScript.HasValue()) {
                    File.WriteAllText(Path.Combine(this.OutputPath, SqlFileName), databaseUpdateScript);

                    this.UpdateProgress("Writing Database Schema To File");
                    targetDatabase.SaveToDisk(Path.Combine(this.OutputPath, SnapshotFileName));
                }
            }

            if ((differences == null || differences.Length == 0) && databaseUpdateScript.IsEmpty())
                throw new InvalidOperationException("No differences were found. Aborting bundle.");

            this.UpdateProgress("Clearing Directory");
            await clearDirectoryTask;
            Directory.CreateDirectory(this.OutputPath);

            this.UpdateProgress("Creating Manual Update Zip");
            var manualUpdateBundler = new ManualUpdateBundler(this.BundleName, this.Build);
            await manualUpdateBundler.CreateBundleAsync(this.OutputPath, this.TargetPath, differences, databaseUpdateScript);
            
            this.UpdateProgress("Writing BundleInfo.txt");
            this.WriteInfoFile(Path.Combine(this.OutputPath, "BundleInfo.txt"), differences, this.BundleDescription);

            if (shouldtaskBackup) {
                this.UpdateProgress("Waiting For Full Backup To Finish Copying");
                await backupTask;

                this.UpdateProgress("Applying Differences To Backup");
                await this.CopyFilesAsync(this.SourcePath, this.FullBackupPath, new string[1]);
            }

            // If we have a custom output path, copy everything there
            if (customOutputPath.HasValue()) {
                // Add the bundle name to the output path
                customOutputPath = Path.Combine(customOutputPath, this.BundleName);

                this.UpdateProgress("Copying To Custom Output Path");
                await ClearDirectoryAsync(customOutputPath);
                await CloneDirectoryAsync(this.OutputPath, customOutputPath);
            }
        }

        #region Database Registration

        public void RegisterDatabase(string sourceDB, string targetDB)
        {
            this.UpdateProgress("Starting Database Registration");

            // 2 Different processes need to happen depending on if it is a connection string or a file
            // All connection strings will require a "Data Source", so check if the string contains that.
            if (sourceDB.Contains("Data Source="))
                this.SourceDatabaseTask = RegisterDatabaseFromConnectionString(sourceDB);
            else
                this.SourceDatabaseTask = RegisterDatabaseFromFile(sourceDB);

            if (targetDB.Contains("Data Source="))
                this.SourceDatabaseTask = RegisterDatabaseFromConnectionString(targetDB);
            else
                this.SourceDatabaseTask = RegisterDatabaseFromFile(targetDB);
        }

        private async Task<Database> RegisterDatabaseFromConnectionString(string connectionString)
        {
            // Register the database on a background thread
            return await Task.Run(() => {
                var database = new Database();

                var builder = new SqlConnectionStringBuilder(connectionString);
                var connectionProperties = new ConnectionProperties(builder.DataSource, builder.InitialCatalog, builder.UserID, builder.Password);

                database.Register(connectionProperties, DatabaseCompare.Options);

                return database;
            });
        }

        private async Task<Database> RegisterDatabaseFromFile(string path)
        {
            // Check the file exists
            if (!File.Exists(path))
                throw new FileNotFoundException("Could not find the Database Snapshot", path);

            var database = new Database();

            await Task.Run(() => database.LoadFromDisk(path));

            return database;
        }

        #endregion

        #region Web File Registration

        public void RegisterWebFiles(string targetPath) => this.RegisterWebFiles(string.Empty, targetPath);
        public void RegisterWebFiles(string sourcePath, string targetPath)
        {
            this.UpdateProgress("Registering Source/Target Web Files");

            if (targetPath.IsEmpty())
                throw new ArgumentNullException(nameof(targetPath), "Target path cannot be null or empty. If you don't wish to compare Web Files, don't register them");

            this.SourcePath = sourcePath;
            this.TargetPath = targetPath;
            
            this.FullBackupPath = Path.Combine(this.OutputPath, WebFilesRelease);
        }

        #endregion

        #region Helpers 

        private void WriteInfoFile(string outputPath, string[] differences, string description)
        {
            using (var streamWriter = new StreamWriter(outputPath, true)) {
                if (description.HasValue()) {
                    streamWriter.WriteLine("/********************************* Description *********************************/");
                    streamWriter.WriteLine();
                    streamWriter.WriteLine(description + Environment.NewLine);
                }

                streamWriter.WriteLine("/******************************** Changed Files ********************************/");
                streamWriter.WriteLine();

                foreach (var file in differences) {
                    streamWriter.WriteLine(DirectoryCompare.GetRelativePath(file, this.TargetPath));
                }
            }
        }

        private void ValidatePackage()
        {
            if (this.RootPath.IsEmpty() || !Directory.Exists(this.RootPath))
                throw new DirectoryNotFoundException("The root path is not valid. Make sure that it exists and is not empty in the App.config");

            if (this.Build < 50)
                throw new InvalidOperationException("The Target Build cannot be less than 50 as this bundler is only intended to work on MEX 15");
        }

        private void UpdateProgress(string message) => new ProgressChangedEvent(message).Raise(this);

        private string GetRevision()
        {
            // Matches with anything that ends with the same build number, an underscore, and a number.
            // Returns 2 groups if matched. The entire match and the trailing number
            var regex = $"_Build{this.Build}" + @"_(\d+)$";
            var rx = new Regex(regex);

            // If the directory hasn't been create, this will be the first one
            if (!Directory.Exists(this.CustomisationPath ?? ""))
                return "1";

            var allRevisions = new DirectoryInfo(this.CustomisationPath).GetDirectories();

            // Get each directory that matches the above regex, get the second match and parse it to a double.
            var validRevisions = allRevisions.Select(dir => rx.Match(dir.Name))
                                             .Where(match => match.Success && match.Groups.Count == 2)
                                             .Select(match => double.Parse(match.Groups[1].Value))
                                             .OrderBy(num => num).ToArray();

            // If there are no revisions currently or the first one is not 1, set the revision to 1
            if (!validRevisions.Any() || validRevisions.First() != 1)
                return "1";

            // Check to see if any of the numbers are missing from the sequence. EG. [1, 2, 3, 6] would return 4
            for (int i = 1; i < validRevisions.Length; i++) {
                // Revisions start at 1 but arrays start at 0, so we need to check if the revision is equal to i + 1
                if (validRevisions[i] != i + 1) {
                    return (validRevisions[i - 1] + 1).ToString();
                }
            }

            // If the sequence was unbroken, find the highest revision and add 1 
            return (validRevisions.Last() + 1).ToString();
        }

        private async Task CloneDirectoryAsync(string sourcePath, string destinationPath)
        {
            await Task.Run(async () => {
                // Create all the directories first
                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*.*", SearchOption.AllDirectories)) {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
                }

                // Get all the files
                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                await CopyFilesAsync(sourcePath, destinationPath, files);
            });
        }
        private async Task CopyFilesAsync(string sourcePath, string destinationPath, string[] files)
        {
            // Exit early if there are no files
            if (files.Length == 0)
                return;
            
            Task[] tasks = new Task[files.Length];

            // Queue up all the file copies
            for (int i = 0; i < files.Length; i++) {
                var file = files[i];
                var relativeFilePath = DirectoryCompare.GetRelativePath(file, sourcePath);

                var fullDestinationPath = Path.Combine(destinationPath, relativeFilePath);
                var fileDirectory = Path.GetDirectoryName(fullDestinationPath);
                    
                // Create the directory if it doesn't already exist
                Directory.CreateDirectory(fileDirectory);

                tasks[i] = Task.Run(() => File.Copy(file, fullDestinationPath, true));
            }

            // Wait for all the files to copy
            await Task.WhenAll(tasks);
        }

        public static async Task ClearDirectoryAsync(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                return;

            var outputDir = new DirectoryInfo(dirPath);

            var files = outputDir.GetFiles("*.*", SearchOption.AllDirectories);
            var tasks = new Task[files.Length];
            
            // Queue up file deletions
            for (int i = 0; i < files.Length; i++) {
                var file = files[i];
                tasks[i] = Task.Run(() => file.Delete());
            }

            await Task.WhenAll(tasks);
        }

        #endregion  
    }
}