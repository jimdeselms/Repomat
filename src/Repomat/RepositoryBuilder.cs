using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Schema;

namespace Repomat
{
    public class RepositoryBuilder<TRepo>
    {
        private readonly DataLayerBuilder _dataLayerBuilder;
        private readonly RepositoryDef _repoDef;

        internal RepositoryBuilder(DataLayerBuilder repositoryFactory, RepositoryDef repoDef)
        {
            _dataLayerBuilder = repositoryFactory;
            _repoDef = repoDef;
        }

        public TRepo CreateRepo()
        {
            return (TRepo)_dataLayerBuilder.CreateRepo<TRepo>();
        }

        public MethodBuilder SetupMethod(string methodName)
        {
            var methods = _repoDef.Methods.Where(i => i.MethodName == methodName).ToArray();
            if (methods.Length == 0)
            {
                // TODO: Test
                throw new RepomatException(string.Format("Method {0}.{1} not found", typeof(TRepo).ToCSharp(), methodName));
            }

            if (methods.Length > 1)
            {
                // TODO: Test
                throw new RepomatException(string.Format("More than one method {0}.{1} found. Distinguish by passing the set of parameter types to SetupMethod", typeof(TRepo).ToCSharp(), methodName));
            }

            return new MethodBuilder(methods[0], _repoDef, _dataLayerBuilder.DatabaseType);
        }

        public MethodBuilder SetupMethodWithParameters(string methodName, params Type[] parameters)
        {
            foreach (var method in _repoDef.Methods.Where(m => m.MethodName == methodName))
            {
                if (parameters.Length != method.Parameters.Count)
                {
                    continue;
                }

                bool notFound = false;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] != method.Parameters[i].Type)
                    {
                        notFound = true;
                        break;
                    }
                }

                if (notFound)
                {
                    continue;
                }

                return new MethodBuilder(method, _repoDef, _dataLayerBuilder.DatabaseType);
            }

            throw new RepomatException(string.Format("Couldn't find method {0} matching the specified parameters", methodName));
        }

        public PropertyBuilder SetupProperty(string propertyName)
        {
            return new PropertyBuilder(RepoDef.Properties.First(c => c.PropertyName == propertyName));
        }

        internal RepositoryDef RepoDef { get { return _repoDef; } }
    }
}
