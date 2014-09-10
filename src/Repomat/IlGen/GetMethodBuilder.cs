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
        private readonly ILGenerator _ctorIlBuilder;
        private readonly SqlMethodBuilderFactory _methodBuilderFactory;

        private bool _useStrictTyping;

        internal GetMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, int customQueryIdx, SqlMethodBuilderFactory methodBuilderFactory, bool useStrictTyping, ILGenerator ctorIlBuilder)
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
                _ctorIlBuilder.Emit(OpCodes.Ldc_I4, 0);
                _ctorIlBuilder.Emit(OpCodes.Stsfld, indexesAssignedField);

                foreach (var col in RepositoryDefBuilder.GetAssignableColumnsForType(RepoDef.ColumnNamingConvention, MethodDef.ReturnType))
                {
                    // private _queryX_columnYIdx = 0;
                    var field = DefineStaticField<int>(string.Format("_query{0}_column{1}Idx", _customQueryIdx, col.PropertyName));
                    columnIndexFields[col.PropertyName] = field;
                    _ctorIlBuilder.Emit(OpCodes.Ldc_I4, 0);
                    _ctorIlBuilder.Emit(OpCodes.Stsfld, field);
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
                IlGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);
            }
            else
            {
                var readerLocal = IlGenerator.DeclareLocal(typeof(IDataReader));

                // using (var reader = cmd.ExecuteReader())
                IlGenerator.BeginExceptionBlock();

                IlGenerator.Emit(OpCodes.Ldloc, CommandLocal);
                IlGenerator.Emit(OpCodes.Call, _executeReaderMethod);
                IlGenerator.Emit(OpCodes.Stloc, readerLocal);

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
                
                IlGenerator.BeginFinallyBlock();

                IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
                IlGenerator.Emit(OpCodes.Callvirt, _disposeMethod);

                IlGenerator.EndExceptionBlock();
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
                returnValue = IlGenerator.DeclareLocal(EntityDef.Type);
            }
            else
            {
                returnValue = ReturnValueLocal;
            }
            
            EmitArgumentMappingCheck(readerLocal, indexesAssignedField, columnsToGet, columnIndexFields);

            var afterRead = IlGenerator.DefineLabel();

            // if (reader.Read())
            IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlGenerator.Emit(OpCodes.Call, _readMethod);
            IlGenerator.Emit(OpCodes.Brfalse, afterRead);

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
                var noMoreRowsFound = IlGenerator.DefineLabel();

                IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
                IlGenerator.Emit(OpCodes.Callvirt, _readMethod);
                IlGenerator.Emit(OpCodes.Brfalse, noMoreRowsFound);

                ThrowRepomatException("More than one row returned from singleton query");

                IlGenerator.MarkLabel(noMoreRowsFound);
            }

            var afterElse = IlGenerator.DefineLabel();

            if (MethodDef.IsTryGet)
            {
                //var tryGetOutColumn = MethodDef.OutParameterOrNull;
                //CodeBuilder.WriteLine("{0} = newObj;", tryGetOutColumn.Name);
                //CodeBuilder.WriteLine("return true;");
                //CodeBuilder.CloseBrace();
                //CodeBuilder.WriteLine("{0} = default({1});", tryGetOutColumn.Name, EntityDef.Type.ToCSharp());
                //CodeBuilder.WriteLine("return false;");
                IlGenerator.Emit(OpCodes.Ldarg, MethodDef.OutParameterOrNull.Index);
                IlGenerator.Emit(OpCodes.Ldloc, returnValue);
                IlGenerator.Emit(OpCodes.Stind_Ref);
                IlGenerator.Emit(OpCodes.Ldc_I4_1);
                IlGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);

                IlGenerator.Emit(OpCodes.Br, afterElse);
                IlGenerator.MarkLabel(afterRead);

                IlGenerator.Emit(OpCodes.Ldarg, MethodDef.OutParameterOrNull.Index);
                IlGenerator.Emit(OpCodes.Ldnull);
                IlGenerator.Emit(OpCodes.Stind_Ref);
                IlGenerator.Emit(OpCodes.Ldc_I4_0);
                IlGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);
            }
            else
            {
                IlGenerator.Emit(OpCodes.Br, afterElse);
                IlGenerator.MarkLabel(afterRead);

                if ((MethodDef.SingletonGetMethodBehavior & SingletonGetMethodBehavior.FailIfNoRowFound) != 0)
                {
                    ThrowRepomatException("No rows returned from singleton query");
                }
                else
                {
                    // return default(X);
                    IlGenerator.Emit(OpCodes.Ldnull);
                    IlGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);
                }
            }

            IlGenerator.MarkLabel(afterElse);

        }

        private void WriteMultiRowSimpleTypeRead(LocalBuilder readerLocal)
        {
            var rowType = MethodDef.ReturnType.GetCoreType();
            bool isEnumerable = MethodDef.ReturnType.IsIEnumerableOfType(rowType);

            var listType = typeof(List<>).MakeGenericType(rowType);
            var ctor = listType.GetConstructor(Type.EmptyTypes);
            var addMethod = listType.GetMethod("Add", new Type[] { rowType });
            IlGenerator.Emit(OpCodes.Newobj, ctor);

            var resultListLocal = IlGenerator.DeclareLocal(listType);
            IlGenerator.Emit(OpCodes.Stloc, resultListLocal);

            var whileLoopStart = IlGenerator.DefineLabel();
            var whileLoopEnd = IlGenerator.DefineLabel();

            IlGenerator.MarkLabel(whileLoopStart);
            IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlGenerator.Emit(OpCodes.Call, _readMethod);
            IlGenerator.Emit(OpCodes.Brfalse, whileLoopEnd);

            // return PrimitiveTypeInfo.Get(t).GetReaderGetExpr(index, _useStrictTyping);
            IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlGenerator.Emit(OpCodes.Ldc_I4_0);
            IlGenerator.Emit(OpCodes.Call, _getValueMethod);
            PrimitiveTypeInfo.Get(rowType).EmitConversion(IlGenerator);

            var currentResultLocal = IlGenerator.DeclareLocal(rowType);
            IlGenerator.Emit(OpCodes.Stloc, currentResultLocal);
            IlGenerator.Emit(OpCodes.Ldloc, resultListLocal);
            IlGenerator.Emit(OpCodes.Ldloc, currentResultLocal);
            IlGenerator.Emit(OpCodes.Call, addMethod);

            IlGenerator.Emit(OpCodes.Br, whileLoopStart);

            IlGenerator.MarkLabel(whileLoopEnd);

            IlGenerator.Emit(OpCodes.Ldloc, resultListLocal);

            if (MethodDef.ReturnType.IsArray)
            {
                var toArrayMethod = typeof(Enumerable).GetMethod("ToArray", BindingFlags.Static | BindingFlags.Public);
                var genericToArrayMethod = toArrayMethod.MakeGenericMethod(rowType);
                IlGenerator.Emit(OpCodes.Call, genericToArrayMethod);
            }

            IlGenerator.Emit(OpCodes.Ret);
        }

        private void EmitArgumentMappingCheck(LocalBuilder readerLocal, FieldBuilder indexesAssignedField, PropertyDef[] columnsToGet, IDictionary<string, FieldBuilder> columnIndexFields)
        {
            if (!MethodDef.IsScalarQuery && MethodDef.CustomSqlOrNull != null)
            {
                var afterIndexAssignment = IlGenerator.DefineLabel();

                // if (!_query{0}_columnIndexesAssigned)
                IlGenerator.Emit(OpCodes.Ldsfld, indexesAssignedField);
                IlGenerator.Emit(OpCodes.Brtrue, afterIndexAssignment);

                // Repomat.Runtime.ReaderHelper.VerifyFieldsAreUnique(reader);
                IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
                IlGenerator.Emit(OpCodes.Call, _verifyFieldsAreUniqueMethod);

                foreach (var columnToGet in columnsToGet)
                {
                    // _query{0}_column{1}Idx = Repomat.Runtime.ReaderHelper.GetIndexForColumn(reader, \"{2}\");", queryIdx, columnToGet.PropertyName, columnToGet.ColumnName
                    IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
                    IlGenerator.Emit(OpCodes.Ldstr, columnToGet.ColumnName);
                    IlGenerator.Emit(OpCodes.Call, _getIndexForColumn);
                    IlGenerator.Emit(OpCodes.Stsfld, columnIndexFields[columnToGet.PropertyName]);
                }

                IlGenerator.MarkLabel(afterIndexAssignment);
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

            var rowLocal = IlGenerator.DeclareLocal(EntityDef.Type);

            LocalBuilder listLocalOrNull = null;

            if (!isEnumerable)
            {
                listLocalOrNull = IlGenerator.DeclareLocal(listType);
                IlGenerator.Emit(OpCodes.Newobj, listType.GetConstructor(Type.EmptyTypes));
                IlGenerator.Emit(OpCodes.Stloc, listLocalOrNull);
            }

            Label whileReaderReadStart = IlGenerator.DefineLabel();
            IlGenerator.MarkLabel(whileReaderReadStart);

            Label whileReaderReadEnd = IlGenerator.DefineLabel();

            // while (reader.Read())
            IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlGenerator.Emit(OpCodes.Call, _readMethod);
            IlGenerator.Emit(OpCodes.Brfalse, whileReaderReadEnd);

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
                IlGenerator.Emit(OpCodes.Ldloc, listLocalOrNull);
                IlGenerator.Emit(OpCodes.Ldloc, rowLocal);
                IlGenerator.Emit(OpCodes.Callvirt, addMethod);
            }

            IlGenerator.Emit(OpCodes.Br, whileReaderReadStart);

            IlGenerator.MarkLabel(whileReaderReadEnd);

            if (!isEnumerable)
            {
                IlGenerator.Emit(OpCodes.Ldloc, listLocalOrNull);
                if (MethodDef.ReturnType.IsArray)
                {
                    var toArrayMethod = typeof(System.Linq.Enumerable)
                        .GetMethod("ToArray", BindingFlags.Static | BindingFlags.Public)
                        .MakeGenericMethod(EntityDef.Type);
                    IlGenerator.Emit(OpCodes.Call, toArrayMethod);

                }
                IlGenerator.Emit(OpCodes.Stloc, ReturnValueLocal);
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
                        IlGenerator.Emit(OpCodes.Ldarg, argIndex + 1);
                    }
                }

                IlGenerator.Emit(OpCodes.Newobj, ctor);
                IlGenerator.Emit(OpCodes.Stloc, resultLocal);
            }
            else
            {
                // body.WriteLine("var newObj = new {0}();", EntityDef.Type.ToCSharp());
                var ctor = EntityDef.Type.GetConstructor(Type.EmptyTypes);
                IlGenerator.Emit(OpCodes.Newobj, ctor);
                IlGenerator.Emit(OpCodes.Stloc, resultLocal);

                for (int i = 0; i < selectColumns.Count; i++)
                {
                    // body.WriteLine("newObj.{0} = {1};", selectColumns[i].PropertyName, GetReaderGetExpression(selectColumns[i].Type, indexExpr));
                    var setter = EntityDef.Type.GetProperty(selectColumns[i].PropertyName).GetSetMethod();
                    IlGenerator.Emit(OpCodes.Ldloc, resultLocal);
                    EmitReaderGetExpression(readerLocal, i, selectColumns[i], queryIndexOrNull, readerIndexes);
                    IlGenerator.Emit(OpCodes.Call, setter);
                }
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    var index = i + 1;

                    var setter = EntityDef.Type.GetProperty(arg.Name.Capitalize()).GetSetMethod();
                    IlGenerator.Emit(OpCodes.Ldloc, resultLocal);
                    IlGenerator.Emit(OpCodes.Ldarg, index);
                    IlGenerator.Emit(OpCodes.Call, setter);
                }
            }
        }

        private static readonly MethodInfo _getValueMethod = typeof(IDataRecord).GetMethod("GetValue");

        private void EmitReaderGetExpression(LocalBuilder readerLocal, int index, PropertyDef property, int? queryIndexOrNull, IDictionary<string, FieldBuilder> readerIndexes)
        {
            // return PrimitiveTypeInfo.Get(t).GetReaderGetExpr(index, _useStrictTyping);
            IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
            EmitIndexExpr(index, property.PropertyName, queryIndexOrNull, readerIndexes);
            IlGenerator.Emit(OpCodes.Call, _getValueMethod);
            PrimitiveTypeInfo.Get(property.Type).EmitConversion(IlGenerator);
        }

        private void EmitIndexExpr(int index, string propertyName, int? queryIndexOrNull, IDictionary<string, FieldBuilder> readerIndexes)
        {
            if (queryIndexOrNull.HasValue)
            {
                IlGenerator.Emit(OpCodes.Ldsfld, readerIndexes[propertyName]);
            }
            else
            {
                IlGenerator.Emit(OpCodes.Ldc_I4, index);
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
            PrimitiveTypeInfo.Get(convertToType).EmitConversion(IlGenerator);
        }

    }
}
