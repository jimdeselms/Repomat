using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal class CustomMethodValidator : MethodValidator
    {
        public CustomMethodValidator(RepositoryDef repoDef, MethodDef methodDef, IList<ValidationError> errors)
            : base(repoDef, methodDef, errors)
        {
            AddValidators(EnsureMethodHasCustomSql);
        }

        private void EnsureMethodHasCustomSql()
        {
            if (MethodDef.CustomSqlOrNull == null)
            {
                AddError("CustomMethodWithoutSql", "Method looks custom, but does not have SQL defined. Call SetCustomSql() to define the SQL");
            }
        }
    }
}
