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
        private readonly IReadOnlyList<PropertyDef> _columns;
        private readonly IReadOnlyList<PropertyDef> _primaryKey;
        private readonly IReadOnlyList<PropertyDef> _nonPkColumns;
        private readonly bool _hasIdentity;
        private readonly bool _createClassThroughConstructor;

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
        
        public IReadOnlyList<PropertyDef> Columns { get { return _columns; } }
        public IReadOnlyList<PropertyDef> PrimaryKey { get { return _primaryKey; } }
        public IReadOnlyList<PropertyDef> NonPkColumns { get { return _nonPkColumns; } }
        public bool HasIdentity { get { return _hasIdentity; } }
        public bool CreateClassThroughConstructor { get { return _createClassThroughConstructor; } }
    }
}
