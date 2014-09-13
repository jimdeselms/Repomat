using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Repomat.Schema
{
    internal class ParameterDetails
    {
        private readonly string _name;
        private readonly Type _type;
        private readonly bool _isOut;
        private readonly bool _isTransaction;
        private readonly bool _isConnection;
        private readonly int _index;

        public ParameterDetails(ParameterInfo parameterInfo, int index)
            : this(parameterInfo.ParameterType, parameterInfo.Name, parameterInfo.IsOut, index)
        {
        }

        internal ParameterDetails(Type type, string name, bool isOut, int index)
        {
            _name = name;
            _type = type;
            _isOut = isOut;
            _isTransaction = _type == typeof(IDbTransaction);
            _isConnection = _type == typeof(IDbConnection);
            _index = index;
        }

        public string Name { get { return _name; } }
        public Type Type { get { return _type; } }
        public bool IsOut { get { return _isOut; } }
        public bool IsTransaction { get { return _isTransaction; } }
        public bool IsConnection { get { return _isConnection; } }
        public int Index { get { return _index; } }

        public bool IsSimpleArgument { get { return !_isOut && !_isTransaction && !_isConnection; } }
        public bool IsPrimitiveType
        {
            get
            {
                return _type == typeof(int)
                    || _type == typeof(uint)
                    || _type == typeof(byte)
                    || _type == typeof(short)
                    || _type == typeof(ushort)
                    || _type == typeof(long)
                    || _type == typeof(ulong)
                    || _type == typeof(string)
                    || _type == typeof(DateTime)
                    || _type == typeof(DateTimeOffset)
                    || _type == typeof(int?)
                    || _type == typeof(uint?)
                    || _type == typeof(byte?)
                    || _type == typeof(short?)
                    || _type == typeof(ushort?)
                    || _type == typeof(long?)
                    || _type == typeof(ulong?)
                    || _type == typeof(DateTime?)
                    || _type == typeof(DateTimeOffset?);
            }
        }

        public override string ToString()
        {
            Type type = _isOut ? _type.GetElementType() : _type;
            string outOrNone = _isOut ? "out " : "";

            return string.Format("{0}{1} {2}", outOrNone, type.ToCSharp(), _name);
        }
    }
}
