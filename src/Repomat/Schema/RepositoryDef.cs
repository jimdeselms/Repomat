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
        private string _tableName;
        private readonly Type _entityType;
        private readonly Type _repoType;
        private readonly IReadOnlyList<PropertyDef> _columns;
        private readonly IReadOnlyList<PropertyDef> _primaryKey;
        private readonly IReadOnlyList<PropertyDef> _nonPkColumns;
        private readonly IReadOnlyList<MethodDef> _implementationDetails;
        private readonly bool _hasIdentity;
        private readonly bool _createClassThroughConstructor;

        public RepositoryDef(Type entityType, Type repoType, string tableName, IEnumerable<PropertyDef> columns, IEnumerable<PropertyDef> primaryKey, IEnumerable<MethodDef> implementationDetails, bool hasIdentity, bool createClassThroughConstructor)
        {
            _entityType = entityType;
            _repoType = repoType;
            _tableName = tableName;
            _columns = columns.ToArray();
            _primaryKey = primaryKey.ToArray();
            _implementationDetails = implementationDetails.ToArray();
            _hasIdentity = hasIdentity;
            _createClassThroughConstructor = createClassThroughConstructor;

            _nonPkColumns = _columns.Where(c => _primaryKey.All(pk => pk.ColumnName != c.ColumnName)).ToList();
        }

        public string TableName
        {
            get { return _tableName; } 
            set { _tableName = value; }
        }

        public IReadOnlyList<PropertyDef> Properties { get { return _columns; }} 

        public IReadOnlyList<PropertyDef> PrimaryKey { get { return _primaryKey; }}

        public IReadOnlyList<PropertyDef> NonPrimaryKeyColumns { get { return _nonPkColumns; } } 

        public bool HasIdentity { get { return _hasIdentity; }}

        public bool CreateClassThroughConstructor { get { return _createClassThroughConstructor; } }

        public IReadOnlyList<MethodDef> Methods { get { return _implementationDetails; } }

        public Type EntityType { get { return _entityType; } }

        public Type RepositoryType { get { return _repoType; } }

        public PropertyDef FindPropertyByParameterName(string parameterName)
        {
            return _columns.First(c => c.PropertyName.Uncapitalize() == parameterName);
        }

    }
}
