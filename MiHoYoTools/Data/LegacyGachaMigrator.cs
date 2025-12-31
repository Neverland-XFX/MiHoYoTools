using MiHoYoTools.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MiHoYoTools.Data
{
    public static class LegacyGachaMigrator
    {
        private const string MigrationKey = "gacha_migrated_v1";

        public static void Run()
        {
            if (IsMigrated())
            {
                return;
            }

            MigrateStarRail();
            MigrateZenless();
            MarkMigrated();
        }

        private static bool IsMigrated()
        {
            var db = new AppDb(AppPaths.DatabasePath);
            using var connection = db.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT value FROM db_meta WHERE key = $key;";
            command.Parameters.AddWithValue("$key", MigrationKey);
            var value = command.ExecuteScalar() as string;
            return value == "1";
        }

        private static void MarkMigrated()
        {
            var db = new AppDb(AppPaths.DatabasePath);
            using var connection = db.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO db_meta (key, value) VALUES ($key, $value);";
            command.Parameters.AddWithValue("$key", MigrationKey);
            command.Parameters.AddWithValue("$value", "1");
            command.ExecuteNonQuery();
        }

        private static void MigrateStarRail()
        {
            var recordsPath = Path.Combine(AppPaths.GetLegacyGameRoot(GameType.StarRail), "GachaRecords");
            if (!Directory.Exists(recordsPath))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(recordsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<Depend.GachaModel.GachaData>(json);
                    GachaRepository.SyncFromStarRail(data);
                }
                catch (Exception)
                {
                    // Ignore malformed legacy files to keep migration resilient.
                }
            }
        }

        private static void MigrateZenless()
        {
            var recordsPath = Path.Combine(AppPaths.GetLegacyGameRoot(GameType.ZenlessZoneZero), "GachaRecords");
            if (!Directory.Exists(recordsPath))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(recordsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<Modules.Zenless.Depend.GachaModel.GachaData>(json);
                    GachaRepository.SyncFromZenless(data);
                }
                catch (Exception)
                {
                    // Ignore malformed legacy files to keep migration resilient.
                }
            }
        }
    }
}

