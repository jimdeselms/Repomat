using Repomat.CodeGen;
using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Databases
{
    internal abstract class SqlDataLayerBuilder : DataLayerBuilder
    {
        private readonly IDbConnection _connectionOrNull;
        private readonly Func<IDbConnection> _connectionFuncOrNull;

        protected SqlDataLayerBuilder(IDbConnection conn, DatabaseType databaseType)
            : base(databaseType)
        {
            _connectionOrNull = conn;
            _connectionFuncOrNull = null;
        }

        protected SqlDataLayerBuilder(Func<IDbConnection> conn, DatabaseType databaseType)
            : base(databaseType)
        {
            _connectionOrNull = null;
            _connectionFuncOrNull = conn;
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
