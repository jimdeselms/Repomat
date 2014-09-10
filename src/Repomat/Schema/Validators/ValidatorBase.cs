using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal abstract class ValidatorBase
    {
        private readonly RepositoryDef _repoDef;
        private readonly IList<ValidationError> _errors;
        private readonly DatabaseType _databaseType;
        private readonly List<Action> _validators = new List<Action>();

        protected ValidatorBase(RepositoryDef repoDef, DatabaseType databaseType, IList<ValidationError> errors)
        {
            _repoDef = repoDef;
            _databaseType = databaseType;
            _errors = errors;
        }

        public IEnumerable<ValidationError> Validate()
        {
            foreach (var validator in _validators)
            {
                validator();
            }

            return _errors;
        }

        public IReadOnlyList<string> Errors { get { return (List<string>)(_errors); } }

        internal IList<ValidationError> ErrorList { get { return _errors; } }

        public bool HasErrors { get { return _errors.Count > 0; } }

        protected DatabaseType DatabaseType { get { return _databaseType; } }

        protected RepositoryDef RepoDef { get { return _repoDef; } }

        internal IEnumerable<Action> Validators { get { return _validators; } }
        
        protected void AddValidators(params Action[] actions)
        {
            foreach (var action in actions)
            {
                _validators.Add(action);
            }
        }

        protected virtual void AddError(string errorCode, string format, params object[] args)
        {
            _errors.Add(new ValidationError(errorCode, format, args));
        }
    }
}
