using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal class GetMethodValidator : MethodValidator
    {
        //TODO: Other validations
        // There can be either an IDbConnection or IDbTransaction, but not both
        // The parameter names must match the property names of the entity
        public GetMethodValidator(RepositoryDef repoDef, MethodDef methodDef, DatabaseType databaseType, IList<ValidationError> errors)
            : base(repoDef, methodDef, databaseType, errors)
        {
            AddValidators(
                ReturnTypeValidations,
                TryGetValidations);
        }

        private void ReturnTypeValidations()
        {
            if (!MethodDef.IsTryGet)
            {
                if (MethodDef.IsSingleton && MethodDef.ReturnType != MethodDef.EntityDef.Type)
                {
                    AddError("SingleGetReturnWrongType", "expected return type {0}, returns {1} instead",
                        MethodDef.EntityDef.Type.ToCSharp(),
                        MethodDef.ReturnType.ToCSharp());
                }
                else if (!MethodDef.IsSingleton && !MethodDef.ReturnType.ImplementsIEnumerableOfType(MethodDef.EntityDef.Type))
                {
                    AddError("MultiGetReturnWrongType", "expected enumerable return type {0}, returns {1} instead",
                        MethodDef.EntityDef.Type.ToCSharp(),
                        MethodDef.ReturnType.ToCSharp());
                }
            }
        }

        private void TryGetValidations()
        {
            if (MethodDef.MethodType == MethodType.Get && MethodDef.OutParameterOrNull != null)
            {
                if (MethodDef.ReturnType != typeof(bool))
                {
                    AddError("TryGetReturnWrongType", "expected return type bool, returns {0} instead",
                        MethodDef.ReturnType.ToCSharp());
                }

                if (MethodDef.OutParameterOrNull != null)
                {
                    if (MethodDef.OutParameterOrNull.Type.GetElementType() != MethodDef.EntityDef.Type)
                    {
                        AddError("TryGetOutParamWrongType", "expected out parameter of type {0}, out parameter of type {1} instead",
                            MethodDef.EntityDef.Type.ToCSharp(),
                            MethodDef.OutParameterOrNull.Type.GetElementType().ToCSharp());
                    }
                }
            }
        }

    }
}
