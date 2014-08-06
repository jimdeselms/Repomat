using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal class InsertMethodValidator : MethodValidator
    {
        public InsertMethodValidator(RepositoryDef repoDef, MethodDef methodDef, IList<ValidationError> errors)
            : base(repoDef, methodDef, errors)
        {
        }
    }
}
