using CustomWorkBundler.Logic.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CustomWorkBundler.Logic
{
    public class FileCompare : IEqualityComparer<string>
    {
        private static Dictionary<string, string> CheckedFilesCache { get; set; } = new Dictionary<string, string>();
        
        public bool Equals(string fileA, string fileB)
        {
            // If they are the same file return true
            if (fileA == fileB) {
                return true;
            }
            
            // Cache of what files are the same. Saves doing a byte compare over and over again.
            if (CheckedFilesCache.TryGetValue(fileA, out string comparedFile)) {
                // Check the cached file is the same as the current fileB. 
                return fileB == comparedFile;
            }
            
            // If either of them is null or empty
            if (fileA.IsEmpty() || fileB.IsEmpty())
                return false;

            // If the filenames are different return false
            if (Path.GetFileName(fileA) != Path.GetFileName(fileB))
                return false;
            
            var bytesA = File.ReadAllBytes(fileA);
            var bytesB = File.ReadAllBytes(fileB);

            // Compare them on a btye level as a last resort
            var areEqual = bytesA.SequenceEqual(bytesB);

            // Add them to the cached files if they are the same
            if (areEqual)
                CheckedFilesCache.Add(fileA, fileB);

            return areEqual;
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}