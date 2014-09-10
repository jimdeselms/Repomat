using System;
using System.Linq;
using System.Collections.Generic;

namespace Repomat.Schema.Validators
{
    internal class RepositoryDefValidator : ValidatorBase
    {
        public RepositoryDefValidator(RepositoryDef repoDef, DatabaseType databaseType)
            : base(repoDef, databaseType, new List<ValidationError>())
        {
            AddValidators(OnlyOneSingletonGetAllowed);
            AddMethodValidations();
        }

        public void AddMethodValidations()
        {
            foreach (var method in RepoDef.Methods)
            {
                var validator = MethodValidatorFactory.Create(RepoDef, method, DatabaseType, ErrorList);
                AddValidators(validator.Validators.ToArray());
            }
        }

        private void OnlyOneSingletonGetAllowed()
        {
            foreach (var entityDef in RepoDef.GetEntityDefs())
            {
                if (!entityDef.HasExplicitPrimaryKey)
                {
                    // Get all the singleton get methods.
                    var singletonGets = RepoDef.Methods.Where(m => m.EntityDef == entityDef && m.MethodType == MethodType.Get && m.IsSingleton).ToArray();
                    {
                        for (int i = 0; i < singletonGets.Length; i++)
                        {
                            for (int j = i+1; j < singletonGets.Length; j++)
                            {
                                if (GetMethodHashCode(singletonGets[i]) != GetMethodHashCode(singletonGets[j]))
                                {
                                    AddError("DupePrimaryKey", "Entity with more than one singleton get. call UsesPrimaryKey() to define primary key.");
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private int GetMethodHashCode(MethodDef method)
        {
            int result = 0;
            foreach (var parameter in method.Parameters.Where(p => !p.IsOut))
            {
                result = result << 1;
                result = result ^ (parameter.Type.GetHashCode()<< 1) ^ parameter.Type.Name.GetHashCode();
            }
            return result;
        }
    }
}
