using MiHoYoTools.Core;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using ZenlessGachaModel = MiHoYoTools.Modules.Zenless.Depend.GachaModel;

namespace MiHoYoTools.Data
{
    public static class GachaRepository
    {
        private static readonly object SyncLock = new object();

        public static void SyncFromStarRail(Depend.GachaModel.GachaData data)
        {
            if (data?.info == null || data.list == null)
            {
                return;
            }

            foreach (var pool in data.list)
            {
                var records = pool.records?.Select(record => new GachaRecordRow
                {
                    RecordId = record.id,
                    GachaId = record.gachaId,
                    ItemId = record.itemId,
                    Count = ParseInt(record.count),
                    Time = record.time,
                    Name = record.name,
                    ItemType = record.itemType,
                    RankType = record.rankType,
                    Lang = record.lang
                }) ?? Enumerable.Empty<GachaRecordRow>();

                UpsertRecords(GameType.StarRail, data.info.uid, pool.cardPoolId, pool.cardPoolType, records);
            }
        }

        public static void SyncFromZenless(Modules.Zenless.Depend.GachaModel.GachaData data)
        {
            if (data?.info == null || data.list == null)
            {
                return;
            }

            foreach (var pool in data.list)
            {
                var records = pool.records?.Select(record => new GachaRecordRow
                {
                    RecordId = record.id,
                    GachaId = null,
                    ItemId = record.itemId,
                    Count = ParseInt(record.count),
                    Time = record.time,
                    Name = record.name,
                    ItemType = record.itemType,
                    RankType = record.rankType,
                    Lang = record.lang
                }) ?? Enumerable.Empty<GachaRecordRow>();

                UpsertRecords(GameType.ZenlessZoneZero, data.info.uid, pool.cardPoolId, pool.cardPoolType, records);
            }
        }

        private static void UpsertRecords(
            GameType game,
            string uid,
            int poolId,
            string poolType,
            IEnumerable<GachaRecordRow> records)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                return;
            }

