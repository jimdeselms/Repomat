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
    internal class SqlServerDataLayerBuilder : DataLayerBuilder
    {
        private readonly IDbConnection _connectionOrNull;
        private readonly Func<IDbConnection> _connectionFuncOrNull;

        public SqlServerDataLayerBuilder(Func<IDbConnection> connectionFactory)
            : this(connectionFactory, null)
        {
            _connectionFuncOrNull = connectionFactory;
        }

        public SqlServerDataLayerBuilder(IDbConnection connection) : this(null, connection)
        {
            _connectionOrNull = connection;
        }

        private SqlServerDataLayerBuilder(Func<IDbConnection> connectionFactory, IDbConnection connection)
        {
        }

        protected bool NewConnectionEveryTime { get { return _connectionOrNull == null; } }

        // Internal because protected will expose it to the outside
        internal override RepositoryClassBuilder CreateClassBuilder(RepositoryDef tableDef)
        {
            return new SqlServerRepositoryClassBuilder(tableDef, NewConnectionEveryTime);
        }

        // internal because protected will expose it to the outside.
        internal override object CreateRepoInstance(Type repoClass, RepositoryDef tableDef)
        {
            if (_connectionOrNull != null)
            {
                var ctor = repoClass.GetConstructor(new[] { typeof(IDbConnection) });
                return ctor.Invoke(new object[] { _connectionOrNull });
            }
            else
            {
                var ctor = repoClass.GetConstructor(new[] { typeof(Func<IDbConnection>) });
                return ctor.Invoke(new object[] { _connectionFuncOrNull });
            }
        }
    }
}
