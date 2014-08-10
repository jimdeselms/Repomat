using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal class RepositoryDefValidator
    {
        private DatabaseType _databaseType;

        public RepositoryDefValidator(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        public IReadOnlyList<ValidationError> Validate(RepositoryDef repoDef)
        {
            List<ValidationError> errors = new List<ValidationError>();

            foreach (var method in repoDef.Methods)
            {
                var validator = MethodValidatorFactory.Create(repoDef, method, _databaseType, errors);
                validator.Validate();
            }

            return errors;
        }
    }
}
