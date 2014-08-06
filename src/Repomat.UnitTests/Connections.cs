using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    public class Connections
    {
        static Connections()
        {
            string appDir = Path.Combine(Path.GetTempPath(), "\\GraniteOak\\Repomat\\Tests");
            if (!Directory.Exists(appDir))
            {
                Directory.CreateDirectory(appDir);
            }

            _sqliteDatabaseFile = Path.Combine(appDir, "testDb.db");
        }

        private static string _sqliteDatabaseFile;

        public static IDbConnection NewSqlConnection(bool open=true)
        {
            var conn = new SqlConnection(@"Server=.\SqlExpress;Database=scratch;User Id=repomat_test;Password=repomat_test");
            if (open) conn.Open();
            return conn;
        }

        public static IDbConnection NewInMemoryConnection(bool open=true)
        {
            var conn = new SQLiteConnection("Data Source=:memory:;Version=3");
            if (open) conn.Open();
            return conn;
        }

        public static IDbConnection NewSQLiteConnection(bool open=true)
        {
            var conn = new SQLiteConnection(string.Format("Data Source={0};Version=3", _sqliteDatabaseFile));
            if (open) conn.Open();
            return conn;
        }

        public static void DumpSqliteDatabase()
        {
            if (File.Exists(_sqliteDatabaseFile))
            {
                File.Delete(_sqliteDatabaseFile);
            }
        }
    }
}
