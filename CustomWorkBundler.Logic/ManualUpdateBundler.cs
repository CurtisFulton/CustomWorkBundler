using RedGate.SQLCompare.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomWorkBundler
{
    public class ManualUpdateBundler
    {
        public const int RegisterDBTimeout = 30000;
        public const string WebFilesBackup = "MEXDATA_Release";

        private Database SourceDatabase { get; set; }
        private Database TargetDatabase { get; set; }

        public event Action<string> OnProgressUpdated = (msg) => { };

        private DirectoryCompare DirectoryComparer { get; set; }
        private SQLCompare SQLComparer { get; set; }
        private VBUpdateScript VBUpdateScript { get; set; }

        private string SourceDirectory { get; set; }
        private string TargetDirectory { get; set; }
        private string OutputDirectory { get; set; }
        private string CustomOutputDirectory { get; set; }

        private string BundleDescription { get; set; }

        private double BuildNumber { get; set; }

        public ManualUpdateBundler(string sourceDirectory, string targetDirectory, string outputDirectory, double buildNumber, string bundleDescription)
        {
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            OutputDirectory = outputDirectory;

            BundleDescription = bundleDescription;

            BuildNumber = buildNumber;
        }
        
        public async Task CreateBundleAsync(string bundleName, bool createWebfilesBackup, Task<Database> sourceDB, Task<Database> targetDB, string customOutputDirectory = null)
        {
            CustomOutputDirectory = customOutputDirectory;

            await Task.Run(async () => {
                OnProgressUpdated("Validating Bundle....");
                if (!String.IsNullOrWhiteSpace(CustomOutputDirectory) && !Directory.Exists(CustomOutputDirectory))
                    throw new DirectoryNotFoundException($"Could not find the output directory {CustomOutputDirectory}. Make sure it exists before selecting it.");

                // Store the bundle output folder for convinience
                var bundleOutput = Path.Combine(OutputDirectory, bundleName);
                var webFilesBackupPath = Path.Combine(bundleOutput, WebFilesBackup);

                Task cloneWebFilesTask = null;

                if (createWebfilesBackup) {
                    // Copy both the source and the ReleaseFiles (In this order so the target overrides the source)
                    cloneWebFilesTask = CloneDirectory(SourceDirectory, webFilesBackupPath);
                }

                // Compare the directories, and generate the Vb update script
                DirectoryComparer = new DirectoryCompare(SourceDirectory, TargetDirectory, false);

                // Start comparing directories on a separate thread
                var compareTask = DirectoryComparer.CompareDirectoriesAsync();

                OnProgressUpdated("Registering DB's...");

                // Wait till we get both our DB's
                await Task.WhenAll(sourceDB, targetDB);

                SourceDatabase = sourceDB.Result;
                TargetDatabase = targetDB.Result;

                if (SourceDatabase == null || TargetDatabase == null)
                    throw new SqlCompareException("Error occured while trying to register the DB's. Try again.");

                OnProgressUpdated("Generating Update Script...");
                // Only try to compare the db's, if both of them are not null
                if (SourceDatabase != null && TargetDatabase != null)
                    SQLComparer = new SQLCompare(SourceDatabase, TargetDatabase);

                OnProgressUpdated("Comparing Directories...");
                await compareTask;

                OnProgressUpdated("Creating VB Update Script...");
                VBUpdateScript = new VBUpdateScript(BuildNumber, bundleName, SQLComparer?.UpdateScript);

                OnProgressUpdated("Copying Difference To ReleaseFiles...");
                // Copy the differences to the output folder, and 
                DirectoryComparer.CopyDifferencesTo(Path.Combine(bundleOutput, "ReleaseFiles"));
                VBUpdateScript.WriteVbScriptTo(Path.Combine(bundleOutput, $"{bundleName}.vb"));

                // Save the database snapshot
                if (SQLComparer != null) {
                    OnProgressUpdated("Saving Database Snapshot...");
                    SQLComparer.WriteDatabaseTo(bundleOutput, $"{bundleName}.snp");

                    if (!string.IsNullOrWhiteSpace(SQLComparer.UpdateScript))
                        File.WriteAllText(Path.Combine(bundleOutput, "UpdateScript.sql"), SQLComparer.UpdateScript);
                }

                OnProgressUpdated("Zipping Files...");
                // Zip the final bundle for manual updates
                ZipBundle(bundleOutput, bundleName);

                OnProgressUpdated("Writing Info.Txt...");
                var infoPath = Path.Combine(bundleOutput, "Info.txt");

                using (var streamWriter = new StreamWriter(infoPath, true)) {
                    if (!string.IsNullOrWhiteSpace(BundleDescription)) {
                        streamWriter.WriteLine("/********************************* Description *********************************/");
                        streamWriter.WriteLine();
                        streamWriter.WriteLine(BundleDescription + Environment.NewLine);
                    }
                    
                    streamWriter.WriteLine("/******************************** Changed Files ********************************/");
                    streamWriter.WriteLine();

                    foreach (var file in DirectoryComparer.Differences) {
                        streamWriter.WriteLine(DirectoryCompare.GetRelativePath(file.FullName, TargetDirectory));
                    }
                }

                if (createWebfilesBackup) {
                    OnProgressUpdated("Waiting For Release Files To Copy");
                    await cloneWebFilesTask;

                    await CloneDirectory(Path.Combine(bundleOutput, "ReleaseFiles"), webFilesBackupPath);
                }

                // Remove the Release files and .vb as they are no longer needed because they are in the .zip
                OnProgressUpdated("Cleaning Up...");
                Directory.Delete(Path.Combine(bundleOutput, "ReleaseFiles"), true);
                File.Delete(Path.Combine(bundleOutput, bundleName + ".vb"));
                
                if (!string.IsNullOrWhiteSpace(CustomOutputDirectory)) {
                    OnProgressUpdated("Copying to Custom Output Directory...");
                    var customOutput = Path.Combine(CustomOutputDirectory, bundleName);
                    Directory.Delete(customOutput, true);

                    await CloneDirectory(bundleOutput, customOutput);
                }
            });
        }
        
        private async Task CopyFile(string SourcePath, string DestinationPath)
        {
            await Task.Run(() => {
                File.Copy(SourcePath, DestinationPath, true);
            });
        }

        private async Task CloneDirectory(string SourcePath, string DestinationPath)
        {
            await Task.Run(() => {
                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(SourcePath, "*.*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

                var allFiles = Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories);
                Task[] tasks = new Task[allFiles.Length];

                // Queue up all the copies
                for (int i = 0; i < allFiles.Length; i++) {
                    var newPath = allFiles[i];
                    tasks[i] = CopyFile(newPath, newPath.Replace(SourcePath, DestinationPath));
                }

                // Wait for all tasks to finish
                Task.WaitAll(tasks);
            });
        }
        
        private static void ZipBundle(string targetDirectory, string bundleName)
        {
            var sourceDirectory = Path.Combine(targetDirectory, bundleName);
            var targetPath = Path.Combine(targetDirectory, $"{bundleName}.zip");

            var releaseFilesPath = Path.Combine(targetDirectory, "ReleaseFiles");
            var allFiles = new DirectoryInfo(releaseFilesPath).GetFiles("*.*", SearchOption.AllDirectories);

            // Open up file stream and archive
            using (var fileStream = new FileStream(targetPath, FileMode.CreateNew)) {
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true)) {
                    // Loop over every file
                    for (int i = 0; i < allFiles.Length; i++) {
                        // Get the relative path
                        var relativePath = DirectoryCompare.GetRelativePath(allFiles[i].FullName, releaseFilesPath);

                        // Find the name (Including file path) inside the archive
                        var newFileName = Path.Combine(bundleName, "ReleaseFiles", relativePath);

                        archive.CreateEntryFromFile(allFiles[i].FullName, newFileName, CompressionLevel.Fastest);
                    }
                    
                    // Lastly add the .vb
                    var vbFileName = bundleName + ".vb";
                    archive.CreateEntryFromFile(Path.Combine(targetDirectory, vbFileName), Path.Combine(bundleName, vbFileName), CompressionLevel.Fastest);
                }
            }
        }
    }
}
