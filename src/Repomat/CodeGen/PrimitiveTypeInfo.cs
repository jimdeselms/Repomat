using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class PrimitiveTypeInfo
    {
        private readonly Type _type;
        private readonly DbType _dbType;
        private readonly string _readerGetExpr;
        private readonly string _scalarConvertExpr;
        private readonly string _sqlDatatype;
        private readonly Action<IlBuilder> _emitConversion;
        private readonly bool _canBeNull;

        public static readonly FieldInfo DBNULL_VALUE = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);

        public PrimitiveTypeInfo(Type type, DbType dbType, string readerGetExpr, string scalarConvertExpr, string sqlDatatype, Action<IlBuilder> emitConversion)
        {
            _type = type;
            _dbType = dbType;
            _readerGetExpr = readerGetExpr;
            _scalarConvertExpr = scalarConvertExpr;
            _sqlDatatype = sqlDatatype;
            _emitConversion = emitConversion;

            _canBeNull = type.IsNullable() || !type.IsValueType;
        }

        public Type Type { get { return _type; } }
        public string ScalarConvertExpr { get { return _scalarConvertExpr; } }
        public string SqlDatatype { get { return _sqlDatatype; } }
        public DbType DbType { get { return _dbType; } }
        public bool CanBeNull { get { return _canBeNull; } }

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
                    Action<IlBuilder> conversion = underlyingType._emitConversion;

                    CreateType(t, underlyingType.DbType, getExpr, convertExpr, datatype, conversion);
                }
            }

            return _primitiveTypes[t]; 
        }

        public void EmitConversion(IlBuilder il)
        {
            _emitConversion(il);
        }

        static PrimitiveTypeInfo()
        {
            CreateType(typeof(int), DbType.Int32, "reader.GetInt32({0})", "System.Convert.ToInt32({0})", "INT {0} NOT NULL", SimpleConversion("ToInt32"));
            CreateType(typeof(short), DbType.Int16, "reader.GetInt16({0})", "System.Convert.ToInt16({0})", "SMALLINT {0} NOT NULL", SimpleConversion("ToInt16"));
            CreateType(typeof(long), DbType.Int64, "reader.GetInt64({0})", "System.Convert.ToInt64({0})", "BIGINT {0} NOT NULL", SimpleConversion("ToInt64"));
            CreateType(typeof(byte), DbType.Byte, "reader.GetByte({0})", "System.Convert.ToByte({0})", "TINYINT {0} NOT NULL", SimpleConversion("ToByte"));
            CreateType(typeof(bool), DbType.Boolean, "reader.GetBoolean({0})", "System.Convert.ToBoolean({0})", "BIT {0} NOT NULL", SimpleConversion("ToBoolean"));
            CreateType(typeof(decimal), DbType.Decimal, "reader.GetDecimal({0})", "System.Convert.ToDecimal({0})", "MONEY {0} NOT NULL", SimpleConversion("ToDecimal"));
            CreateType(typeof(DateTime), DbType.DateTime, "reader.GetDateTime({0})", "System.Convert.ToDateTime({0})", "DATETIME {0} NOT NULL", SimpleConversion("ToDateTime"));
            CreateType(typeof(DateTimeOffset), DbType.DateTimeOffset, "reader.GetDateTimeOffset({0})", "(System.DateTimeOffset){0}", "DATETIMEOFFSET(7) {0} NOT NULL", CastConversion<DateTimeOffset>());
            CreateType(typeof(uint), DbType.UInt32, "(uint)reader.GetInt32({0})", "System.Convert.ToUInt32({0})", "INT {0} NOT NULL", SimpleConversion("ToUInt32"));
            CreateType(typeof(ushort), DbType.UInt16, "(ushort)reader.GetInt16({0})", "System.Convert.ToUInt16({0})", "SMALLINT {0} NOT NULL", SimpleConversion("ToUInt16"));
            CreateType(typeof(ulong), DbType.UInt64, "(ulong)reader.GetInt64({0})", "System.Convert.ToUInt64({0})", "BIGINT", SimpleConversion("ToUInt64"));
            CreateType(typeof(char), DbType.String, "(char)reader.GetString({0})[0]", "System.Convert.ToString({0})[0]", "VARCHAR(1)", CharConversion());
            CreateType(typeof(int?), DbType.Int32, "reader.IsDBNull({0}) ? null : (int?)reader.GetInt32({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (int?)System.Convert.ToInt32({0})", "INT", NullableConversion<int>("ToInt32"));
            CreateType(typeof(short?), DbType.Int16, "reader.IsDBNull({0}) ? null : (short?)reader.GetInt16({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (short?)System.Convert.ToInt16({0})", "SMALLINT", NullableConversion<short>("ToInt16"));
            CreateType(typeof(long?), DbType.Int64, "reader.IsDBNull({0}) ? null : (long?)reader.GetInt64({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (long?)System.Convert.ToInt64({0})", "BIGINT", NullableConversion<long>("ToInt64"));
            CreateType(typeof(byte?), DbType.Byte, "reader.IsDBNull({0}) ? null : (byte?)reader.GetByte({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (byte?)System.Convert.ToByte({0})", "TINYINT", NullableConversion<byte>("ToByte"));
            CreateType(typeof(bool?), DbType.Boolean, "reader.IsDBNull({0}) ? null : (bool?)reader.GetBoolean({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (bool?)System.Convert.ToBoolean({0})", "BIT", NullableConversion<bool>("ToBoolean"));
            CreateType(typeof(decimal?), DbType.Decimal, "reader.IsDBNull({0}) ? null : (decimal?)reader.GetDecimal({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (decimal?)System.Convert.ToDecimal({0})", "MONEY", NullableConversion<decimal>("ToDecimal"));
            CreateType(typeof(DateTime?), DbType.DateTime, "reader.IsDBNull({0}) ? null : (DateTime?)reader.GetDateTime({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (DateTime?)System.Convert.ToDateTime({0})", "DATETIME", NullableConversion<DateTime>("ToDateTime"));
            CreateType(typeof(DateTimeOffset?), DbType.DateTimeOffset, "reader.IsDBNull({0}) ? null : (DateTimeOffset?)reader.GetDateTimeOffset({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (System.DateTimeOffset?){0}", "DATETIMEOFFSET(7)");
            CreateType(typeof(uint?), DbType.UInt32, "reader.IsDBNull({0}) ? null : (uint?)reader.GetInt32({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (uint?)System.Convert.ToUInt32({0})", "INT", NullableConversion<uint>("ToUInt32"));
            CreateType(typeof(ushort?), DbType.UInt16, "reader.IsDBNull({0}) ? null : (ushort?)reader.GetInt16({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (ushort?)System.Convert.ToUInt16({0})", "SMALLINT", NullableConversion<ushort>("ToInt16"));
            CreateType(typeof(ulong?), DbType.UInt64, "reader.IsDBNull({0}) ? null : (ulong?)reader.GetInt64({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (ulong?)System.Convert.ToUInt64({0})", "BIGINT", NullableConversion<ulong>("ToInt64"));
            CreateType(typeof(char?), DbType.String, "reader.IsDBNull({0}) ? null : (char)reader.GetString({0})[0]", "({0} == null || {0} == System.DBNull.Value) ? null : (char?)System.Convert.ToString({0})[0]", "VARCHAR(1)", NullableCharConversion());
            CreateType(typeof(string), DbType.String, "reader.IsDBNull({0}) ? null : reader.GetString({0})", "System.Convert.ToString({0})", "VARCHAR({1})", StringConversion());
            CreateType(typeof(byte[]), DbType.Binary, "reader.IsDBNull({0}) ? null : (byte[])reader.GetValue({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (byte[])({0})", "VARBINARY({1})", ByteArrayConversion());
        }

        private static void CreateType(Type t, DbType dbType, string readerGetExpr, string scalarConvertExpr, string sqlDatatype, Action<IlBuilder> emitConversion=null)
        {
            _primitiveTypes[t] = new PrimitiveTypeInfo(t, dbType, readerGetExpr, scalarConvertExpr, sqlDatatype, emitConversion);
        }

        private static Action<IlBuilder> SimpleConversion(string convertMethodName)
        {
            MethodInfo convertMethod = typeof(Convert).GetMethod(convertMethodName, new Type[] { typeof(object) });
            return il => {
                il.ILGenerator.EmitCall(OpCodes.Call, convertMethod, new Type[] { typeof(int) });
            };
        }

        private static Action<IlBuilder> NullableConversion<T>(string convertMethodName) where T : struct
        {
            var nullableCtor = typeof(T?).GetConstructor(new[] { typeof(T) });
            var dbNullValue = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);

            return il =>
            {
                var label1 = il.DefineLabel();
                var end = il.DefineLabel();

                var nullableValueLabel = il.DeclareLocal(typeof(object));
                var resultLocal = il.DeclareLocal(typeof(T?));

                il.ILGenerator.Emit(OpCodes.Stloc, nullableValueLabel);
                il.Ldloc(nullableValueLabel);
                il.ILGenerator.Emit(OpCodes.Brfalse, label1);

                il.Ldloc(nullableValueLabel);
                il.ILGenerator.Emit(OpCodes.Ldsfld, dbNullValue);
                il.ILGenerator.Emit(OpCodes.Beq, label1);

                il.Ldloc(nullableValueLabel);
                SimpleConversion(convertMethodName)(il);
                il.ILGenerator.Emit(OpCodes.Newobj, nullableCtor);
                il.ILGenerator.Emit(OpCodes.Br, end);

                il.MarkLabel(label1);
                il.ILGenerator.Emit(OpCodes.Ldloca_S, resultLocal);
                il.ILGenerator.Emit(OpCodes.Initobj, typeof(T?));
                il.Ldloc(resultLocal);
                il.MarkLabel(end);
            };
        }

        private static Action<IlBuilder> StringConversion()
        {
            var dbNullValue = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);

            return il =>
            {
                var label1 = il.DefineLabel();
                var end = il.DefineLabel();

                var nullableValueLabel = il.DeclareLocal(typeof(string));

                il.ILGenerator.Emit(OpCodes.Stloc, nullableValueLabel);
                il.Ldloc(nullableValueLabel);
                il.ILGenerator.Emit(OpCodes.Brfalse, label1);

                il.Ldloc(nullableValueLabel);
                il.ILGenerator.Emit(OpCodes.Ldsfld, dbNullValue);
                il.ILGenerator.Emit(OpCodes.Beq, label1);

                il.Ldloc(nullableValueLabel);
                SimpleConversion("ToString")(il);
                il.ILGenerator.Emit(OpCodes.Br, end);

                il.MarkLabel(label1);
                il.ILGenerator.Emit(OpCodes.Ldnull);
                il.MarkLabel(end);
            };
        }

        private static Action<IlBuilder> ByteArrayConversion()
        {
            var dbNullValue = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);

            return il =>
            {
                var label1 = il.DefineLabel();
                var end = il.DefineLabel();

                var nullableValueLabel = il.DeclareLocal(typeof(string));

                il.ILGenerator.Emit(OpCodes.Stloc, nullableValueLabel);
                il.Ldloc(nullableValueLabel);
                il.ILGenerator.Emit(OpCodes.Brfalse_S, label1);

                il.Ldloc(nullableValueLabel);
                il.ILGenerator.Emit(OpCodes.Ldsfld, dbNullValue);
                il.ILGenerator.Emit(OpCodes.Beq_S, label1);

                il.Ldloc(nullableValueLabel);
                CastConversion<byte[]>()(il);
                il.ILGenerator.Emit(OpCodes.Br, end);

                il.MarkLabel(label1);
                il.ILGenerator.Emit(OpCodes.Ldnull);
                il.MarkLabel(end);
            };
        }

        private static Action<IlBuilder> CastConversion<T>()
        {
            bool needsUnboxing = typeof(T).IsValueType;

            return il => {
                if (needsUnboxing)
                {
                    il.ILGenerator.Emit(OpCodes.Unbox_Any, typeof(T));
                }
            };
        }

        private static Action<IlBuilder> CharConversion()
        {
            return il =>
            {
                var loc = il.ILGenerator.DeclareLocal(typeof(string));
                SimpleConversion("ToString")(il);
                il.ILGenerator.Emit(OpCodes.Stloc, loc);
                il.Ldloc(loc);

                var getChars = typeof(string).GetMethod("get_Chars");
                il.Ldc(0);
                il.Call(getChars);
            };
        }

        private static Action<IlBuilder> NullableCharConversion()
        {
            var nullableCtor = typeof(char?).GetConstructor(new[] { typeof(char) });
            var dbNullValue = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            var getChars = typeof(string).GetMethod("get_Chars");

            return il =>
            {
                var label1 = il.DefineLabel();
                var end = il.DefineLabel();

                var nullableValueLocal = il.DeclareLocal(typeof(object));
                var resultLocal = il.DeclareLocal(typeof(char?));

                il.ILGenerator.Emit(OpCodes.Stloc, nullableValueLocal);
                il.Ldloc(nullableValueLocal);
                il.ILGenerator.Emit(OpCodes.Brfalse_S, label1);

                il.Ldloc(nullableValueLocal);
                il.ILGenerator.Emit(OpCodes.Ldsfld, dbNullValue);
                il.ILGenerator.Emit(OpCodes.Beq_S, label1);

                il.Ldloc(nullableValueLocal);
                CharConversion()(il);
                il.ILGenerator.Emit(OpCodes.Newobj, nullableCtor);
                il.ILGenerator.Emit(OpCodes.Br, end);

                il.MarkLabel(label1);
                il.ILGenerator.Emit(OpCodes.Ldloca_S, resultLocal);
                il.ILGenerator.Emit(OpCodes.Initobj, typeof(char?));
                il.Ldloc(resultLocal);
                il.MarkLabel(end);
            };
        }
    }
}
