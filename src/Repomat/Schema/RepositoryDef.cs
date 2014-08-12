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
        private readonly EntityDef _entityDef;
        private readonly Type _repoType;
        private readonly IReadOnlyList<MethodDef> _implementationDetails;
        private readonly NamingConvention _tableNamingConvention;
        private readonly NamingConvention _columnNamingConvention;

        public RepositoryDef(EntityDef entityDef, Type repoType, IEnumerable<MethodDef> implementationDetails, NamingConvention tableNamingConvention, NamingConvention columnNamingConvention)
        {
            _entityDef = entityDef;
            _repoType = repoType;
            _implementationDetails = implementationDetails.ToArray();
            _tableNamingConvention = tableNamingConvention;
            _columnNamingConvention = columnNamingConvention;
        }

        public EntityDef GetEntityDef(Type entityType)
        {
            return RepositoryDefBuilder.GetEntityDef(_repoType, _tableNamingConvention, _columnNamingConvention, entityType);
        }

        public IReadOnlyList<PropertyDef> Properties 
        { 
            get 
            { 
                return _entityDef.Columns; 
            }
        } 

        public IReadOnlyList<PropertyDef> PrimaryKey { get { return _entityDef.PrimaryKey; }}

        public IReadOnlyList<PropertyDef> NonPrimaryKeyColumns { get { return _entityDef.NonPkColumns; } } 

        public bool HasIdentity { get { return _entityDef.HasIdentity; }}

        public bool CreateClassThroughConstructor { get { return _entityDef.CreateClassThroughConstructor; } }

        public IReadOnlyList<MethodDef> Methods { get { return _implementationDetails; } }

        public Type EntityType { get { return _entityDef == null ? typeof(void) : _entityDef.Type; } }

        public Type RepositoryType { get { return _repoType; } }

        public PropertyDef FindPropertyByParameterName(string parameterName)
        {
            return _entityDef.Columns.First(c => c.PropertyName.Uncapitalize() == parameterName);
        }

    }
}
