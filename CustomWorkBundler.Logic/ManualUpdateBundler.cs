using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace CustomWorkBundler.Logic
{
    public class ManualUpdateBundler
    {
        private const string VBTemplateName = "VbTemplate.vb";
        private readonly CompressionLevel CompressionLevel = CompressionLevel.Optimal;

        private string BundleName { get; set; }
        private double Build { get; set; }

        public ManualUpdateBundler(string bundleName, double build)
        {
            this.BundleName = bundleName;
            this.Build = build;
        }

        public async Task CreateBundleAsync(string outputPath, string rootPath, string[] changedFiles, string dbUpdateScript)
        {
            var zipPath = Path.Combine(outputPath, $"{this.BundleName}.zip");
            
            await Task.Run(() => {
                using (var fileStream = new FileStream(zipPath, FileMode.CreateNew)) {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true)) {
                        // Write all the files to the archive
                        //await WriteFilesToArchiveAsync(archive, changedFiles, rootPath);
                        WriteFilesToArchive(archive, changedFiles, rootPath);

                        // Create a new archive entry for the vb script
                        var vbScriptEntry = archive.CreateEntry($"{this.BundleName}.vb", this.CompressionLevel);
                        using (var vbWriter = new StreamWriter(vbScriptEntry.Open())) {
                            // Write the Vb script to the archive entry
                            vbWriter.Write(GetVbBundleScript(dbUpdateScript));
                        }
                    }
                }
            });
        }

        private void WriteFilesToArchive(ZipArchive archive, string[] files, string rootPath)
        {
            if (files == null || files.Length == 0)
                return;
            
            for (int i = 0; i < files.Length; i++) {
                var file = files[i];

                // Relative Path for this file
                var relativePath = DirectoryCompare.GetRelativePath(file, rootPath);

                // Make the file in form "<BundleName>/ReleaseFiles/<RelativePath>" for the archive
                var archiveFilePath = Path.Combine(this.BundleName, "ReleaseFiles", relativePath);
                archive.CreateEntryFromFile(file, archiveFilePath, this.CompressionLevel);
            }
        }

        private string GetVbBundleScript(string sqlScript)
        {
            var vbString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), VBTemplateName));

            // Replace the bundle name
            vbString = vbString.Replace("CustomBundle", this.BundleName);

            // Update the Web Config Build Number
            var buildTag = "Settings(\"CurrentBuild\").Value =";
            var buildIndex = vbString.IndexOf(buildTag) + buildTag.Length;
            vbString = vbString.Insert(buildIndex, $"\"{this.Build}\"");
            
            // Add the line to update the database build
            sqlScript += Environment.NewLine;
            sqlScript += $"UPDATE SystemOption SET DatabaseBuild = '{this.Build}'";

            // Add SQL script
            var sqlTag = @"<![CDATA[";
            var sqlIndex = vbString.IndexOf(sqlTag) + sqlTag.Length;
            vbString = vbString.Insert(sqlIndex, Environment.NewLine + sqlScript);

            return vbString;
        }
    }
}