using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;
using Repomat.CodeGen;
using Repomat.Schema;

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
    }
}
