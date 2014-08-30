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
                indexesAssignedField = DefineField<bool>(string.Format("_query{0}_columnIndexesAssigned", _customQueryIdx));
                _ctorIlBuilder.Emit(OpCodes.Ldc_I4, 0);
                _ctorIlBuilder.Emit(OpCodes.Stfld, indexesAssignedField);

                foreach (var col in RepositoryDefBuilder.GetAssignableColumnsForType(RepoDef.ColumnNamingConvention, MethodDef.ReturnType))
                {
                    // private _queryX_columnYIdx = 0;
                    var field = DefineField<int>(string.Format("_query{0}_column{1}Idx", _customQueryIdx, col.PropertyName));
                    columnIndexFields[col.PropertyName] = field;
                    _ctorIlBuilder.Emit(OpCodes.Ldc_I4, 0);
                    _ctorIlBuilder.Emit(OpCodes.Stfld, field);
                }
            }

            if (MethodDef.EntityDef != null && MethodDef.EntityDef.Type != typeof(void) && MethodDef.ReturnType.Equals(typeof(IEnumerable<>).MakeGenericType(MethodDef.EntityDef.Type)))
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
                IlGenerator.Emit(OpCodes.Ret);
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
                    throw new NotImplementedException();
//                    WriteMultiRowSimpleTypeRead();
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
            EmitArgumentMappingCheck(readerLocal, indexesAssignedField, columnsToGet, columnIndexFields);

            var afterRead = IlGenerator.DefineLabel();

            // if (reader.Read())
            IlGenerator.Emit(OpCodes.Ldloc, readerLocal);
            IlGenerator.Emit(OpCodes.Call, _readMethod);
            IlGenerator.Emit(OpCodes.Brfalse, afterRead);

            if (MethodDef.CustomSqlOrNull != null)
            {
                AppendObjectSerialization(readerLocal, ReturnValueLocal, columnsToGet.ToList(), Enumerable.Empty<ParameterDetails>(), queryIdx, columnIndexFields);
            }
            else
            {
                AppendObjectSerialization(readerLocal, ReturnValueLocal, columnsToGet.ToList(), MethodDef.Properties, null, columnIndexFields);
            }
            //if ((MethodDef.SingletonGetMethodBehavior & SingletonGetMethodBehavior.FailIfMultipleRowsFound) != 0)
            //{
            //    CodeBuilder.WriteLine("if (reader.Read())");
            //    CodeBuilder.OpenBrace();
            //    CodeBuilder.WriteLine("throw new Repomat.RepomatException(\"More than one row returned from singleton query\");");
            //    CodeBuilder.CloseBrace();
            //}

            //if (MethodDef.IsTryGet)
            //{
            //    var tryGetOutColumn = MethodDef.OutParameterOrNull;
            //    CodeBuilder.WriteLine("{0} = newObj;", tryGetOutColumn.Name);
            //    CodeBuilder.WriteLine("return true;");
            //    CodeBuilder.CloseBrace();
            //    CodeBuilder.WriteLine("{0} = default({1});", tryGetOutColumn.Name, EntityDef.Type.ToCSharp());
            //    CodeBuilder.WriteLine("return false;");
            //}
            //else
            //{
            //    CodeBuilder.WriteLine("return newObj;");
            //    CodeBuilder.CloseBrace();

            //    if ((MethodDef.SingletonGetMethodBehavior & SingletonGetMethodBehavior.FailIfNoRowFound) != 0)
            //    {
            //        CodeBuilder.WriteLine("throw new Repomat.RepomatException(\"No rows returned from singleton query\");");
            //    }
            //    else
            //    {
            //        CodeBuilder.WriteLine("return default({0});", EntityDef.Type.ToCSharp());
            //    }
            //}

            IlGenerator.MarkLabel(afterRead);
        }

        private void EmitArgumentMappingCheck(LocalBuilder readerLocal, FieldBuilder indexesAssignedField, PropertyDef[] columnsToGet, IDictionary<string, FieldBuilder> columnIndexFields)
        {
            if (!MethodDef.IsScalarQuery && MethodDef.CustomSqlOrNull != null)
            {
                var afterIndexAssignment = IlGenerator.DefineLabel();

                // if (!_query{0}_columnIndexesAssigned)
                IlGenerator.Emit(OpCodes.Ldfld, indexesAssignedField);
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
                    IlGenerator.Emit(OpCodes.Stfld, columnIndexFields[columnToGet.PropertyName]);
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

            bool isEnumerable = MethodDef.ReturnType.IsIEnumerableOfType(EntityDef.Type);
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

        private void AppendObjectSerialization(LocalBuilder readerLocal, LocalBuilder resultLocal, IReadOnlyList<PropertyDef> selectColumns, IEnumerable<ParameterDetails> argColumns, int? queryIndexOrNull, IDictionary<string, FieldBuilder> readerIndexes)
        {
            var args = argColumns.ToArray();

            if (EntityDef.CreateClassThroughConstructor)
            {
                //body.WriteLine("var newObj = new {0}(", EntityDef.Type.ToCSharp());

                //var argToExprMap = new Dictionary<string, string>();
                //for (int i = 0; i < selectColumns.Count; i++)
                //{
                //    var col = selectColumns[i];
                //    string indexExpr = GetIndexExpr(i, col.PropertyName, queryIndexOrNull);
                //    argToExprMap[col.PropertyName.Uncapitalize()] = GetReaderGetExpression(col.Type, indexExpr);
                //}
                //foreach (var arg in argColumns)
                //{
                //    argToExprMap[arg.Name] = arg.Name;
                //}

                //var arguments = new List<string>();
                //foreach (var prop in EntityDef.Properties)
                //{
                //    arguments.Add(argToExprMap[prop.PropertyName.Uncapitalize()]);
                //}

                //body.Write(string.Join(", ", arguments));

                //body.WriteLine(");");
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

                    // body.WriteLine("newObj.{0} = {1};", arg.Name.Capitalize(), arg.Name);
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
                IlGenerator.Emit(OpCodes.Ldloc, readerIndexes[propertyName]);
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
                SetCommandText(MethodDef.CustomSqlOrNull);
                if (MethodDef.CustomSqlIsStoredProcedure)
                {
                    throw new NotImplementedException();
//                    CodeBuilder.WriteLine("cmd.CommandType = System.Data.CommandType.StoredProcedure;");
                }
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
