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

        public RepositoryDef(Type entityType, Type repoType, string tableName, IEnumerable<PropertyDef> columns, IEnumerable<PropertyDef> primaryKey, IEnumerable<MethodDef> implementationDetails, bool hasIdentity, bool createClassThroughConstructor)
        {
            _entityDef = new EntityDef(entityType, tableName, columns, primaryKey, hasIdentity, createClassThroughConstructor);
            _repoType = repoType;
            _implementationDetails = implementationDetails.ToArray();
        }

        public string TableName
        {
            get { return _entityDef.TableName; } 
            set { _entityDef.TableName = value; }
        }

        public IReadOnlyList<PropertyDef> Properties { get { return _entityDef.Columns; }} 

        public IReadOnlyList<PropertyDef> PrimaryKey { get { return _entityDef.PrimaryKey; }}

        public IReadOnlyList<PropertyDef> NonPrimaryKeyColumns { get { return _entityDef.NonPkColumns; } } 

        public bool HasIdentity { get { return _entityDef.HasIdentity; }}

        public bool CreateClassThroughConstructor { get { return _entityDef.CreateClassThroughConstructor; } }

        public IReadOnlyList<MethodDef> Methods { get { return _implementationDetails; } }

        public Type EntityType { get { return _entityDef.Type; } }

        public Type RepositoryType { get { return _repoType; } }

        public PropertyDef FindPropertyByParameterName(string parameterName)
        {
            return _entityDef.Columns.First(c => c.PropertyName.Uncapitalize() == parameterName);
        }

    }
}
