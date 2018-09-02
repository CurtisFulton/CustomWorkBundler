using CustomWorkBundler.Logic.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CustomWorkBundler.Logic
{
    public class DirectoryCompare
    {
        private string PathA { get; set; }
        private string PathB { get; set; }

        private string[] ExcludedExtensions { get; set; } = new string[] { ".js", ".map", ".pdb", ".csproj", ".user" };
        private string[] ExcludedDirectories { get; set; } = new string[] { "obj", "Documents", "Downloads" };
        private string[] ExcludedFiles { get; set; } = new string[] { "debuglog", "log.txt", "listinglayout", "reportdata", "userdata", "Web.config",
                                                                     "log4net", "Newtonsoft.json", "System.Net", "System.Web", "MEXData_Ideablade.dll",
                                                                     "UpgradeSQL.sql", "Mexdata.dll.config", "IdeaBlade.ibconfig", "jquery.livequery.min.js" };
        
        public DirectoryCompare(string pathA, string pathB)
        {
            this.PathA = pathA;
            this.PathB = pathB;
        }

        public async Task<string[]> GetDifferencesAsync(bool ignoreRootFiles)
        {
            // If both paths are empty return null
            if (this.PathA.IsEmpty() && this.PathB.IsEmpty())
                return null;

            return await Task.Run(() => {
                // Get all files in PathA and Path B
                var filesA = this.GetFilesInDirectory(this.PathA, ignoreRootFiles);
                var filesB = this.GetFilesInDirectory(this.PathB, ignoreRootFiles);

                // If PathA wasn't passed in, return all of PathB's files
                if (this.PathA.IsEmpty())
                    return filesB;

                var fileComparer = new FileCompare();

                var differences = new List<string>(filesB.Length);
                for (int i = 0; i < filesB.Length; i++) {
                    var file = filesB[i];

                    // If fileB is in FilesA continue on
                    if (filesA.Contains(file, fileComparer))
                        continue;

                    differences.Add(file);
                    
                    // Special case for .js files. We only considered .min files 'valid', so we need to add the .js and .map files back
                    if (Path.GetExtension(file) == ".js") {
                        var fileDirectory = Path.GetDirectoryName(file);
                        var baseFileName = Path.GetFileNameWithoutExtension(file).Replace(".min", "");

                        // Get the .js and .map files for this .min file
                        var baseJSFile = Path.Combine(fileDirectory, $"{baseFileName}.js");
                        var mapFile = Path.Combine(fileDirectory, $"{baseFileName}.min.js.map");

                        // Check the .js and .map exist
                        if (!File.Exists(baseJSFile))
                            throw new FileNotFoundException($"Could not find the .js file for '{baseFileName}' inside {fileDirectory}");
                        if (!File.Exists(mapFile))
                            throw new FileNotFoundException($"Could not find the .js file for '{mapFile}' inside {fileDirectory}");

                        // TODO: Probably check that the min/map were modified AFTER the base JS file
                        // Add the files as differences
                        differences.Add(baseJSFile);
                        differences.Add(mapFile);
                    }
                }

                return differences.ToArray();
            });
        }

        private string[] GetFilesInDirectory(string path, bool ignoreRootFiles)
        {
            // If the path is null or empty, return null
            if (path.IsEmpty())
                return null;

            var directory = new DirectoryInfo(path);
            // Gets all files that are valid and selects the full name
            return directory.GetFiles("*.*", SearchOption.AllDirectories)
                            .Where(file => IsValidFile(file.FullName, ignoreRootFiles))
                            .Select(file => file.FullName).ToArray();
        }

        private bool IsValidFile(string file, bool ignoreRootFiles)
        {
            var fileDirectory = Path.GetDirectoryName(file);
            var fileName = Path.GetFileName(file);
            
            if (ignoreRootFiles) {
                // If we are ignoring root files and this files directory matches PathA or PathB, return false
                if (fileDirectory == this.PathA || fileDirectory == this.PathB)
                    return false;
            }

            // Check this files extension isn't included in any of the excluded extensions
            if (this.ExcludedExtensions.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                return false;

            // Check this file isn't included in any of the excluded files
            if (this.ExcludedFiles.Any(x => fileName.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                return false;

            // Check this file isn't in any of the excluded directories
            if (this.ExcludedDirectories.Any(x => GetRelativePath(fileDirectory, this.PathA).StartsWith(x) || GetRelativePath(fileDirectory, this.PathB).StartsWith(x)))
                return false;
            
            return true;
        }

        #region Helper Functions

        public static string GetRelativePath(string path, string rootPath)
        {
            var relativePath = "";

            if (path.IndexOf(rootPath) == -1)
                return path;

            if (path.Length != rootPath.Length)
                relativePath = path.Substring(rootPath.Length + 1);

            return relativePath;
        }

        #endregion
    }
}