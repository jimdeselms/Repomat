using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal abstract class MethodValidator : ValidatorBase
    {
        private readonly MethodDef _methodDef;

        public MethodValidator(RepositoryDef repoDef, MethodDef methodDef, DatabaseType databaseType, IList<ValidationError> errors)
            : base(repoDef, databaseType, errors)
        {
            _methodDef = methodDef;

            AddValidators(
                ValidateMethodWithParameterThatDoesntMapToProperty,
                ValidateMethodWithParameterDifferentTypeThanProperty,
                ValidateNonCustomMethodHasEntityDefined);
        }

        protected override void AddError(string errorCode, string format, params object[] args)
        {
            string firstPart = string.Format("{0} method {1} on {2}: ", MethodDef.MethodType, MethodDef.NameAndArgumentList, RepoDef.RepositoryType.ToCSharp());
            base.AddError(errorCode, firstPart + format, args);
        }

        private void ValidateMethodWithParameterThatDoesntMapToProperty()
        {
            // Custom methods can have any kind of parameters at all.
            if (MethodDef.MethodType != MethodType.Custom)
            {
                foreach (var argument in MethodDef.Properties)
                {
                    PropertyDef matchingPropertyOrNull;
                    if (!TryGetPropertyFromArgumentName(argument.Name, out matchingPropertyOrNull))
                    {
                        AddError("ParameterDoesntHaveProperty",
                                 "found parameter {0} that does not map to a settable property {1}",
                                 argument.Name,
                                 argument.Name.Capitalize(),
                                 MethodDef.EntityDef.Type.ToCSharp());
                    }
                }
            }
        }

        private void ValidateMethodWithParameterDifferentTypeThanProperty()
        {
            if (MethodDef.MethodType != MethodType.Custom)
            {
                foreach (var argument in MethodDef.Properties)
                {
                    PropertyDef matchingPropertyOrNull;
                    if (TryGetPropertyFromArgumentName(argument.Name, out matchingPropertyOrNull))
                    {
                        if (matchingPropertyOrNull != null)
                        {
                            if (matchingPropertyOrNull.Type != argument.Type)
                            {
                                AddError("ParameterAndPropertyDontMatch", "parameter {0} is not the same type as property {1}. It must be {2}",
                                    argument.Name,
                                    matchingPropertyOrNull.PropertyName,
                                    matchingPropertyOrNull.Type.ToCSharp());
                            }
                        }
                    }
                }
            }
        }

        private void ValidateNonCustomMethodHasEntityDefined()
        {
            if (MethodDef.MethodType != MethodType.Custom)
            {
                if (MethodDef.EntityDef == null)
                {
                    AddError("CantInferEntityType", "Can't infer entity type. Call SetEntityType() to define it explicitly");
                }
            }
        }

        private bool TryGetPropertyFromArgumentName(string propName, out PropertyDef property)
        {
            property = MethodDef.EntityDef.Properties.FirstOrDefault(p => p.PropertyName == propName.Capitalize());
            if (property == null)
            {
                property = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        protected MethodDef MethodDef { get { return _methodDef; } }
    }
}