            lock (SyncLock)
            {
                var db = new AppDb(AppPaths.DatabasePath);
                using var connection = db.Open();
                using var transaction = connection.BeginTransaction();
                using var command = connection.CreateCommand();
                command.CommandText = @"
INSERT OR REPLACE INTO gacha_records
    (game, uid, pool_id, pool_type, gacha_id, item_id, count, time, name, item_type, rank_type, lang, record_id)
VALUES
    ($game, $uid, $pool_id, $pool_type, $gacha_id, $item_id, $count, $time, $name, $item_type, $rank_type, $lang, $record_id);";

                var gameParam = command.Parameters.Add("$game", SqliteType.Text);
                var uidParam = command.Parameters.Add("$uid", SqliteType.Text);
                var poolIdParam = command.Parameters.Add("$pool_id", SqliteType.Integer);
                var poolTypeParam = command.Parameters.Add("$pool_type", SqliteType.Text);
                var gachaIdParam = command.Parameters.Add("$gacha_id", SqliteType.Text);
                var itemIdParam = command.Parameters.Add("$item_id", SqliteType.Text);
                var countParam = command.Parameters.Add("$count", SqliteType.Integer);
                var timeParam = command.Parameters.Add("$time", SqliteType.Text);
                var nameParam = command.Parameters.Add("$name", SqliteType.Text);
                var itemTypeParam = command.Parameters.Add("$item_type", SqliteType.Text);
                var rankTypeParam = command.Parameters.Add("$rank_type", SqliteType.Text);
                var langParam = command.Parameters.Add("$lang", SqliteType.Text);
                var recordIdParam = command.Parameters.Add("$record_id", SqliteType.Text);

                foreach (var record in records)
                {
                    gameParam.Value = game.ToString();
                    uidParam.Value = uid;
                    poolIdParam.Value = poolId;
                    poolTypeParam.Value = poolType ?? string.Empty;
                    gachaIdParam.Value = record.GachaId ?? string.Empty;
                    itemIdParam.Value = record.ItemId ?? string.Empty;
                    countParam.Value = record.Count;
                    timeParam.Value = record.Time ?? string.Empty;
                    nameParam.Value = record.Name ?? string.Empty;
                    itemTypeParam.Value = record.ItemType ?? string.Empty;
                    rankTypeParam.Value = record.RankType ?? string.Empty;
                    langParam.Value = record.Lang ?? string.Empty;
                    recordIdParam.Value = record.RecordId ?? string.Empty;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public static void DeleteByGameUid(GameType game, string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                return;
            }

            lock (SyncLock)
            {
                var db = new AppDb(AppPaths.DatabasePath);
                using var connection = db.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM gacha_records WHERE game = $game AND uid = $uid;";
                command.Parameters.AddWithValue("$game", game.ToString());
                command.Parameters.AddWithValue("$uid", uid);
                command.ExecuteNonQuery();
            }
        }

        public static List<string> GetUids(GameType game)
        {
            var uids = new List<string>();
            lock (SyncLock)
            {
                var db = new AppDb(AppPaths.DatabasePath);
                using var connection = db.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT DISTINCT uid FROM gacha_records WHERE game = $game ORDER BY uid;";
                command.Parameters.AddWithValue("$game", game.ToString());
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        uids.Add(reader.GetString(0));
                    }
                }
            }
            return uids;
        }

        public static ZenlessGachaModel.GachaData GetZenlessGachaData(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                return null;
            }

            var data = new ZenlessGachaModel.GachaData
            {
                info = new ZenlessGachaModel.GachaInfo { uid = uid },
                list = new List<ZenlessGachaModel.GachaPool>()
            };

            lock (SyncLock)
            {
                var db = new AppDb(AppPaths.DatabasePath);
                using var connection = db.Open();
                using var command = connection.CreateCommand();
                command.CommandText = @"
SELECT pool_id, pool_type, item_id, count, time, name, item_type, rank_type, lang, record_id
FROM gacha_records
WHERE game = $game AND uid = $uid;";
                command.Parameters.AddWithValue("$game", GameType.ZenlessZoneZero.ToString());
                command.Parameters.AddWithValue("$uid", uid);

                using var reader = command.ExecuteReader();
                var pools = new Dictionary<int, ZenlessGachaModel.GachaPool>();
                while (reader.Read())
                {
                    var poolId = reader.GetInt32(0);
                    var poolType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                    if (!pools.TryGetValue(poolId, out var pool))
                    {
                        pool = new ZenlessGachaModel.GachaPool
                        {
                            cardPoolId = poolId,
                            cardPoolType = poolType,
                            records = new List<ZenlessGachaModel.GachaRecord>()
                        };
                        pools[poolId] = pool;
                    }
                    else if (string.IsNullOrWhiteSpace(pool.cardPoolType) && !string.IsNullOrWhiteSpace(poolType))
                    {
                        pool.cardPoolType = poolType;
                    }

                    var record = new ZenlessGachaModel.GachaRecord
                    {
                        gachaType = poolId.ToString(),
                        itemId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        count = reader.IsDBNull(3) ? "0" : reader.GetInt32(3).ToString(),
                        time = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        name = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        itemType = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        rankType = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        lang = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        id = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                    };
                    pool.records.Add(record);
                }

                foreach (var pool in pools.Values)
                {
                    if (string.IsNullOrWhiteSpace(pool.cardPoolType))
                    {
                        pool.cardPoolType = GetZenlessPoolType(pool.cardPoolId);
                    }

                    pool.records = pool.records
                        .OrderByDescending(r => ParseLong(r.id))
                        .ToList();
                    data.list.Add(pool);
                }
            }

            data.list = data.list
                .OrderBy(pool => GetZenlessPoolOrder(pool.cardPoolId))
                .ToList();

            return data;
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out var parsed) ? parsed : 0;
        }

        private static long ParseLong(string value)
        {
            return long.TryParse(value, out var parsed) ? parsed : 0;
        }

        private static int GetZenlessPoolOrder(int cardPoolId)
        {
            return cardPoolId switch
            {
                2 => 1,
                3 => 2,
                1 => 95,
                5 => 96,
                _ => 100
            };
        }

        private static string GetZenlessPoolType(int cardPoolId)
        {
            return cardPoolId switch
            {
                2 => "独家频段",
                3 => "音擎频段",
                1 => "常驻频段",
                5 => "邦布频段",
                _ => "未知频段"
            };
        }

        private sealed class GachaRecordRow
        {
            public string RecordId { get; set; }
            public string GachaId { get; set; }
            public string ItemId { get; set; }
            public int Count { get; set; }
            public string Time { get; set; }
            public string Name { get; set; }
            public string ItemType { get; set; }
            public string RankType { get; set; }
            public string Lang { get; set; }
        }
    }
}

