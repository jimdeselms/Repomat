using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Databases;

namespace Repomat.Schema
{
    internal class RepositoryDef
    {
        private readonly Type _repoType;
        private readonly IReadOnlyList<MethodDef> _implementationDetails;
        private readonly NamingConvention _tableNamingConvention;
        private readonly NamingConvention _columnNamingConvention;

        public RepositoryDef(Type repoType, IEnumerable<MethodDef> implementationDetails, NamingConvention tableNamingConvention, NamingConvention columnNamingConvention)
        {
            _repoType = repoType;
            _implementationDetails = implementationDetails.ToArray();
            _tableNamingConvention = tableNamingConvention;
            _columnNamingConvention = columnNamingConvention;
        }

        public EntityDef GetEntityDef(Type entityType)
        {
            return RepositoryDefBuilder.GetEntityDef(_repoType, _tableNamingConvention, _columnNamingConvention, entityType);
        }

        public IReadOnlyList<MethodDef> Methods { get { return _implementationDetails; } }

        public Type RepositoryType { get { return _repoType; } }

        public NamingConvention ColumnNamingConvention { get { return _columnNamingConvention; } }

        internal IEnumerable<EntityDef> GetEntityDefs()
        {
            return Methods.Select(m => m.EntityDef).Where(m => m != null).Distinct();
        }
    }
}
