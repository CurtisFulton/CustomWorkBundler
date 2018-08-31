using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomWorkBundler
{
    public class DirectoryCompare
    {
        public FileInfo[] Differences { get; private set; }
        public string DirectoryPathA { get; private set; }
        public string DirectoryPathB { get; private set; }

        private List<string> ExcludeExtensions { get; set; } = new List<string> { ".js", ".map", ".pdb", ".csproj", ".user"};
        private List<string> ExcludeFiles { get; set; } = new List<string> { "debuglog", "log.txt", "listinglayout", "reportdata", "userdata", "Web.config",
                                                                            "log4net", "Newtonsoft.json", "System.Net", "System.Web", "MEXData_Ideablade.dll",
                                                                            "UpgradeSQL.sql", "Mexdata.dll.config", "IdeaBlade.ibconfig", "jquery.livequery.min.js" };

        private List<string> ExcludePaths { get; set; } = new List<string> { "obj", "Documents", "Downloads"};
        private bool IgnoreRootFiles { get; set; } = true;

        public DirectoryCompare(string pathA, string pathB, bool generateDifferences = true)
        {
            DirectoryPathA = pathA;
            DirectoryPathB = pathB;

            if (generateDifferences)
                Differences = CompareDirectories();
        }

        public FileInfo[] CompareDirectories()
        {
            // If source path is null, we want to just return all the files in Path B as there is nothing to compare to
            if (DirectoryPathA == null)
                return new DirectoryInfo(DirectoryPathB).GetFiles("*.*", SearchOption.AllDirectories).Where(IsValidFile).ToArray();

            DirectoryInfo dirA = new DirectoryInfo(DirectoryPathA);
            DirectoryInfo dirB = new DirectoryInfo(DirectoryPathB);
            
            FileInfo[] filesA = dirA.GetFiles("*.*", SearchOption.AllDirectories).Where(IsValidFile).ToArray();
            FileInfo[] filesB = dirB.GetFiles("*.*", SearchOption.AllDirectories).Where(IsValidFile).ToArray();
            
            var fileComparer = new FileCompare(DirectoryPathA, DirectoryPathB);

            var differences = new List<FileInfo>(filesB.Length);
            for (int i = 0; i < filesB.Length; i++) {
                // Check if the file in B's director does not exist/Is not the same in directory A
                if (!filesA.Contains(filesB[i], fileComparer))
                    differences.Add(filesB[i]);
            }

            Differences = differences.ToArray();

            return Differences;
        }

        public async Task<FileInfo[]> CompareDirectoriesAsync()
        {
            DirectoryInfo dirA = new DirectoryInfo(DirectoryPathA);

            FileInfo[] filesA = dirA.GetFiles("*.*", SearchOption.AllDirectories).Where(IsValidFile).ToArray();
            
            DirectoryInfo dirB = new DirectoryInfo(DirectoryPathB);

            FileInfo[] filesB = dirB.GetFiles("*.*", SearchOption.AllDirectories).Where(IsValidFile).ToArray();
            
            var fileComparer = new FileCompare(DirectoryPathA, DirectoryPathB);
            
            await Task.Run(() => {
                var differences = new List<FileInfo>();
                for (int i = 0; i < filesB.Length; i++) {
                    // Check if the file in B's director does not exist in directory A
                    if (!filesA.Contains(filesB[i], fileComparer))
                        differences.Add(filesB[i]);
                }

                Differences = differences.ToArray();
                Console.WriteLine($"Found {Differences.Length} differences.");
            });

            return Differences;
        }

        public void CopyDifferencesTo(string outputDirectory)
        {
            // Create the output directory (Or do nothing if it exists)
            Directory.CreateDirectory(outputDirectory);
            
            foreach (var file in Differences) {
                var fullDirPath = file.DirectoryName;
                string relativePath = GetRelativePath(fullDirPath, DirectoryPathB);

                var newPath = Path.Combine(outputDirectory, relativePath);
                Directory.CreateDirectory(newPath);

                File.Copy(file.FullName, Path.Combine(newPath, file.Name), true);

                // Copy the .min and .min.map for js files
                if (file.Extension == ".js") {
                    var fileName = Path.GetFileNameWithoutExtension(file.FullName).Replace(".min", "");
                    var originalFilePath = Path.Combine(file.DirectoryName, $"{fileName}.js");
                    var mapFilePath = Path.Combine(file.DirectoryName, $"{fileName}.min.js.map");

                    if (!File.Exists(originalFilePath))
                        throw new FileNotFoundException($"Could not find the .js for '{fileName}' inside {file.DirectoryName}");

                    if (!File.Exists(mapFilePath))
                        throw new FileNotFoundException($"Could not find the .min.js.map for '{fileName}' inside {file.DirectoryName}");

                    // TODO Check the min/map files are up to date
                    File.Copy(originalFilePath, Path.Combine(newPath, $"{fileName}.js"));
                    File.Copy(mapFilePath, Path.Combine(newPath, $"{fileName}.min.js.map"));
                }
            }
        }
        
        private static async Task DeleteFileAsync(FileInfo file)
        {
            await Task.Run(() => {
                file.Delete();
            });
        }

        public static async Task ClearDirectory(string dirPath)
        {
            // Delete all files/Folders in there
            var outputDir = new DirectoryInfo(dirPath);

            var allFiles = outputDir.GetFiles("*.*", SearchOption.AllDirectories);
            var tasks = new Task[allFiles.Length];

            // Queue up file deletions
            for (int i = 0; i < allFiles.Length; i++) {
                tasks[i] = DeleteFileAsync(allFiles[i]);
            }

            await Task.WhenAll(tasks);

            outputDir.Delete(true);
            Directory.CreateDirectory(dirPath);
        }

        private bool IsValidFile(FileInfo file)
        {
            if (IgnoreRootFiles) {
                if (file.DirectoryName == DirectoryPathA || file.DirectoryName == DirectoryPathB)
                    return false;
            }

            if (ExcludeExtensions.Any(x => file.Name.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (ExcludeFiles.Any(x => file.Name.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (ExcludePaths.Any(x => GetRelativePath(file.DirectoryName, DirectoryPathA).StartsWith(x) || GetRelativePath(file.DirectoryName, DirectoryPathB).StartsWith(x)))
                return false;

            // All .min.js files are included unless specified
            if (file.Name.EndsWith(".min.js"))
                return true;

            return true;
        }

        public static string GetRelativePath(string path, string rootPath)
        {
            var relativePath = "";

            if (path.IndexOf(rootPath) == -1)
                return path;

            if (path.Length != rootPath.Length)
                relativePath = path.Substring(rootPath.Length + 1);

            return relativePath;
        }
    }
}
