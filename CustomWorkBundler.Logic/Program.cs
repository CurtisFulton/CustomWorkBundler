using System;
using RedGate.SQLCompare.Engine;
using System.IO;
using RedGate.Shared.ComparisonInterfaces;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;

namespace CustomWorkBundler
{
    class Program
    {
        private const string OriginDirectoryPath = @"D:\Custom Work\Bundler Test\Standard Web";
        private const string TargetDirectoryPath = @"D:\Custom Work\Bundler Test\Custom Web";
        private const string OutputDirectory = @"D:\Custom Work\Bundler Test\Diff Output";
        private const string OutputName = @"BuildBundle";
        
        static void Main(string[] args)
        {
            Directory.CreateDirectory(OutputDirectory);
            // Remove any files/folders aready in the directory
            DirectoryCompare.ClearDirectory(OutputDirectory);

            SaveDirectoryDifferences();

            var updateString = GetDatabaseUpdateSQL();

            Console.WriteLine("Creating VB script");
            var vbString = new VBUpdateScript(73, "CustomBundle", updateString).VBString;
            File.WriteAllText(Path.Combine(OutputDirectory, OutputName, "BuildBundle.vb"), vbString);

            // Turn it into a zip file for manual updates
            ZipBundle();

            Console.ReadKey(true);
        }
        
        private static void SaveDirectoryDifferences()
        {
            var directoryCompare = new DirectoryCompare(OriginDirectoryPath, TargetDirectoryPath, true);
            
            
            foreach (var file in directoryCompare.Differences) {
                Console.WriteLine($"File Diff: {DirectoryCompare.GetRelativePath(file.FullName, TargetDirectoryPath)}");
            }

            var releaseFilePath = Path.Combine(OutputDirectory, OutputName, "ReleaseFiles");

            directoryCompare.CopyDifferencesTo(releaseFilePath);
            Console.WriteLine($"Changes saved to {releaseFilePath}");
        }

        private static string GetDatabaseUpdateSQL()
        {
            var sourceConnection = new ConnectionProperties(@"supportwizard\anthony2012", "MEXDB_Patheon_Live", "sa", "Admin123");
            var targetConnection = new ConnectionProperties(@"supportwizard\anthony2012", "MEXDB_Patheon_Testing", "sa", "Admin123");
            //var targetConnection = new ConnectionProperties("devsvr", "MEX_DEV_15_Build_73", "sa", "Admin123");

            using (SQLCompare sqlCompare = new SQLCompare(sourceConnection, targetConnection)) {
                File.WriteAllText(Path.Combine(OutputDirectory, @"UpgradeSQL.sql"), sqlCompare.UpdateScript);
                // Write the snapshot
                sqlCompare.WriteDatabaseTo(OutputDirectory, $"DatabaseSnapshot_{DateTime.Now:yyyy-MM-dd}.snp");

                Console.WriteLine("Finished SQL Compare.");
                return sqlCompare.UpdateScript;
            }
        }

        private static void ZipBundle()
        {
            Console.WriteLine("Zipping Files");

            var sourceDirectory = Path.Combine(OutputDirectory, OutputName);
            var targetPath = Path.Combine(OutputDirectory, $"{OutputName}.zip");

            ZipFile.CreateFromDirectory(sourceDirectory, targetPath, CompressionLevel.Optimal, true);

            Console.WriteLine("Finished Zipping Files.");
        }
    }
}
