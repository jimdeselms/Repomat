using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;
using Repomat.CodeGen;
using Repomat.Schema;
using Repomat.IlGen;

namespace Repomat.Databases
{
    internal class SQLiteDataLayerBuilder : SqlDataLayerBuilder
    {
        public SQLiteDataLayerBuilder(IDbConnection connection) : base(connection, DatabaseType.SQLite)
        {
        }

        public SQLiteDataLayerBuilder(Func<IDbConnection> connectionFactory) : base(connectionFactory, DatabaseType.SQLite)
        {
        }

        // internal because protected will expose it to the outside.
        internal override RepositoryClassBuilder CreateClassBuilder(RepositoryDef tableDef)
        {
            return new SQLiteRepositoryClassBuilder(tableDef, NewConnectionEveryTime);
        }

        internal override RepoSqlBuilder CreateRepoSqlBuilder(RepositoryDef repoDef, bool newConnectionEveryTime, IlGen.RepoConnectionType repoConnectionType)
        {
            return new SQLiteRepoSqlBuilder(repoDef, newConnectionEveryTime, repoConnectionType);
        }
    }
}
