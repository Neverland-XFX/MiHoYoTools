using MiHoYoTools.Core;
using Microsoft.Data.Sqlite;

namespace MiHoYoTools.Data
{
    public static class DbInitializer
    {
        public static void Initialize()
        {
            SQLitePCL.Batteries_V2.Init();
            AppPaths.EnsureFolders();

            var db = new AppDb(AppPaths.DatabasePath);
            using var connection = db.Open();
            using var command = connection.CreateCommand();

            command.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS db_meta (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT
);

CREATE TABLE IF NOT EXISTS accounts (
    game TEXT NOT NULL,
    uid TEXT NOT NULL,
    name TEXT,
    is_legacy INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT,
    PRIMARY KEY (game, uid)
);

CREATE TABLE IF NOT EXISTS gacha_records (
    game TEXT NOT NULL,
    uid TEXT NOT NULL,
    pool_id INTEGER NOT NULL,
    pool_type TEXT,
    gacha_id TEXT,
    item_id TEXT,
    count INTEGER,
    time TEXT,
    name TEXT,
    item_type TEXT,
    rank_type TEXT,
    lang TEXT,
    record_id TEXT NOT NULL,
    PRIMARY KEY (game, uid, pool_id, record_id)
);

CREATE INDEX IF NOT EXISTS idx_gacha_uid ON gacha_records(uid);
CREATE INDEX IF NOT EXISTS idx_gacha_game_uid ON gacha_records(game, uid);
";
            command.ExecuteNonQuery();
        }
    }
}

