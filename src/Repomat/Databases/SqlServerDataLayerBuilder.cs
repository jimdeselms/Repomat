using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.CodeGen;
using Repomat.Schema;

namespace Repomat.Databases
{
    internal class SqlServerDataLayerBuilder : SqlDataLayerBuilder
    {
        public SqlServerDataLayerBuilder(IDbConnection connection) : base(connection, DatabaseType.SqlServer)
        {
        }

        public SqlServerDataLayerBuilder(Func<IDbConnection> connectionFactory) : base(connectionFactory, DatabaseType.SqlServer)
        {
        }

        // Internal because protected will expose it to the outside
        internal override RepositoryClassBuilder CreateClassBuilder(RepositoryDef tableDef)
        {
            return new SqlServerRepositoryClassBuilder(tableDef, NewConnectionEveryTime);
        }
    }
}
