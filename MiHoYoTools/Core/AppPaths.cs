using System;
using System.IO;

namespace MiHoYoTools.Core
{
    public static class AppPaths
    {
        private const string VendorFolder = "JSG-LLC";
        private const string AppFolder = "MiHoYoTools";

        public static string DocumentsRoot => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string Root => Path.Combine(DocumentsRoot, VendorFolder, AppFolder);

        public static string DatabasePath => Path.Combine(Root, "mhytools.db");
        public static string Logs => Path.Combine(Root, "Logs");
        public static string Cache => Path.Combine(Root, "Cache");
        public static string Exports => Path.Combine(Root, "Exports");
        public static string Extras => Path.Combine(Root, "Extras");

        public static void EnsureFolders()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Logs);
            Directory.CreateDirectory(Cache);
            Directory.CreateDirectory(Exports);
            Directory.CreateDirectory(Extras);
        }

        public static string GetGameRoot(GameType game) => Path.Combine(Root, GetGameFolderName(game));

        public static string GetLegacyGameRoot(GameType game)
        {
            return Path.Combine(DocumentsRoot, VendorFolder, GetLegacyGameFolderName(game));
        }

        public static string ResolveGameRoot(GameType game)
        {
            var preferred = GetGameRoot(game);
            var legacy = GetLegacyGameRoot(game);
            if (Directory.Exists(preferred))
            {
                return preferred;
            }

            if (Directory.Exists(legacy))
            {
                return legacy;
            }

            return preferred;
        }

        public static string ResolveGameFolder(GameType game, string relativePath)
        {
            var root = ResolveGameRoot(game);
            return string.IsNullOrWhiteSpace(relativePath) ? root : Path.Combine(root, relativePath);
        }

        public static string ResolveGameFile(GameType game, string relativePath, string fileName)
        {
            var folder = ResolveGameFolder(game, relativePath);
            return Path.Combine(folder, fileName);
        }

        public static string ResolveGameRootRelative(GameType game)
        {
            var preferred = Path.Combine(VendorFolder, AppFolder, GetGameFolderName(game));
            var legacy = Path.Combine(VendorFolder, GetLegacyGameFolderName(game));
            var preferredAbs = Path.Combine(DocumentsRoot, preferred);
            var legacyAbs = Path.Combine(DocumentsRoot, legacy);

            if (Directory.Exists(preferredAbs))
            {
                return preferred;
            }

            if (Directory.Exists(legacyAbs))
            {
                return legacy;
            }

            return preferred;
        }

        public static string ResolveGameFolderRelative(GameType game, string relativePath)
        {
            var root = ResolveGameRootRelative(game);
            return string.IsNullOrWhiteSpace(relativePath) ? root : Path.Combine(root, relativePath);
        }

        private static string GetGameFolderName(GameType game)
        {
            return game switch
            {
                GameType.StarRail => "StarRail",
                GameType.ZenlessZoneZero => "ZenlessZoneZero",
                _ => throw new ArgumentOutOfRangeException(nameof(game), game, "Unsupported game type.")
            };
        }

        private static string GetLegacyGameFolderName(GameType game)
        {
            return game switch
            {
                GameType.StarRail => "SRTools",
                GameType.ZenlessZoneZero => "ZenlessTools",
                _ => throw new ArgumentOutOfRangeException(nameof(game), game, "Unsupported game type.")
            };
        }
    }
}

