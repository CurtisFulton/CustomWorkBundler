using CustomWorkBundler.WPF.Commands;
using CustomWorkBundler.WPF.Properties;
using CustomWorkBundler.WPF.Extensions;
using Microsoft.Data.ConnectionUI;
using System.Windows.Input;

namespace CustomWorkBundler.WPF.ViewModels
{
    public class ConnectionStringViewModel : BaseViewModel
    {
        public string ConnectionString { get; set; }
        public ICommand OpenConnectionDialogCommand { get; set; }

        public ConnectionStringViewModel()
        {
            this.OpenConnectionDialogCommand = new RelayCommand(this.OpenConnectionDialog);
        }

        private void OpenConnectionDialog()
        {
            // Setup Datasource to be SQL Server
            var sqlDataSource = new DataSource("MicrosoftSqlServer", "Microsoft SQL Server");
            sqlDataSource.Providers.Add(DataProvider.SqlDataProvider);

            // Setup SQL data provider for Dialog
            var dialog = new DataConnectionDialog();
            dialog.DataSources.Add(sqlDataSource);
            dialog.SelectedDataProvider = DataProvider.SqlDataProvider;
            dialog.SelectedDataSource = sqlDataSource;

            // Set's the values in the dialog from user settings
            SetDialogValues(dialog);

            // Display the Dialog
            var result = DataConnectionDialog.Show(dialog);

            if (result == System.Windows.Forms.DialogResult.OK && dialog.ConnectionString.Contains("Data Source=")) {
                this.ConnectionString = dialog.ConnectionString;

                // Save the values back to user settings
                SaveDialogValues(dialog);
            }
        }

        private void SetDialogValues(DataConnectionDialog dialog)
        {
            // Always set WindowsAugentication to false
            dialog.SetProperty("UseWindowsAuthentication", false);

            dialog.SetProperty("ServerName", Settings.Default.ServerName);
            dialog.SetProperty("UserName", Settings.Default.UserName);
            dialog.SetProperty("Password", Settings.Default.Password);
            dialog.SetProperty("DatabaseName", Settings.Default.DatabaseName);

            dialog.SetProperty("SavePassword", Settings.Default.SavePassword);
        }

        private void SaveDialogValues(DataConnectionDialog dialog)
        {
            // Should probably make this store the username/password stuff per server name. But yolo
            Settings.Default.ServerName = dialog.GetProperty("ServerName");
            Settings.Default.UserName = dialog.GetProperty("UserName");
            Settings.Default.DatabaseName = dialog.GetProperty("DatabaseName");
            Settings.Default.SavePassword = dialog.GetProperty<bool>("SavePassword");

            // If save password isn't set, set the password to be blank
            if (Settings.Default.SavePassword)
                Settings.Default.Password = dialog.GetProperty("Password");
            else
                Settings.Default.Password = "";

            Settings.Default.Save();
        }
    }
}