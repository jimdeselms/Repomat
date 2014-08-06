using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    public class SqlHelper : DbHelper
    {
        public SqlHelper() : base(Connections.NewSqlConnection())
        {
        }

        public override bool TableExists(string tableName)
        {
            var count = ExecuteScalar<int>(
                "if exists (select 1 from information_schema.tables where table_type='BASE TABLE' and table_name=@t) SELECT 1 ELSE SELECT 0",
                new { t = tableName });

            return count == 1;
        }
    }
}
