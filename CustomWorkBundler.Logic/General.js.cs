using CustomWorkBundler.Logic.Extensions;
using System;
using System.IO;
using System.Linq;

namespace CustomWorkBundler.Logic
{
    public static class General
    {
        public static string[] GetExistingCompanyNames(string rootPath) => Directory.GetDirectories(rootPath);
        public static string[] GetExistingCustomisations(string rootPath, string companyName)
        {
            var path = Path.Combine(rootPath, companyName);
            if (companyName.IsEmpty() || !Directory.Exists(path))
                return null;

            return Directory.GetDirectories(Path.Combine(rootPath, companyName));
        }

        public static string GetBundlePath(string rootPath, string companyName, string customisationName)
        {
            if (companyName.IsEmpty() || customisationName.IsEmpty())
                return null;

            return Path.Combine(rootPath, companyName, customisationName);
        }

        public static DirectoryInfo[] GetPreviousWebFiles(string bundlePath)
        {
            var existingCustomisations = new DirectoryInfo(bundlePath).GetDirectories();

            // Select any directory that has the <WebFilesRelease> folder inside it
            return existingCustomisations.Where(dir => dir.GetDirectories().SingleOrDefault(subDir => subDir.Name == PackageBuilder.WebFilesRelease) != null)
                                         .OrderByDescending(dir => dir.CreationTime).ToArray();
        }

        public static FileInfo[] GetPreviousDatabases(string bundlePath)
        {
            var existingCustomisations = new DirectoryInfo(bundlePath).GetDirectories();

            return existingCustomisations.Select(dir => dir.GetFiles().SingleOrDefault(file => file.Name == PackageBuilder.SnapshotFileName))
                                                                      .Where(dir => dir != null)
                                                                      .OrderByDescending(file => file.CreationTime).ToArray();
        }
    }
}