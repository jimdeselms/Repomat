using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema
{
    internal class PropertyDef
    {
        private readonly string _propertyName;
        private string _columnName;

        private readonly Type _type;

        public PropertyDef(string propertyName, string columnName, Type type)
        {
            _propertyName = propertyName;
            _columnName = columnName;
            _type = type;
        }

        public string ColumnName
        {
            get { return _columnName; }
            set { _columnName = value; }
        }

        public string PropertyName { get { return _propertyName; } }
        public Type Type { get { return _type; }}
    }
}
