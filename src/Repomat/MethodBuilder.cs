using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Databases
{
    public class MethodBuilder
    {
        private readonly MethodDef _method;

        internal MethodBuilder(MethodDef method)
        {
            _method = method;
        }

        public MethodBuilder SetCustomSql(string sql)
        {
            _method.CustomSqlOrNull = sql;

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
