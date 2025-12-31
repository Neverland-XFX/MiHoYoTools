using System;
using System.Reflection;
using Windows.ApplicationModel;

namespace MiHoYoTools.Core
{
    public static class AppInfoHelper
    {
        public static string GetDisplayName()
        {
            try
            {
                return Package.Current.DisplayName;
            }
            catch
            {
                return Assembly.GetExecutingAssembly().GetName().Name ?? "MiHoYoTools";
            }
        }

        public static Version GetVersion()
        {
            try
            {
                var packageVersion = Package.Current.Id.Version;
                return new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            }
            catch
            {
                return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
            }
        }

        public static string GetVersionString()
        {
            var version = GetVersion();
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
