using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal abstract class MethodValidator
    {
        private readonly RepositoryDef _repoDef;
        private readonly MethodDef _methodDef;
        private readonly IList<ValidationError> _errors;
        private readonly List<Action> _validators = new List<Action>();

        public MethodValidator(RepositoryDef repoDef, MethodDef methodDef, IList<ValidationError> errors)
        {
            _repoDef = repoDef;
            _methodDef = methodDef;
            _errors = errors;

            AddValidators(
                ValidateMethodWithParameterThatDoesntMapToProperty,
                ValidateMethodWithParameterDifferentTypeThanProperty);
        }

        public void Validate()
        {
            foreach (var validator in _validators)
            {
                validator();
            }
        }

        public IReadOnlyList<string> Errors { get { return (List<string>)(_errors); } }

        public bool HasErrors { get { return _errors.Count > 0; } }

        protected void AddValidators(params Action[] actions)
        {
            foreach (var action in actions)
            {
                _validators.Add(action);
            }
        }

        protected void AddError(string errorCode, string format, params object[] args)
        {
            string firstPart = string.Format("{0} method {1} on {2}: ", MethodDef.MethodType, MethodDef.NameAndArgumentList, RepositoryDef.RepositoryType.ToCSharp());
            _errors.Add(new ValidationError(errorCode, firstPart + format, args));
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
                                 RepositoryDef.EntityType.ToCSharp());
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

        private bool TryGetPropertyFromArgumentName(string propName, out PropertyDef property)
        {
            property = RepositoryDef.Properties.FirstOrDefault(p => p.PropertyName == propName.Capitalize());
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
        protected RepositoryDef RepositoryDef { get { return _repoDef; } }
    }
}
