using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Repomat;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema
{
    /// <summary>
    /// Given a repository interface, figures out what the entities are,
    /// </summary>
    internal class RepositoryEntityFinder
    {
        private readonly Type _repoType;

        public RepositoryEntityFinder(Type repoType)
        {
            _repoType = repoType;
        }

        public IEnumerable<Type> GetRepositoryEntities()
        {
            var methodToEntityTypeMap = _repoType.GetMethods().Select(m => new { EntityTypeOrNull = GetEntityTypeForMethod(m), MethodInfo = m });

            var types = methodToEntityTypeMap.Select(m => m.EntityTypeOrNull).Where(t => t != null).Distinct();

            return types;
        }

        public static Type GetEntityTypeForMethod(MethodInfo methodInfo)
        {
            var coreType = methodInfo.ReturnType.GetCoreType();

            // Does it have an interesting return type? Then that's the entity type.
            if (coreType != typeof (void) && !coreType.IsDatabaseType())
            {
                return coreType;
            }

            // Does it have a parameter that's interesting?
            foreach (var param in methodInfo.GetParameters())
            {
                var parmType = param.ParameterType.GetCoreType();
                if (!parmType.IsDatabaseType() && !parmType.IsAssignableFrom(typeof (IDbConnection)) &&
                    !parmType.IsAssignableFrom(typeof (IDbTransaction)))
                {
                    return parmType;
                }
            }

            // We didn't find it; it'll have to be defined later.
            return null;
        }
    }
}
