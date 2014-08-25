using System;
using System.Collections.Generic;
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
        private readonly string _readerGetExpr;
        private readonly string _scalarConvertExpr;
        private readonly string _sqlDatatype;
        private readonly Action<ILGenerator> _emitConversion;

        public PrimitiveTypeInfo(Type type, string readerGetExpr, string scalarConvertExpr, string sqlDatatype, Action<ILGenerator> emitConversion)
        {
            _type = type;
            _readerGetExpr = readerGetExpr;
            _scalarConvertExpr = scalarConvertExpr;
            _sqlDatatype = sqlDatatype;
            _emitConversion = emitConversion;
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

        public void EmitConversion(ILGenerator ilGenerator)
        {
            _emitConversion(ilGenerator);
        }

        static PrimitiveTypeInfo()
        {
            CreateType(typeof(int), "reader.GetInt32({0})", "System.Convert.ToInt32({0})", "INT {0} NOT NULL", SimpleConversion("ToInt32"));
            CreateType(typeof(short), "reader.GetInt16({0})", "System.Convert.ToInt16({0})", "SMALLINT {0} NOT NULL", SimpleConversion("ToInt16"));
            CreateType(typeof(long), "reader.GetInt64({0})", "System.Convert.ToInt64({0})", "BIGINT {0} NOT NULL", SimpleConversion("ToInt64"));
            CreateType(typeof(byte), "reader.GetByte({0})", "System.Convert.ToByte({0})", "TINYINT {0} NOT NULL", SimpleConversion("ToByte"));
            CreateType(typeof(bool), "reader.GetBoolean({0})", "System.Convert.ToBoolean({0})", "BIT {0} NOT NULL", SimpleConversion("ToBoolean"));
            CreateType(typeof(decimal), "reader.GetDecimal({0})", "System.Convert.ToDecimal({0})", "MONEY {0} NOT NULL", SimpleConversion("ToDecimal"));
            CreateType(typeof(DateTime), "reader.GetDateTime({0})", "System.Convert.ToDateTime({0})", "DATETIME {0} NOT NULL", SimpleConversion("ToDateTime"));
            CreateType(typeof(DateTimeOffset), "reader.GetDateTimeOffset({0})", "(System.DateTimeOffset){0}", "DATETIMEOFFSET(7) {0} NOT NULL", CastConversion<DateTimeOffset>());
            CreateType(typeof(uint), "(uint)reader.GetInt32({0})", "System.Convert.ToUInt32({0})", "INT {0} NOT NULL", SimpleConversion("ToUInt32"));
            CreateType(typeof(ushort), "(ushort)reader.GetInt16({0})", "System.Convert.ToUInt16({0})", "SMALLINT {0} NOT NULL", SimpleConversion("ToUInt16"));
            CreateType(typeof(ulong), "(ulong)reader.GetInt64({0})", "System.Convert.ToUInt64({0})", "BIGINT", SimpleConversion("ToUInt64"));
            CreateType(typeof(char), "(char)reader.GetString({0})[0]", "System.Convert.ToString({0})[0]", "VARCHAR(1)", CharConversion());
            CreateType(typeof(int?), "reader.IsDBNull({0}) ? null : (int?)reader.GetInt32({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (int?)System.Convert.ToInt32({0})", "INT", NullableConversion<int>("ToInt32"));
            CreateType(typeof(short?), "reader.IsDBNull({0}) ? null : (short?)reader.GetInt16({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (short?)System.Convert.ToInt16({0})", "SMALLINT", NullableConversion<short>("ToInt16"));
            CreateType(typeof(long?), "reader.IsDBNull({0}) ? null : (long?)reader.GetInt64({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (long?)System.Convert.ToInt64({0})", "BIGINT", NullableConversion<long>("ToInt64"));
            CreateType(typeof(byte?), "reader.IsDBNull({0}) ? null : (byte?)reader.GetByte({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (byte?)System.Convert.ToByte({0})", "TINYINT", NullableConversion<byte>("ToByte"));
            CreateType(typeof(bool?), "reader.IsDBNull({0}) ? null : (bool?)reader.GetBoolean({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (bool?)System.Convert.ToBoolean({0})", "BIT", NullableConversion<bool>("ToBoolean"));
            CreateType(typeof(decimal?), "reader.IsDBNull({0}) ? null : (decimal?)reader.GetDecimal({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (decimal?)System.Convert.ToDecimal({0})", "MONEY", NullableConversion<decimal>("ToDecimal"));
            CreateType(typeof(DateTime?), "reader.IsDBNull({0}) ? null : (DateTime?)reader.GetDateTime({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (DateTime?)System.Convert.ToDateTime({0})", "DATETIME", NullableConversion<DateTime>("ToDateTime"));
            CreateType(typeof(DateTimeOffset?), "reader.IsDBNull({0}) ? null : (DateTimeOffset?)reader.GetDateTimeOffset({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (System.DateTimeOffset?){0}", "DATETIMEOFFSET(7)");
            CreateType(typeof(uint?), "reader.IsDBNull({0}) ? null : (uint?)reader.GetInt32({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (uint?)System.Convert.ToUInt32({0})", "INT", NullableConversion<uint>("ToUInt32"));
            CreateType(typeof(ushort?), "reader.IsDBNull({0}) ? null : (ushort?)reader.GetInt16({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (ushort?)System.Convert.ToUInt16({0})", "SMALLINT", NullableConversion<ushort>("ToInt16"));
            CreateType(typeof(ulong?), "reader.IsDBNull({0}) ? null : (ulong?)reader.GetInt64({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (ulong?)System.Convert.ToUInt64({0})", "BIGINT", NullableConversion<ulong>("ToInt64"));
            CreateType(typeof(char?), "reader.IsDBNull({0}) ? null : (char)reader.GetString({0})[0]", "({0} == null || {0} == System.DBNull.Value) ? null : (char?)System.Convert.ToString({0})[0]", "VARCHAR(1)", NullableCharConversion());
            CreateType(typeof(string), "reader.IsDBNull({0}) ? null : reader.GetString({0})", "System.Convert.ToString({0})", "VARCHAR({1})", SimpleConversion("ToString"));
            CreateType(typeof(byte[]), "reader.IsDBNull({0}) ? null : (byte[])reader.GetValue({0})", "({0} == null || {0} == System.DBNull.Value) ? null : (byte[])({0})", "VARBINARY({1})");
        }

        private static void CreateType(Type t, string readerGetExpr, string scalarConvertExpr, string sqlDatatype, Action<ILGenerator> emitConversion=null)
        {
            _primitiveTypes[t] = new PrimitiveTypeInfo(t, readerGetExpr, scalarConvertExpr, sqlDatatype, emitConversion);
        }

        private static Action<ILGenerator> SimpleConversion(string convertMethodName)
        {
            MethodInfo convertMethod = typeof(Convert).GetMethod(convertMethodName, new Type[] { typeof(object) });
            return il => {
                il.EmitCall(OpCodes.Call, convertMethod, new Type[] { typeof(int) });
            };
        }

        private static Action<ILGenerator> NullableConversion<T>(string convertMethodName) where T : struct
        {
            var nullableCtor = typeof(T?).GetConstructor(new [] { typeof(T) });
            var dbNullValue = typeof (DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);

            return il => {
                var label1 = il.DefineLabel();
                var end = il.DefineLabel();

                var nullableValueLabel = il.DeclareLocal(typeof(object));
                var resultLocal = il.DeclareLocal(typeof(T?));

                il.Emit(OpCodes.Stloc, nullableValueLabel);
                il.Emit(OpCodes.Ldloc, nullableValueLabel);
                il.Emit(OpCodes.Brfalse_S, label1);

                il.Emit(OpCodes.Ldloc, nullableValueLabel);
                il.Emit(OpCodes.Ldsfld, dbNullValue);
                il.Emit(OpCodes.Beq_S, label1);

                il.Emit(OpCodes.Ldloc, nullableValueLabel);
                SimpleConversion(convertMethodName)(il);
                il.Emit(OpCodes.Newobj, nullableCtor);
                il.Emit(OpCodes.Br, end);

                il.MarkLabel(label1);
                il.Emit(OpCodes.Ldloca_S, resultLocal);
                il.Emit(OpCodes.Initobj, typeof(T?));
                il.Emit(OpCodes.Ldloc, resultLocal);
                il.MarkLabel(end);
            };
        }

        private static Action<ILGenerator> CastConversion<T>()
        {
            bool needsUnboxing = typeof(T).IsValueType;

            return il => {
                if (needsUnboxing)
                {
                    il.Emit(OpCodes.Unbox_Any, typeof(T));
                }
            };
        }

        private static Action<ILGenerator> CharConversion()
        {
            return il =>
            {
                var loc = il.DeclareLocal(typeof(string));
                SimpleConversion("ToString")(il);
                il.Emit(OpCodes.Stloc, loc);
                il.Emit(OpCodes.Ldloc, loc);

                var getChars = typeof(string).GetMethod("get_Chars");
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Callvirt, getChars);
            };
        }

        private static Action<ILGenerator> NullableCharConversion()
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

                il.Emit(OpCodes.Stloc, nullableValueLocal);
                il.Emit(OpCodes.Ldloc, nullableValueLocal);
                il.Emit(OpCodes.Brfalse_S, label1);

                il.Emit(OpCodes.Ldloc, nullableValueLocal);
                il.Emit(OpCodes.Ldsfld, dbNullValue);
                il.Emit(OpCodes.Beq_S, label1);

                il.Emit(OpCodes.Ldloc, nullableValueLocal);
                CharConversion()(il);
                il.Emit(OpCodes.Newobj, nullableCtor);
                il.Emit(OpCodes.Br, end);

                il.MarkLabel(label1);
                il.Emit(OpCodes.Ldloca_S, resultLocal);
                il.Emit(OpCodes.Initobj, typeof(char?));
                il.Emit(OpCodes.Ldloc, resultLocal);
                il.MarkLabel(end);
            };
        }
    }
}
