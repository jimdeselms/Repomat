using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    internal class SqliteHelper : DbHelper
    {
        public SqliteHelper(IDbConnection conn=null) : base(conn ?? Connections.NewInMemoryConnection())
        {
        }

        public override bool TableExists(string tableName)
        {
            var count = ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@t",
                new { t = tableName });

            return count == 1;
        }
    }
}
