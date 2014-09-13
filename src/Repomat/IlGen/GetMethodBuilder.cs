using Repomat.CodeGen;
using Repomat.Runtime;
using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class GetMethodBuilder : MethodBuilderBase
    {
        private readonly int _customQueryIdx;
        private readonly IlBuilder _ctorIlBuilder;
        private readonly SqlMethodBuilderFactory _methodBuilderFactory;

        private bool _useStrictTyping;

        internal GetMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, int customQueryIdx, SqlMethodBuilderFactory methodBuilderFactory, bool useStrictTyping, IlBuilder ctorIlBuilder)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
            _customQueryIdx = customQueryIdx;
            _ctorIlBuilder = ctorIlBuilder;
            _useStrictTyping = useStrictTyping;
            _methodBuilderFactory = methodBuilderFactory;
        }

        private static readonly MethodInfo _executeReaderMethod = typeof(IDbCommand).GetMethod("ExecuteReader", Type.EmptyTypes);

        protected override void GenerateMethodIl(LocalBuilder cmdVariable)
        {
            FieldBuilder indexesAssignedField = null;
            Dictionary<string, FieldBuilder> columnIndexFields = new Dictionary<string, FieldBuilder>();

            if (MethodDef.CustomSqlOrNull != null && !MethodDef.IsScalarQuery)
            {
                // private _queryX_columnIndexesAssigned = false;
                indexesAssignedField = DefineStaticField<bool>(string.Format("_query{0}_columnIndexesAssigned", _customQueryIdx));
                _ctorIlBuilder.Ldc(0);
                _ctorIlBuilder.Stfld(indexesAssignedField);

                foreach (var col in RepositoryDefBuilder.GetAssignableColumnsForType(RepoDef.ColumnNamingConvention, MethodDef.ReturnType))
                {
                    // private _queryX_columnYIdx = 0;
                    var field = DefineStaticField<int>(string.Format("_query{0}_column{1}Idx", _customQueryIdx, col.PropertyName));
                    columnIndexFields[col.PropertyName] = field;
                    _ctorIlBuilder.Ldc(0);
                    _ctorIlBuilder.Stfld(field);
                }
            }

            if (NewConnectionEveryTime && MethodDef.EntityDef != null && MethodDef.EntityDef.Type != typeof(void) && MethodDef.ReturnType.Equals(typeof(IEnumerable<>).MakeGenericType(MethodDef.EntityDef.Type)))
            {
                //GenerateCodeForEnumerableGetMethod();
            }
            else
            {
                GenerateGetMethodBody(_customQueryIdx, indexesAssignedField, columnIndexFields);
            }
        }

        private static readonly MethodInfo _disposeMethod = typeof(IDisposable).GetMethod("Dispose", Type.EmptyTypes);

        private void GenerateGetMethodBody(int queryIdx, FieldBuilder indexesAssignedField, IDictionary<string, FieldBuilder> columnIndexFields)
        {
            Type typeToGet = MethodDef.ReturnType.GetCoreType();
            PropertyDef[] columnsToGet = DetermineColumnsToGet(typeToGet);

            WriteSqlStatement(columnsToGet);

            WriteParameterAssignmentsFromArgList();

            if (MethodDef.IsScalarQuery)
            {
                // var __result = cmd.ExecuteScalar();
                // return Convert.ToSomething(__result);
                ExecuteScalar();
                EmitScalarConversion(MethodDef.ReturnType);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);
            }
            else
            {
                var readerLocal = IlBuilder.DeclareLocal(typeof(IDataReader));

                // using (var reader = cmd.ExecuteReader())
                IlBuilder.BeginExceptionBlock();

                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, CommandLocal);
                IlBuilder.Call(_executeReaderMethod);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, readerLocal);

                if (MethodDef.IsSingleton)
                {
                    WriteSingletonResultRead(readerLocal, columnsToGet, queryIdx, indexesAssignedField, columnIndexFields);
                }
                else if (MethodDef.ReturnType.GetCoreType().IsDatabaseType())
                {
                    WriteMultiRowSimpleTypeRead(readerLocal);
                }
                else
                {
                    WriteMultiRowResultRead(readerLocal, queryIdx, indexesAssignedField, columnsToGet, columnIndexFields);
                }

                IlBuilder.BeginFinallyBlock();

                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
                IlBuilder.Call(_disposeMethod);

                IlBuilder.EndExceptionBlock();
            }
        }

        private static readonly MethodInfo _verifyFieldsAreUniqueMethod = typeof(ReaderHelper).GetMethod("VerifyFieldsAreUnique");
        private static readonly MethodInfo _getIndexForColumn = typeof(ReaderHelper).GetMethod("GetIndexForColumn");
        private static readonly MethodInfo _readMethod = typeof(IDataReader).GetMethod("Read", Type.EmptyTypes);
        
        private void WriteSingletonResultRead(LocalBuilder readerLocal, PropertyDef[] columnsToGet, int queryIdx, FieldBuilder indexesAssignedField, IDictionary<string, FieldBuilder> columnIndexFields)
        {
            LocalBuilder returnValue;
            if (MethodDef.IsTryGet)
            {
                returnValue = IlBuilder.ILGenerator.DeclareLocal(EntityDef.Type);
            }
            else
            {
                returnValue = ReturnValueLocal;
            }
            
            EmitArgumentMappingCheck(readerLocal, indexesAssignedField, columnsToGet, columnIndexFields);

            var afterRead = IlBuilder.DefineLabel();

            // if (reader.Read())
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlBuilder.Call(_readMethod);
            IlBuilder.ILGenerator.Emit(OpCodes.Brfalse, afterRead);

            if (MethodDef.CustomSqlOrNull != null)
            {
                AppendObjectSerialization(readerLocal, returnValue, columnsToGet.ToList(), Enumerable.Empty<ParameterDetails>(), queryIdx, columnIndexFields);
            }
            else
            {
                AppendObjectSerialization(readerLocal, returnValue, columnsToGet.ToList(), MethodDef.Properties, null, columnIndexFields);
            }
            
            if ((MethodDef.SingletonGetMethodBehavior & SingletonGetMethodBehavior.FailIfMultipleRowsFound) != 0)
            {
                var noMoreRowsFound = IlBuilder.DefineLabel();

                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
                IlBuilder.Call(_readMethod);
                IlBuilder.ILGenerator.Emit(OpCodes.Brfalse, noMoreRowsFound);

                ThrowRepomatException("More than one row returned from singleton query");

                IlBuilder.MarkLabel(noMoreRowsFound);
            }

            var afterElse = IlBuilder.DefineLabel();

            if (MethodDef.IsTryGet)
            {
                //var tryGetOutColumn = MethodDef.OutParameterOrNull;
                //CodeBuilder.WriteLine("{0} = newObj;", tryGetOutColumn.Name);
                //CodeBuilder.WriteLine("return true;");
                //CodeBuilder.CloseBrace();
                //CodeBuilder.WriteLine("{0} = default({1});", tryGetOutColumn.Name, EntityDef.Type.ToCSharp());
                //CodeBuilder.WriteLine("return false;");
                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, MethodDef.OutParameterOrNull.Index);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, returnValue);
                IlBuilder.ILGenerator.Emit(OpCodes.Stind_Ref);
                IlBuilder.Ldc(1);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);

                IlBuilder.ILGenerator.Emit(OpCodes.Br, afterElse);
                IlBuilder.MarkLabel(afterRead);

                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, MethodDef.OutParameterOrNull.Index);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldnull);
                IlBuilder.ILGenerator.Emit(OpCodes.Stind_Ref);
                IlBuilder.Ldc(0);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);
            }
            else
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Br, afterElse);
                IlBuilder.MarkLabel(afterRead);

                if ((MethodDef.SingletonGetMethodBehavior & SingletonGetMethodBehavior.FailIfNoRowFound) != 0)
                {
                    ThrowRepomatException("No rows returned from singleton query");
                }
                else
                {
                    // return default(X);
                    IlBuilder.ILGenerator.Emit(OpCodes.Ldnull);
                    IlBuilder.ILGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);
                }
            }

            IlBuilder.MarkLabel(afterElse);

        }

        private void WriteMultiRowSimpleTypeRead(LocalBuilder readerLocal)
        {
            var rowType = MethodDef.ReturnType.GetCoreType();
            bool isEnumerable = MethodDef.ReturnType.IsIEnumerableOfType(rowType);

            var listType = typeof(List<>).MakeGenericType(rowType);
            var ctor = listType.GetConstructor(Type.EmptyTypes);
            var addMethod = listType.GetMethod("Add", new Type[] { rowType });
            IlBuilder.ILGenerator.Emit(OpCodes.Newobj, ctor);

            var resultListLocal = IlBuilder.DeclareLocal(listType);
            IlBuilder.ILGenerator.Emit(OpCodes.Stloc, resultListLocal);

            var whileLoopStart = IlBuilder.DefineLabel();
            var whileLoopEnd = IlBuilder.DefineLabel();

            IlBuilder.MarkLabel(whileLoopStart);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlBuilder.Call(_readMethod);
            IlBuilder.ILGenerator.Emit(OpCodes.Brfalse, whileLoopEnd);

            // return PrimitiveTypeInfo.Get(t).GetReaderGetExpr(index, _useStrictTyping);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlBuilder.Ldc(0);
            IlBuilder.Call(_getValueMethod);
            PrimitiveTypeInfo.Get(rowType).EmitConversion(IlBuilder);

            var currentResultLocal = IlBuilder.DeclareLocal(rowType);
            IlBuilder.ILGenerator.Emit(OpCodes.Stloc, currentResultLocal);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, resultListLocal);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, currentResultLocal);
            IlBuilder.Call(addMethod);

            IlBuilder.ILGenerator.Emit(OpCodes.Br, whileLoopStart);

            IlBuilder.MarkLabel(whileLoopEnd);

            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, resultListLocal);

            if (MethodDef.ReturnType.IsArray)
            {
                var toArrayMethod = typeof(Enumerable).GetMethod("ToArray", BindingFlags.Static | BindingFlags.Public);
                var genericToArrayMethod = toArrayMethod.MakeGenericMethod(rowType);
                IlBuilder.Call(genericToArrayMethod);
            }

            IlBuilder.Ret();
        }

        private void EmitArgumentMappingCheck(LocalBuilder readerLocal, FieldBuilder indexesAssignedField, PropertyDef[] columnsToGet, IDictionary<string, FieldBuilder> columnIndexFields)
        {
            if (!MethodDef.IsScalarQuery && MethodDef.CustomSqlOrNull != null)
            {
                var afterIndexAssignment = IlBuilder.DefineLabel();

                // if (!_query{0}_columnIndexesAssigned)
                IlBuilder.ILGenerator.Emit(OpCodes.Ldsfld, indexesAssignedField);
                IlBuilder.ILGenerator.Emit(OpCodes.Brtrue, afterIndexAssignment);

                // Repomat.Runtime.ReaderHelper.VerifyFieldsAreUnique(reader);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
                IlBuilder.Call(_verifyFieldsAreUniqueMethod);

                foreach (var columnToGet in columnsToGet)
                {
                    // _query{0}_column{1}Idx = Repomat.Runtime.ReaderHelper.GetIndexForColumn(reader, \"{2}\");", queryIdx, columnToGet.PropertyName, columnToGet.ColumnName
                    IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
                    IlBuilder.ILGenerator.Emit(OpCodes.Ldstr, columnToGet.ColumnName);
                    IlBuilder.Call(_getIndexForColumn);
                    IlBuilder.Stfld(columnIndexFields[columnToGet.PropertyName]);
                }

                IlBuilder.MarkLabel(afterIndexAssignment);
            }
        }

        private static Type GetListType(Type elementType)
        {
            Type listType = typeof(List<>).MakeGenericType(elementType);
            return listType;
        }

        private void WriteMultiRowResultRead(LocalBuilder readerLocal, int queryIdx, FieldBuilder indexesAssignedField, PropertyDef[] columnsToGet, IDictionary<string, FieldBuilder> columnIndexFields)
        {
//            EmitArgumentMappingCheck(readerLocal, indexesAssignedField, columnsToGet, columnIndexFields);

            bool isEnumerable = MethodDef.ReturnType.IsIEnumerableOfType(EntityDef.Type) && NewConnectionEveryTime;
            var listType = GetListType(EntityDef.Type);

            var rowLocal = IlBuilder.DeclareLocal(EntityDef.Type);

            LocalBuilder listLocalOrNull = null;

            if (!isEnumerable)
            {
                listLocalOrNull = IlBuilder.DeclareLocal(listType);
                IlBuilder.ILGenerator.Emit(OpCodes.Newobj, listType.GetConstructor(Type.EmptyTypes));
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, listLocalOrNull);
            }

            Label whileReaderReadStart = IlBuilder.DefineLabel();
            IlBuilder.MarkLabel(whileReaderReadStart);

            Label whileReaderReadEnd = IlBuilder.DefineLabel();

            // while (reader.Read())
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlBuilder.Call(_readMethod);
            IlBuilder.ILGenerator.Emit(OpCodes.Brfalse, whileReaderReadEnd);

            // The inside of the loop goes here.

            if (MethodDef.CustomSqlOrNull != null)
            {
                AppendObjectSerialization(readerLocal, rowLocal, columnsToGet.ToList(), Enumerable.Empty<ParameterDetails>(), queryIdx, columnIndexFields);
            }
            else
            {
                AppendObjectSerialization(readerLocal, rowLocal, columnsToGet.ToList(), MethodDef.Properties, null, columnIndexFields);
            }

            if (isEnumerable)
            {
//               CodeBuilder.WriteLine("yield return newObj;");
                throw new NotImplementedException();
            }
            else
            {
                // result.Add(newObj);

                var addMethod = listType.GetMethod("Add", new[] { EntityDef.Type });
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, listLocalOrNull);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, rowLocal);
                IlBuilder.Call(addMethod);
            }

            IlBuilder.ILGenerator.Emit(OpCodes.Br, whileReaderReadStart);

            IlBuilder.MarkLabel(whileReaderReadEnd);

            if (!isEnumerable)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, listLocalOrNull);
                if (MethodDef.ReturnType.IsArray)
                {
                    var toArrayMethod = typeof(System.Linq.Enumerable)
                        .GetMethod("ToArray", BindingFlags.Static | BindingFlags.Public)
                        .MakeGenericMethod(EntityDef.Type);
                    IlBuilder.Call(toArrayMethod);

                }
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);
            }
        }

        private void AppendObjectSerialization(LocalBuilder readerLocal, LocalBuilder resultLocal, IList<PropertyDef> selectColumns, IEnumerable<ParameterDetails> argColumns, int? queryIndexOrNull, IDictionary<string, FieldBuilder> readerIndexes)
        {
            var args = argColumns.ToArray();

            if (EntityDef.CreateClassThroughConstructor)
            {
                var types = EntityDef.Properties.Select(p => p.Type);
                var ctor = EntityDef.Type.GetConstructor(types.ToArray());

                foreach (var prop in EntityDef.Properties)
                {
                    int? columnIndex = selectColumns
                        .Select((p, i) => new { p, i })
                        .Where(p => p.p.PropertyName == prop.PropertyName)
                        .Select(p => (int?)p.i)
                        .FirstOrDefault();
                    if (columnIndex.HasValue)
                    {
                        EmitReaderGetExpression(readerLocal, columnIndex.Value, prop, queryIndexOrNull, readerIndexes);
                    }
                    else
                    {
                        var argIndex = argColumns
                            .Select((p, i) => new { p, i })
                            .Where(p => p.p.Name.Capitalize() == prop.PropertyName)
                            .Select(p => p.i).First();
                        IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, argIndex + 1);
                    }
                }

                IlBuilder.ILGenerator.Emit(OpCodes.Newobj, ctor);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, resultLocal);
            }
            else
            {
                // body.WriteLine("var newObj = new {0}();", EntityDef.Type.ToCSharp());
                var ctor = EntityDef.Type.GetConstructor(Type.EmptyTypes);
                IlBuilder.ILGenerator.Emit(OpCodes.Newobj, ctor);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, resultLocal);

                for (int i = 0; i < selectColumns.Count; i++)
                {
                    // body.WriteLine("newObj.{0} = {1};", selectColumns[i].PropertyName, GetReaderGetExpression(selectColumns[i].Type, indexExpr));
                    var setter = EntityDef.Type.GetProperty(selectColumns[i].PropertyName).GetSetMethod();
                    IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, resultLocal);
                    EmitReaderGetExpression(readerLocal, i, selectColumns[i], queryIndexOrNull, readerIndexes);
                    IlBuilder.Call(setter);
                }
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    var index = i + 1;

                    var setter = EntityDef.Type.GetProperty(arg.Name.Capitalize()).GetSetMethod();
                    IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, resultLocal);
                    IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, index);
                    IlBuilder.Call(setter);
                }
            }
        }

        private static readonly MethodInfo _getValueMethod = typeof(IDataRecord).GetMethod("GetValue");

        private void EmitReaderGetExpression(LocalBuilder readerLocal, int index, PropertyDef property, int? queryIndexOrNull, IDictionary<string, FieldBuilder> readerIndexes)
        {
            // return PrimitiveTypeInfo.Get(t).GetReaderGetExpr(index, _useStrictTyping);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, readerLocal);
            EmitIndexExpr(index, property.PropertyName, queryIndexOrNull, readerIndexes);
            IlBuilder.Call(_getValueMethod);
            PrimitiveTypeInfo.Get(property.Type).EmitConversion(IlBuilder);
        }

        private void EmitIndexExpr(int index, string propertyName, int? queryIndexOrNull, IDictionary<string, FieldBuilder> readerIndexes)
        {
            if (queryIndexOrNull.HasValue)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldsfld, readerIndexes[propertyName]);
            }
            else
            {
                IlBuilder.Ldc(index);
            }
        }

        protected virtual void WriteSqlStatement(PropertyDef[] columnsToGet)
        {
            if (MethodDef.CustomSqlOrNull != null)
            {
                SetCommandText(MethodDef.CustomSqlOrNull, MethodDef.CustomSqlIsStoredProcedure);
            }
            else
            {
                StringBuilder commandText = new StringBuilder();
                commandText.Append("select ");

                commandText.Append(string.Join(", ", columnsToGet.Select(c => string.Format("[{0}]", c.ColumnName.Capitalize()))));

                commandText.AppendFormat(" from {0} ", EntityDef.TableName);

                var argumentProperties = MethodDef.Parameters
                    .Select(p => EntityDef.Properties.FirstOrDefault(c => c.PropertyName == p.Name.Capitalize()))
                    .Where(p => p != null)
                    .ToArray();

                var equations = argumentProperties.Select(p => string.Format("[{0}] = @{1}", p.ColumnName, p.PropertyName.Uncapitalize())).ToArray();
                if (equations.Length > 0)
                {
                    commandText.Append(" where " + string.Join(" AND ", equations));
                }

                SetCommandText(commandText.ToString());
            }
        }

        private PropertyDef[] DetermineColumnsToGet(Type typeToGet)
        {
            PropertyDef[] columnsToGet;

            if (MethodDef.CustomSqlOrNull != null)
            {
                // TODO: Get this naming convention from the database instead of using noop.
                if (MethodDef.IsScalarQuery)
                {
                    columnsToGet = new PropertyDef[0];
                }
                else
                {
                    columnsToGet = RepositoryDefBuilder.GetAssignableColumnsForType(RepoDef.ColumnNamingConvention, typeToGet).ToArray();
                }
            }
            else
            {
                columnsToGet = EntityDef.Properties.Where(c => !MethodDef.Properties.Select(p => p.Name.Capitalize()).Contains(c.ColumnName)).ToArray();
            }

            return columnsToGet;
        }

        private void EmitScalarConversion(Type convertToType)
        {
            PrimitiveTypeInfo.Get(convertToType).EmitConversion(IlBuilder);
        }

    }
}
