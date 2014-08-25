using Repomat.CodeGen;
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
            if (MethodDef.CustomSqlOrNull != null && !MethodDef.IsSimpleQuery)
            {
                // private _queryX_columnIndexesAssigned = false;
                var indexesAssignedField = DefineField<bool>(string.Format("_query{0}_columnIndexesAssigned", _customQueryIdx));
                _ctorIlBuilder.Emit(OpCodes.Ldc_I4, 0);
                _ctorIlBuilder.Emit(OpCodes.Stfld, indexesAssignedField);

                Dictionary<string, FieldBuilder> columnIndexFields = new Dictionary<string, FieldBuilder>();
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
                //GenerateConnectionAndStatementHeader();
                GenerateGetMethodBody(_customQueryIdx);
                //GenerateMethodFooter();
            }

            IlGenerator.Emit(OpCodes.Ldnull);
            IlGenerator.Emit(OpCodes.Ret);
        }

        private void GenerateGetMethodBody(int queryIdx)
        {
            Type typeToGet = MethodDef.ReturnType.GetCoreType();
            PropertyDef[] columnsToGet = DetermineColumnsToGet(typeToGet);

            WriteSqlStatement(columnsToGet);

            WriteParameterAssignments();

            if (MethodDef.IsSimpleQuery)
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

                IlGenerator.BeginExceptionBlock();

                IlGenerator.Emit(OpCodes.Ldloc, CommandLocal);
                IlGenerator.Emit(OpCodes.Call, _executeReaderMethod);
                IlGenerator.Emit(OpCodes.Stloc, readerLocal);

                IlGenerator.Emit(OpCodes.Ldnull);
                IlGenerator.Emit(OpCodes.Ret);

                IlGenerator.BeginFinallyBlock();
                IlGenerator.EndExceptionBlock();
                //CodeBuilder.WriteLine("using (var reader = cmd.ExecuteReader())");

                //CodeBuilder.OpenBrace();

                //if (MethodDef.IsSingleton)
                //{
                //    WriteSingletonResultRead(columnsToGet, queryIdx);
                //}
                //else if (MethodDef.ReturnType.GetCoreType().IsDatabaseType())
                //{
                //    WriteMultiRowSimpleTypeRead();
                //}
                //else
                //{
                //    WriteMultiRowResultRead(columnsToGet, queryIdx);
                //}
                //CodeBuilder.CloseBrace();
            }
        }

        private void WriteSqlStatement(PropertyDef[] columnsToGet)
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

        private void WriteParameterAssignments()
        {
            for (int argIndex = 0; argIndex < MethodDef.Parameters.Count; argIndex++)
            {
                ParameterDetails arg = MethodDef.Parameters[argIndex];

                var column = EntityDef.Properties.FirstOrDefault(c => c.PropertyName == arg.Name.Capitalize());
                if (column == null)
                {
                    if (MethodDef.CustomSqlOrNull != null)
                    {
                        column = new PropertyDef(arg.Name, arg.Name, typeof(void));
                    }
                    else
                    {
                        continue;
                    }
                }

                IlGenerator.BeginScope();

                var parmLocal = IlGenerator.DeclareLocal(typeof(IDbDataParameter));

                // Add one to the argument index; the first one is "this"
                base.AddSqlParameter(parmLocal, arg.Name, argIndex+1, arg.Type);

                IlGenerator.EndScope();
            }
        }
        private PropertyDef[] DetermineColumnsToGet(Type typeToGet)
        {
            PropertyDef[] columnsToGet;

            if (MethodDef.CustomSqlOrNull != null)
            {
                // TODO: Get this naming convention from the database instead of using noop.
                if (MethodDef.IsSimpleQuery)
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
