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
    internal class SQLiteDataLayerBuilder : SqlServerDataLayerBuilder
    {
        public SQLiteDataLayerBuilder(IDbConnection connection) : base(connection)
        {
        }

        public SQLiteDataLayerBuilder(Func<IDbConnection> connectionFactory) : base(connectionFactory)
        {
        }

        // internal because protected will expose it to the outside.
        internal override RepositoryClassBuilder<TRepo> CreateClassBuilder<TType, TRepo>(RepositoryDef tableDef)
        {
            return new SQLiteRepositoryClassBuilder<TType, TRepo>(tableDef, NewConnectionEveryTime);
        }
    }
}
