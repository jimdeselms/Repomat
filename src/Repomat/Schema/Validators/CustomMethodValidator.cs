using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal class CustomMethodValidator : MethodValidator
    {
        public CustomMethodValidator(RepositoryDef repoDef, MethodDef methodDef, DatabaseType databaseType, IList<ValidationError> errors)
            : base(repoDef, methodDef, databaseType, errors)
        {
            AddValidators(
                EnsureMethodHasCustomSql,
                FailIfDatabaseDoesntSupportProcs);
        }

        private void EnsureMethodHasCustomSql()
        {
            if (MethodDef.CustomSqlOrNull == null)
            {
                AddError("CustomMethodWithoutSql", "Method looks custom, but does not have SQL defined. Call SetCustomSql() to define the SQL");
            }
        }

        private void FailIfDatabaseDoesntSupportProcs()
        {
            if (MethodDef.CustomSqlIsStoredProcedure && !DatabaseType.SupportsStoredProcedures)
            {
                AddError("DbDoesntSupportProcs", "Database type {0} does not support stored procedures", DatabaseType.Name);
            }
        }
    }
}
