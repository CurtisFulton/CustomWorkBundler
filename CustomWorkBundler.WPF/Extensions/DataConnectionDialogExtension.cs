using Microsoft.Data.ConnectionUI;
using System;
using System.Reflection;

namespace CustomWorkBundler.WPF.Extensions
{
    public static class DataConnectionDialogExtension
    {
        public static void SetProperty(this DataConnectionDialog dialog, string propertyName, object value)
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

        public static string GetProperty(this DataConnectionDialog dialog, string propertyName) => GetProperty<string>(dialog, propertyName);
        public static T GetProperty<T>(this DataConnectionDialog dialog, string propertyName)
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

        private static void SetPropertyValue(string propertyName, object target, object value, BindingFlags bindingFlags)
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
    }
}