using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema
{
    internal class EntityDef
    {
        private string _tableName;
        private readonly Type _type;
        private readonly IList<PropertyDef> _columns;
        private readonly IList<PropertyDef> _nonPkColumns;
        private readonly bool _hasIdentity;
        private readonly bool _createClassThroughConstructor;

        private bool _hasExplicitPrimaryKey = false;

        private IList<PropertyDef> _primaryKey;
        
        public EntityDef(Type type, string tableName, IEnumerable<PropertyDef> columns, IEnumerable<PropertyDef> primaryKey, bool hasIdentity, bool createClassThroughConstructor)
        {
            _type = type;
            _tableName = tableName;
            _columns = columns.ToArray();
            _primaryKey = primaryKey.ToArray();
            _hasIdentity = hasIdentity;
            _createClassThroughConstructor = createClassThroughConstructor;

            _nonPkColumns = _columns.Where(c => _primaryKey.All(pk => pk.ColumnName != c.ColumnName)).ToArray();
        }

        public Type Type { get { return _type; } }

        public string TableName 
        {
            get
            {
                return _tableName;
            }
            set 
            { 
                _tableName = value; 
            } 
        }

        public PropertyDef FindPropertyByParameterName(string parameterName)
        {
            return Properties.First(c => c.PropertyName.Uncapitalize() == parameterName);
        }

        public IList<PropertyDef> PrimaryKey 
        { 
            get { return _primaryKey; }
            internal set
            {
                _primaryKey = value;
                _hasExplicitPrimaryKey = true;
            }
        }

        internal bool HasExplicitPrimaryKey
        {
            get { return _hasExplicitPrimaryKey; }
        }

        public IList<PropertyDef> Properties { get { return _columns; } }
        public IList<PropertyDef> NonPrimaryKeyColumns { get { return _nonPkColumns; } }
        public bool CreateClassThroughConstructor { get { return _createClassThroughConstructor; } }
        public bool HasIdentity { get { return _hasIdentity; } }
    }
}
