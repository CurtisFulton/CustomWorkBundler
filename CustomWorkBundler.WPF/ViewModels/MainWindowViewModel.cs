using CustomWorkBundler.Logic;
using CustomWorkBundler.Logic.Events;
using CustomWorkBundler.WPF.Commands;
using CustomWorkBundler.WPF.Properties;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CustomWorkBundler.WPF.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public BundleDetailsViewModel BundleDetails { get; private set; } = new BundleDetailsViewModel();
        public ICommand StartBundleCommand { get; private set; } 

        public string Progress { get; set; }
        
        public MainWindowViewModel()
        {
            var bundleCommand = new AsyncRelayCommand(() => this.BundleDetails.IsBundling, this.BundleDetails.StartBundleAsync);
            this.StartBundleCommand = bundleCommand;

            // Register listener for the progress changed event
            ProgressChangedEvent.RegisterListener((args) => this.Progress = args.ProgressMessage);
            this.Progress = "Waiting To Bundle...";

            this.SetUpUserSettings();
        }

        private void SetUpUserSettings()
        {
            // Initialize the settings if they are nul
            if (Settings.Default.OutputPaths == null)
                Settings.Default.OutputPaths = new System.Collections.Generic.List<string>();
            if (Settings.Default.SourcePaths == null)
                Settings.Default.SourcePaths = new System.Collections.Generic.List<string>();
            if (Settings.Default.TargetPaths == null)
                Settings.Default.TargetPaths = new System.Collections.Generic.List<string>();

            // Clear out any directories that no longer exist
            Settings.Default.OutputPaths = Settings.Default.OutputPaths.Where(path => Directory.Exists(path)).ToList();
            Settings.Default.OutputPaths = Settings.Default.OutputPaths.Where(path => Directory.Exists(path)).ToList();
            Settings.Default.OutputPaths = Settings.Default.OutputPaths.Where(path => Directory.Exists(path)).ToList();

            Settings.Default.Save();
        }
    }
}