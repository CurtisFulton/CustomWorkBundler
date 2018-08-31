using System.Configuration;
using System.IO;
using System.Linq;

namespace CustomWorkBundler.UI.ViewModel
{
    public class ResourceSelectorViewModel : BaseViewModel
    {
        public string Title { get; set; }

        #region Folder Selection

        // Enum switch for which of the folder selection methods to use
        public SelectorType FolderSourceType { get; set; } = SelectorType.Standard;

        // Custom Folder 
        public FolderPickerViewModel FolderPicker { get; set; }

        // Previous Bundle Folder
        public int PreviousBundleFolderIndex { get; set; }
        public DirectoryInfo[] AllPreviousBundleFolders { get; set; }
        public DirectoryInfo SelectedPreviousBundleFolder => AllPreviousBundleFolders[PreviousBundleFolderIndex];

        // Standard Build Folder
        public int StandardFolderIndex { get; set; }
        public DirectoryInfo[] AllStandardBuildFolders { get; set; }
        public DirectoryInfo SelectedStandardBuildFolder => AllStandardBuildFolders[StandardFolderIndex];

        #endregion

        #region Database Connection Details

        // Enum switch for which of the connections to use
        public SelectorType DatabaseSourceType { get; set; } = SelectorType.Custom;

        public CustomConnectionViewModel CustomConnectionViewModel { get; private set; }
        
        // Previous Bundle Connection
        public int PreviousBundleDatabaseIndex { get; set; }
        public FileInfo[] AllPreviousBundleDatabases { get; set; }
        public FileInfo SelectedPreviousBundleDatabase => AllPreviousBundleDatabases[PreviousBundleDatabaseIndex];

        // Standard Build Connection
        public int StandardDatabaseIndex { get; set; }
        public FileInfo[] AllStandardDatabases { get; set; }
        public FileInfo SelectedBuild => AllStandardDatabases[StandardDatabaseIndex];

        #endregion

        public ResourceSelectorViewModel(string title)
        {
            Title = title;
            FolderPicker = new FolderPickerViewModel("Root Directory");

            CustomConnectionViewModel = new CustomConnectionViewModel();
            
            AllPreviousBundleDatabases = GetAllPreviousDatabases();
            AllStandardDatabases = GetAllStandardDatabases();
            
            AllPreviousBundleFolders = GetAllPreviousWebFiles();
            AllStandardBuildFolders = GetAllStandardWebFiles();
        }

        private FileInfo[] GetAllStandardDatabases()
        {
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Databases")))
                return null;

            var databasesRoot = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Databases"));
            
            var allSnapshots = databasesRoot.GetFiles("*.snp");
            return allSnapshots.OrderByDescending(f => f.Name).ToArray();
        }

        private FileInfo[] GetAllPreviousDatabases()
        {
            var rootPath = ConfigurationManager.AppSettings["RootStorageDirectory"];

            // TODO: Actually do this
            var allSnapshots = new FileInfo[] { };

            return allSnapshots.OrderByDescending(f => f.Name).ToArray();
        }

        private DirectoryInfo[] GetAllPreviousWebFiles()
        {
            var rootPath = ConfigurationManager.AppSettings["RootStorageDirectory"];

            // TODO: Actually do this
            var allDirectories = new DirectoryInfo[] { };

            return allDirectories.OrderByDescending(f => f.Name).ToArray();
        }

        private DirectoryInfo[] GetAllStandardWebFiles()
        {
            var webFilesRoot = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "WebFiles"));
            
            var allWebFiles = webFilesRoot.GetDirectories();

            return allWebFiles.OrderByDescending(f => f.Name).ToArray();
        }
    }
}
