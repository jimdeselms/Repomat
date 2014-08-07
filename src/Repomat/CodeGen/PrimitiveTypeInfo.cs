using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class PrimitiveTypeInfo
    {
        private readonly Type _type;
        private readonly string _readerGetExpr;
        private readonly string _scalarConvertExpr;
        private readonly string _sqlDatatype;

        public PrimitiveTypeInfo(Type type, string readerGetExpr, string scalarConvertExpr, string sqlDatatype)
        {
            _type = type;
            _readerGetExpr = readerGetExpr;
            _scalarConvertExpr = scalarConvertExpr;
            _sqlDatatype = sqlDatatype;
        }

        public Type Type { get { return _type; } }
        public string ScalarConvertExpr { get { return _scalarConvertExpr; } }
        public string SqlDatatype { get { return _sqlDatatype; } }

        public string GetReaderGetExpr(string index, bool useStrictTyping)
        {
            // If you use strict types, then you can get better performance, since you're just
            // getting the values directly without converting them. But, if your schema's types
            // don't match the types defined in your code, then you'll get an exception.
            //
            // If you don't use strict types, it'll be a lot more forgiving, because it will just
            // convert the resulting object to the correct type.
            if (useStrictTyping)
            {
                return string.Format(_readerGetExpr, index);
            }
            else
            {
                return string.Format(_scalarConvertExpr, string.Format("reader.GetValue({0})", index));
            }
        }

        public string GetScalarConvertExpr(string input)
        {
            return string.Format(_scalarConvertExpr, input);
        }

        public string GetSqlDatatype(bool isIdentity, string width)
        {
            string identity = isIdentity ? "IDENTITY" : "";
            string datatype = string.Format(_sqlDatatype, identity, width);

            if (isIdentity)
            {
                datatype = datatype.Replace("NOT NULL", "");
            }

            return datatype;
        }

        private static readonly Dictionary<Type, PrimitiveTypeInfo> _primitiveTypes = new Dictionary<Type, PrimitiveTypeInfo>();

        public static PrimitiveTypeInfo Get(Type t)
        {
            PrimitiveTypeInfo typeInfo;
            if (!_primitiveTypes.TryGetValue(t, out typeInfo))
            {
                var coreType = t.GetCoreType();

                if (coreType.IsEnum)
                {
                    // Is the actual type an enum? If it is, then the core type we really want is
                    // the nullable version of it.
                    if (t.IsNullable())
                    {
                        coreType = typeof(Nullable<>).MakeGenericType(coreType.GetEnumUnderlyingType());
                    }
                    else
                    {
                        coreType = coreType.GetEnumUnderlyingType();
                    }

                    var underlyingType = Get(coreType);

                    string getExpr = string.Format("({0})({1})", t.ToCSharp(), underlyingType._readerGetExpr);
                    string convertExpr = string.Format("({0})({1})", t.ToCSharp(), underlyingType._scalarConvertExpr);
                    string datatype = underlyingType._sqlDatatype;

                    CreateType(t, getExpr, convertExpr, datatype);
                }
            }

            return _primitiveTypes[t]; 
        }

        static PrimitiveTypeInfo()
        {
            CreateType(typeof(int), "reader.GetInt32({0})", "System.Convert.ToInt32({0})", "INT {0} NOT NULL");
            CreateType(typeof(short), "reader.GetInt16({0})", "System.Convert.ToInt16({0})", "SMALLINT {0} NOT NULL");
            CreateType(typeof(long), "reader.GetInt64({0})", "System.Convert.ToInt64({0})", "BIGINT {0} NOT NULL");
            CreateType(typeof(byte), "reader.GetByte({0})", "System.Convert.ToByte({0})", "TINYINT {0} NOT NULL");
            CreateType(typeof(bool), "reader.GetBoolean({0})", "System.Convert.ToBoolean({0})", "BIT {0} NOT NULL");
            CreateType(typeof(decimal), "reader.GetDecimal({0})", "System.Convert.ToDecimal({0})", "MONEY {0} NOT NULL");
            CreateType(typeof(DateTime), "reader.GetDateTime({0})", "System.Convert.ToDateTime({0})", "DATETIME {0} NOT NULL");
            CreateType(typeof(DateTimeOffset), "reader.GetDateTimeOffset({0})", "(System.DateTimeOffset){0}", "DATETIMEOFFSET(7) {0} NOT NULL");
            CreateType(typeof(uint), "(uint)reader.GetInt32({0})", "System.Convert.ToUInt32({0})", "INT {0} NOT NULL");
            CreateType(typeof(ushort), "(ushort)reader.GetInt16({0})", "System.Convert.ToUInt16({0})", "SMALLINT {0} NOT NULL");
            CreateType(typeof(ulong), "(ulong)reader.GetInt64({0})", "System.Convert.ToUInt64({0})", "BIGINT");
            CreateType(typeof(char), "(char)reader.GetString({0})[0]", "System.Convert.ToString({0})[0]", "VARCHAR(1)");
            CreateType(typeof(int?), "reader.IsDBNull({0}) ? null : (int?)reader.GetInt32({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (int?)System.Convert.ToInt32({0})", "INT");
            CreateType(typeof(short?), "reader.IsDBNull({0}) ? null : (short?)reader.GetInt16({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (short?)System.Convert.ToInt16({0})", "SMALLINT");
            CreateType(typeof(long?), "reader.IsDBNull({0}) ? null : (long?)reader.GetInt64({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (long?)System.Convert.ToInt64({0})", "BIGINT");
            CreateType(typeof(byte?), "reader.IsDBNull({0}) ? null : (byte?)reader.GetByte({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (byte?)System.Convert.ToByte({0})", "TINYINT");
            CreateType(typeof(bool?), "reader.IsDBNull({0}) ? null : (bool?)reader.GetBoolean({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (bool?)System.Convert.ToBoolean({0})", "BIT");
            CreateType(typeof(decimal?), "reader.IsDBNull({0}) ? null : (decimal?)reader.GetDecimal({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (decimal?)System.Convert.ToDecimal({0})", "MONEY");
            CreateType(typeof(DateTime?), "reader.IsDBNull({0}) ? null : (DateTime?)reader.GetDateTime({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (DateTime?)System.Convert.ToDateTime({0})", "DATETIME");
            CreateType(typeof(DateTimeOffset?), "reader.IsDBNull({0}) ? null : (DateTimeOffset?)reader.GetDateTimeOffset({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (System.DateTimeOffset?){0}", "DATETIMEOFFSET(7)");
            CreateType(typeof(uint?), "reader.IsDBNull({0}) ? null : (uint?)reader.GetInt32({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (uint?)System.Convert.ToUInt32({0})", "INT");
            CreateType(typeof(ushort?), "reader.IsDBNull({0}) ? null : (ushort?)reader.GetInt16({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (ushort?)System.Convert.ToUInt16({0})", "SMALLINT");
            CreateType(typeof(ulong?), "reader.IsDBNull({0}) ? null : (ulong?)reader.GetInt64({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (ulong?)System.Convert.ToUInt64({0})", "BIGINT");
            CreateType(typeof(char?), "reader.IsDBNull({0}) ? null : (char)reader.GetString({0})[0]", "({0} == null || {0} == System.DBNull.Value) ? null : (char?)System.Convert.ToString({0})[0]", "VARCHAR(1)");
            CreateType(typeof(string), "reader.IsDBNull({0}) ? null : reader.GetString({0})", "System.Convert.ToString({0})", "VARCHAR({1})");
        }

        private static void CreateType(Type t, string readerGetExpr, string scalarConvertExpr, string sqlDatatype)
        {
            _primitiveTypes[t] = new PrimitiveTypeInfo(t, readerGetExpr, scalarConvertExpr, sqlDatatype);
        }
    }
}
