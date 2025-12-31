using System;
using System.IO;

namespace MiHoYoTools.Core
{
    internal static class HelperSources
    {
        public const string HelperRepoOwner = "Neverland-XFX";
        public const string HelperRepoName = "MiHoYoTools";

        private const string LocalDependsFolder = "Depends";

        public static string TryGetLocalZip(string zipFileName)
        {
            string baseDirectory = AppContext.BaseDirectory;
            string directPath = Path.Combine(baseDirectory, zipFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }

            string baseDependsPath = Path.Combine(baseDirectory, LocalDependsFolder, zipFileName);
            if (File.Exists(baseDependsPath))
            {
                return baseDependsPath;
            }

            string appDependsPath = Path.Combine(AppPaths.Root, LocalDependsFolder, zipFileName);
            if (File.Exists(appDependsPath))
            {
                return appDependsPath;
            }

            return null;
        }
    }
}
