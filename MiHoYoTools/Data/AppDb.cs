using Microsoft.Data.Sqlite;

namespace MiHoYoTools.Data
{
    public sealed class AppDb
    {
        private readonly string _dbPath;

        public AppDb(string dbPath)
        {
            _dbPath = dbPath;
        }

        public SqliteConnection Open()
        {
            var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            return connection;
        }
    }
}

