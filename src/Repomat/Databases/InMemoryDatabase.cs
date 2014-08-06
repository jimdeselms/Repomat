using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;

namespace Repomat.Databases
{
    internal static class InMemoryDatabase
    {
        public static DataLayerBuilder Create()
        {
            var conn = new SQLiteConnection("Data Source=:memory:;Version=3");
            conn.Open();

            return new SQLiteDataLayerBuilder(conn);
        }
    }
}
