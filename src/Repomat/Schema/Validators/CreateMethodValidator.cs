using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal class CreateMethodValidator : MethodValidator
    {
        public CreateMethodValidator(RepositoryDef repoDef, MethodDef methodDef, DatabaseType databaseType, IList<ValidationError> errors)
            : base(repoDef, methodDef, databaseType, errors)
        {
            AddValidators(FailIfBothCreateAndInsertExist);
        }

        private void FailIfBothCreateAndInsertExist()
        {
            if (RepoDef.Methods.Any(r => r.MethodType == MethodType.Insert))
            {
                AddError("BothCreateAndInsert", "Create and Insert methods are mutually exclusive. Please choose one");
            }
        }
    }
}
