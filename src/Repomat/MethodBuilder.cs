using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat
{
    public class MethodBuilder
    {
        private readonly MethodDef _method;
        private readonly RepositoryDef _repositoryDef;
        private readonly DatabaseType _databaseType;

        internal MethodBuilder(MethodDef method, RepositoryDef repoDef, DatabaseType databaseType)
        {
            _method = method;
            _repositoryDef = repoDef;
            _databaseType = databaseType;
        }

        public MethodBuilder ExecutesSql(string sql)
        {
            _method.CustomSqlOrNull = sql;
            _method.CustomSqlIsStoredProcedure = false;
            return this;
        }

        public MethodBuilder SetEntityType(Type type)
        {
            _method.EntityDef = _repositoryDef.GetEntityDef(type);

            return this;
        }

        public MethodBuilder ExecutesStoredProcedure(string procName = null)
        {
            if (!_databaseType.SupportsStoredProcedures)
            {
                throw new RepomatException("Stored procedures not supported in {0}", _databaseType.Name);
            }
            if (procName == null)
            {
                procName = _method.MethodName;
            }

            _method.CustomSqlOrNull = procName;
            _method.CustomSqlIsStoredProcedure = true;

            return this;
        }

        public MethodBuilder SetMethodType(MethodType methodType)
        {
            _method.MethodType = methodType;

            return this;
        }

        public MethodBuilder SetSingletonGetMethodBehavior(SingletonGetMethodBehavior behavior)
        {
            // TODO: fail if the method is not actually a singleton get method.
            _method.SingletonGetMethodBehavior = behavior;

            return this;
        }
    }
}
