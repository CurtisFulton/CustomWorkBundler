using CustomWorkBundler.UI.Properties;
using Microsoft.Data.ConnectionUI;
using System.Reflection;
using System.Windows.Input;

namespace CustomWorkBundler.UI.ViewModel
{
    public class CustomConnectionViewModel : BaseViewModel
    {
        public string ConnectionString { get; set; }
        public ICommand ConnectionDialogCommand { get; set; }

        public CustomConnectionViewModel()
        {
            ConnectionDialogCommand = new RelayCommand(OpenDataConnectionDiaglog);
        }

        private void OpenDataConnectionDiaglog()
        {
            var sqlDataSource = new DataSource("MicrosoftSqlServer", "Microsoft SQL Server");
            sqlDataSource.Providers.Add(DataProvider.SqlDataProvider);
            var dialog = new DataConnectionDialog();
            
            dialog.DataSources.Add(sqlDataSource);
            dialog.SelectedDataProvider = DataProvider.SqlDataProvider;
            dialog.SelectedDataSource = sqlDataSource;

            SetDialogValues(dialog);


            var result = DataConnectionDialog.Show(dialog);
            if (result == System.Windows.Forms.DialogResult.OK && dialog.ConnectionString.Contains("Data Source=")) {
                ConnectionString = dialog.ConnectionString;

                SaveDialogValues(dialog);
            }
        }

        private void SetDialogValues(DataConnectionDialog dialog)
        {
            SetProperty(dialog, "UseWindowsAuthentication", false);

            SetProperty(dialog, "ServerName", Settings.Default.ServerName);
            SetProperty(dialog, "UserName", Settings.Default.UserName);
            SetProperty(dialog, "Password", Settings.Default.Password);
            SetProperty(dialog, "DatabaseName", Settings.Default.DatabaseName);

            SetProperty(dialog, "SavePassword", Settings.Default.SavePassword);
        }

        private void SaveDialogValues(DataConnectionDialog dialog)
        {
            // Should probably make this store the username/password stuff per server name. But fuck it yolo
            Settings.Default.ServerName = GetProperty<string>(dialog, "ServerName");
            Settings.Default.UserName = GetProperty<string>(dialog, "UserName");
            Settings.Default.DatabaseName = GetProperty<string>(dialog, "DatabaseName");
            Settings.Default.SavePassword = GetProperty<bool>(dialog, "SavePassword");

            // If save password isn't set, set the password to be blank
            if (Settings.Default.SavePassword) 
                Settings.Default.Password = GetProperty<string>(dialog, "Password");
             else 
                Settings.Default.Password = "";
            
            Settings.Default.Save();
        }

        #region Dialog Reflection Helpers

        private void SetProperty(DataConnectionDialog dialog, string propertyName, object value)
        {
            var control = GetPropertyValue("ConnectionUIControl", dialog, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
            if (control == null) {
                return;
            }

            var properties = GetPropertyValue("Properties", control, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.DeclaredOnly);
            if (properties == null) {
                return;
            }

            SetPropertyValue(propertyName, properties, value, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
        }

        private T GetProperty<T>(DataConnectionDialog dialog, string propertyName)
        {
            var control = GetPropertyValue("ConnectionUIControl", dialog, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
            if (control == null) {
                return default(T);
            }

            var properties = GetPropertyValue("Properties", control, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.DeclaredOnly);
            if (properties == null) {
                return default(T);
            }

            var result = GetPropertyValue(propertyName, properties, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            if (result is T)
                return (T)result;

            return default(T);
        }

        private void SetPropertyValue(string propertyName, object target, object value, BindingFlags bindingFlags)
        {
            var propertyInfo = target.GetType().GetProperty(propertyName, bindingFlags);
            if (propertyInfo == null) {
                return;
            }

            propertyInfo.SetValue(target, value);
        }

        private static object GetPropertyValue(string propertyName, object target, BindingFlags bindingFlags)
        {
            var propertyInfo = target.GetType().GetProperty(propertyName, bindingFlags);
            if (propertyInfo == null) {
                return null;
            }

            return propertyInfo.GetValue(target, null);
        }

        #endregion
    }
}
