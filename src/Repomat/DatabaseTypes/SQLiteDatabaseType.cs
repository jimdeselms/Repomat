using Repomat.CodeGen;
using Repomat.Databases;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.DatabaseTypes
{
    internal class SQLiteDatabaseType : DatabaseType
    {
        public SQLiteDatabaseType() : base("SQLite", supportsStoredProcedures: false)
        {
        }

        public override DataLayerBuilder CreateDataLayerBuilder(IDbConnection conn)
        {
            return new SQLiteDataLayerBuilder(conn);
        }

        public override DataLayerBuilder CreateDataLayerBuilder(Func<IDbConnection> conn)
        {
            return new SQLiteDataLayerBuilder(conn);
        }
    }
}
