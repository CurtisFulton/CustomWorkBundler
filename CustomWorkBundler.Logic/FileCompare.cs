using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CustomWorkBundler
{
    public class FileCompare : IEqualityComparer<FileInfo>
    {
        private string PathARoot { get; set; }
        private string PathBRoot { get; set; }

        private static Dictionary<FileInfo, FileInfo> CheckedFilesCache { get; set; }

        public FileCompare(string pathA, string pathB)
        {
            CheckedFilesCache = new Dictionary<FileInfo, FileInfo>();

            PathARoot = pathA;
            PathBRoot = pathB;
        }

        public bool Equals(FileInfo fileA, FileInfo fileB)
        {
            // Cache of what files are the same. Saves doing a byte compare over and over again.
            if (CheckedFilesCache.TryGetValue(fileA, out FileInfo comparedFile)) {
                // Check the cached file is the same as the current fileB. 
                return fileB == comparedFile; 
            }

            // If they are the same file return true
            if (fileA == fileB)
                return true;

            // If they aren't the same file, but either of them is null return false
            if (fileA == null || fileB == null)
                return false;

            // If the filenames are different return false
            if (fileA.Name != fileB.Name)
                return false;

            // Assume if the file names are the same, lengths are the same and their modified times are the same, they are the same file
            if (fileA.LastWriteTime == fileB.LastWriteTime)
                return true;
            
            var bytesA = File.ReadAllBytes(fileA.FullName);
            var bytesB = File.ReadAllBytes(fileB.FullName);
            
            // Compare them on a btye level as a last resort
            var areEqual = bytesA.SequenceEqual(bytesB);

            // Add them to the cached files if they are the same
            if (areEqual)
                CheckedFilesCache.Add(fileA, fileB);

            return areEqual;
        }

        public int GetHashCode(FileInfo obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
