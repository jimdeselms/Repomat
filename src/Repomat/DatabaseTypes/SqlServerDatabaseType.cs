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
    internal class SqlServerDatabaseType : DatabaseType
    {
        public SqlServerDatabaseType()
            : base("SqlServer", supportsStoredProcedures: true)
        {
        }

        public override DataLayerBuilder CreateDataLayerBuilder(IDbConnection conn)
        {
            return new SqlServerDataLayerBuilder(conn);
        }

        public override DataLayerBuilder CreateDataLayerBuilder(Func<IDbConnection> conn)
        {
            return new SqlServerDataLayerBuilder(conn);
        }
    }
}
