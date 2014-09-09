using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.CodeGen;
using Repomat.Schema;
using Repomat.IlGen;

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

        internal override RepoSqlBuilder CreateRepoSqlBuilder(RepositoryDef repoDef, bool newConnectionEveryTime)
        {
            return new SqlServerRepoSqlBuilder(repoDef, newConnectionEveryTime);
        }
    }
}
