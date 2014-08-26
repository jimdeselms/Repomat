using System.Globalization;
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
        private static readonly MethodInfo verifyFieldsAreUniqueMethod = typeof(ReaderHelper).GetMethod("VerifyFieldsAreUnique");
        private static readonly MethodInfo getIndexForColumn = typeof(ReaderHelper).GetMethod("GetIndexForColumn");
        private static readonly MethodInfo readMethod = typeof(IDataReader).GetMethod("Read", Type.EmptyTypes);

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
        {
            //if (MethodDef.CustomSqlOrNull != null && !MethodDef.IsSimpleQuery)
            //{
            //    CodeBuilder.WriteLine("private bool _query{0}_columnIndexesAssigned = false;", _customQueryIdx);
            //    foreach (var col in RepositoryDefBuilder.GetAssignableColumnsForType(RepoDef.ColumnNamingConvention, MethodDef.ReturnType))
            //    {
            //        CodeBuilder.WriteLine("private int _query{0}_column{1}Idx = 0;", _customQueryIdx, col.PropertyName);
            //    }
            //}

            //if (MethodDef.EntityDef != null &&  MethodDef.EntityDef.Type != typeof(void) && MethodDef.ReturnType.Equals(typeof(IEnumerable<>).MakeGenericType(MethodDef.EntityDef.Type)))
            //{
            //    GenerateCodeForEnumerableGetMethod();
            //}
            //else
            //{
            //    GenerateConnectionAndStatementHeader();
            //    GenerateGetMethodBody(_customQueryIdx);
            //    GenerateMethodFooter();
            //}

            IlGenerator.Emit(OpCodes.Ldc_I4, 46);
        }

        private void GenerateCodeForEnumerableGetMethod()
        {
            //GenerateConnectionAndStatementHeader();
            //GenerateGetMethodBody(_customQueryIdx);
            //GenerateMethodFooter();

            //List<string> args = new List<string>();

            //var wrapperMethod = MethodDef.CloneWithNewName(MethodDef.MethodName + "_Implementation");
            //CodeBuilder.WriteLine(wrapperMethod.ToString());
            //CodeBuilder.OpenBrace();

            //CodeBuilder.WriteLine("return new Repomat.Runtime.ConcurrentlyLoadedCollection<{0}>({1}_Implementation({2}));\n",
            //    EntityDef.Type.ToCSharp(),
            //    MethodDef.MethodName,
            //    string.Join(", ", MethodDef.Parameters.Select(p => p.Name)));

            //CodeBuilder.CloseBrace();
        }

        private void GenerateGetMethodBody(int queryIdx)
        {
            //Type typeToGet = MethodDef.ReturnType.GetCoreType();
            //PropertyDef[] columnsToGet = DetermineColumnsToGet(typeToGet);

            //WriteSqlStatement(columnsToGet);

            //WriteParameterAssignments();

            //if (MethodDef.IsSimpleQuery)
            //{
            //    CodeBuilder.WriteLine("var ___result = cmd.ExecuteScalar();");
            //    CodeBuilder.WriteLine("return {0};", GetScalarConvertExpression(MethodDef.ReturnType, "___result"));
            //}
            //else
            //{
            //    CodeBuilder.WriteLine("using (var reader = cmd.ExecuteReader())");

            //    CodeBuilder.OpenBrace();

            //    if (MethodDef.IsSingleton)
            //    {
            //        WriteSingletonResultRead(columnsToGet, queryIdx);
            //    }
            //    else if (MethodDef.ReturnType.GetCoreType().IsDatabaseType())
            //    {
            //        WriteMultiRowSimpleTypeRead();
            //    }
            //    else
            //    {
            //        WriteMultiRowResultRead(columnsToGet, queryIdx);
            //    }
            //    CodeBuilder.CloseBrace();
            //}
        }

        private PropertyDef[] DetermineColumnsToGet(Type typeToGet)
        {
            //PropertyDef[] columnsToGet;

            //if (MethodDef.CustomSqlOrNull != null)
            //{
            //    // TODO: Get this naming convention from the database instead of using noop.
            //    if (MethodDef.IsSimpleQuery)
            //    {
            //        columnsToGet = new PropertyDef[0];
            //    }
            //    else
            //    {
            //        columnsToGet = RepositoryDefBuilder.GetAssignableColumnsForType(RepoDef.ColumnNamingConvention, typeToGet).ToArray();
            //    }
            //}
            //else
            //{
            //    columnsToGet = EntityDef.Properties.Where(c => !MethodDef.Properties.Select(p => p.Name.Capitalize()).Contains(c.ColumnName)).ToArray();
            //}

            //return columnsToGet;
            return null;
        }

        private void WriteSqlStatement(PropertyDef[] columnsToGet)
        {
            StringBuilder sql = new StringBuilder();

            if (MethodDef.CustomSqlOrNull != null)
            {
                sql.AppendFormat("cmd.CommandText = @\"{0}\";", MethodDef.CustomSqlOrNull.Replace("\"", "\"\""));
                if (MethodDef.CustomSqlIsStoredProcedure)
                {
                    sql.AppendFormat("cmd.CommandType = System.Data.CommandType.StoredProcedure;");
                }
            }
            else
            {
                sql.AppendFormat("cmd.CommandText = \"select ");

                sql.AppendFormat(string.Join(", ", columnsToGet.Select(c => string.Format("[{0}]", c.ColumnName.Capitalize()))));

                sql.AppendFormat(" from {0} ", EntityDef.TableName);

                var argumentProperties = MethodDef.Parameters
                    .Select(p => EntityDef.Properties.FirstOrDefault(c => c.PropertyName == p.Name.Capitalize()))
                    .Where(p => p != null)
                    .ToArray();

                var equations = argumentProperties.Select(p => string.Format("[{0}] = @{1}", p.ColumnName, p.PropertyName.Uncapitalize())).ToArray();
                if (equations.Length > 0)
                {
                    sql.AppendFormat(" where " + string.Join(" AND ", equations));
                }
            }

            SetCommandText(sql.ToString());
        }

        private void WriteParameterAssignments()
        {
            //foreach (var arg in MethodDef.Parameters)
            //{
            //    var column = EntityDef.Properties.FirstOrDefault(c => c.PropertyName == arg.Name.Capitalize());
            //    if (column == null)
            //    {
            //        if (MethodDef.CustomSqlOrNull != null)
            //        {
            //            column = new PropertyDef(arg.Name, arg.Name, typeof(void));
            //        }
            //        else
            //        {
            //            continue;
            //        }
            //    }

            //    CodeBuilder.OpenBrace();
            //    CodeBuilder.WriteLine("var parm = cmd.CreateParameter();");
            //    CodeBuilder.WriteLine("parm.ParameterName = \"@{0}\";", column.PropertyName.Capitalize());
            //    CodeBuilder.WriteLine("parm.Value = {0};", GetParmValue(column.PropertyName.Uncapitalize(), column.Type));
            //    CodeBuilder.WriteLine("cmd.Parameters.Add(parm);");
            //    CodeBuilder.CloseBrace();
            //}
        }

        private void WriteSingletonResultRead(PropertyDef[] columnsToGet, int queryIdx)
        {
            //if (!MethodDef.IsSimpleQuery && MethodDef.CustomSqlOrNull != null)
            //{
            //    CodeBuilder.WriteLine("if (!_query{0}_columnIndexesAssigned)", queryIdx);
            //    CodeBuilder.OpenBrace();
            //    CodeBuilder.WriteLine("Repomat.Runtime.ReaderHelper.VerifyFieldsAreUnique(reader);");
            //    foreach (var columnToGet in columnsToGet)
            //    {
            //        CodeBuilder.WriteLine("_query{0}_column{1}Idx = Repomat.Runtime.ReaderHelper.GetIndexForColumn(reader, \"{2}\");", queryIdx, columnToGet.PropertyName, columnToGet.ColumnName);
            //    }
            //    CodeBuilder.CloseBrace();
            //}
            //CodeBuilder.WriteLine("if (reader.Read())");
            //CodeBuilder.OpenBrace();
            //if (MethodDef.CustomSqlOrNull != null)
            //{
            //    AppendObjectSerialization(CodeBuilder, columnsToGet.ToList(), Enumerable.Empty<ParameterDetails>(), queryIdx);
            //}
            //else
            //{
            //    AppendObjectSerialization(CodeBuilder, columnsToGet.ToList(), MethodDef.Properties, null);
            //}
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
        }

        private void WriteMultiRowResultRead(PropertyDef[] columnsToGet, int queryIdx)
        {
            //if (!MethodDef.IsSimpleQuery && MethodDef.CustomSqlOrNull != null)
            //{
            //    CodeBuilder.WriteLine("if (!_query{0}_columnIndexesAssigned)", queryIdx);
            //    CodeBuilder.OpenBrace();
            //    CodeBuilder.WriteLine("Repomat.Runtime.ReaderHelper.VerifyFieldsAreUnique(reader);");
            //    foreach (var columnToGet in columnsToGet)
            //    {
            //        CodeBuilder.WriteLine("_query{0}_column{1}Idx = Repomat.Runtime.ReaderHelper.GetIndexForColumn(reader, \"{2}\");", queryIdx, columnToGet.PropertyName, columnToGet.ColumnName);
            //    }
            //    CodeBuilder.CloseBrace();
            //}
            //bool isEnumerable = MethodDef.ReturnType.IsIEnumerableOfType(EntityDef.Type);
            //if (!isEnumerable)
            //{
            //    CodeBuilder.WriteLine("var result = new System.Collections.Generic.List<{0}>();", EntityDef.Type.ToCSharp());
            //}
            //CodeBuilder.WriteLine("while (reader.Read())");
            //CodeBuilder.OpenBrace();
            //if (MethodDef.CustomSqlOrNull != null)
            //{
            //    AppendObjectSerialization(CodeBuilder, columnsToGet.ToList(), Enumerable.Empty<ParameterDetails>(), queryIdx);
            //}
            //else
            //{
            //    AppendObjectSerialization(CodeBuilder, columnsToGet.ToList(), MethodDef.Properties, null);
            //}

            //if (isEnumerable)
            //{
            //    CodeBuilder.WriteLine("yield return newObj;");
            //}
            //else
            //{
            //    CodeBuilder.WriteLine("result.Add(newObj);");
            //}
            //CodeBuilder.CloseBrace();

            //if (!isEnumerable)
            //{
            //    string toArray = "";
            //    if (MethodDef.ReturnType.IsArray)
            //    {
            //        toArray = ".ToArray()";
            //    }
            //    CodeBuilder.WriteLine("return result{0};", toArray);
            //}
        }

        private void WriteMultiRowSimpleTypeRead()
        {
            //var rowType = MethodDef.ReturnType.GetCoreType();
            //bool isEnumerable = MethodDef.ReturnType.IsIEnumerableOfType(rowType);
            //if (!isEnumerable)
            //{
            //    CodeBuilder.WriteLine("var result = new System.Collections.Generic.List<{0}>();", rowType.ToCSharp());
            //}
            //CodeBuilder.WriteLine("while (reader.Read())");
            //CodeBuilder.OpenBrace();

            //if (isEnumerable)
            //{
            //    CodeBuilder.WriteLine("yield return {0};", GetReaderGetExpression(rowType, "0"));
            //}
            //else
            //{
            //    CodeBuilder.WriteLine("result.Add({0});", GetReaderGetExpression(rowType, "0"));
            //}

            //CodeBuilder.CloseBrace();

            //if (!isEnumerable)
            //{
            //    string toArray = "";
            //    if (MethodDef.ReturnType.IsArray)
            //    {
            //        toArray = ".ToArray()";
            //    }
            //    CodeBuilder.WriteLine("return result{0};", toArray);
            //}
        }

        private void AppendObjectSerialization(CodeBuilder body, IReadOnlyList<PropertyDef> selectColumns, IEnumerable<ParameterDetails> argColumns, int? queryIndexOrNull)
        {
            //if (EntityDef.CreateClassThroughConstructor)
            //{
            //    body.WriteLine("var newObj = new {0}(", EntityDef.Type.ToCSharp());

            //    var argToExprMap = new Dictionary<string, string>();
            //    for (int i = 0; i < selectColumns.Count; i++)
            //    {
            //        var col = selectColumns[i];
            //        string indexExpr = GetIndexExpr(i, col.PropertyName, queryIndexOrNull);
            //        argToExprMap[col.PropertyName.Uncapitalize()] = GetReaderGetExpression(col.Type, indexExpr);
            //    }
            //    foreach (var arg in argColumns)
            //    {
            //        argToExprMap[arg.Name] = arg.Name;
            //    }

            //    var arguments = new List<string>();
            //    foreach (var prop in EntityDef.Properties)
            //    {
            //        arguments.Add(argToExprMap[prop.PropertyName.Uncapitalize()]);
            //    }

            //    body.Write(string.Join(", ", arguments));

            //    body.WriteLine(");");
            //}
            //else
            //{
            //    body.WriteLine("var newObj = new {0}();", EntityDef.Type.ToCSharp());

            //    for (int i = 0; i < selectColumns.Count; i++)
            //    {
            //        string indexExpr = GetIndexExpr(i, selectColumns[i].PropertyName, queryIndexOrNull);
            //        body.WriteLine("newObj.{0} = {1};", selectColumns[i].PropertyName, GetReaderGetExpression(selectColumns[i].Type, indexExpr));
            //    }
            //    foreach (var arg in argColumns)
            //    {
            //        body.WriteLine("newObj.{0} = {1};", arg.Name.Capitalize(), arg.Name);
            //    }
            //}
        }

        //private string GetScalarConvertExpression(Type t, string input)
        //{
        //    return PrimitiveTypeInfo.Get(t).GetScalarConvertExpr(input);
        //}

        //private string GetReaderGetExpression(Type t, string index)
        //{
        //    return PrimitiveTypeInfo.Get(t).GetReaderGetExpr(index, _useStrictTyping);
        //}

        //private string GetIndexExpr(int index, string propertyName, int? queryIndexOrNull)
        //{
        //    if (queryIndexOrNull.HasValue)
        //    {
        //        return string.Format("_query{0}_column{1}Idx", queryIndexOrNull.Value, propertyName);
        //    }
        //    else
        //    {
        //        return index.ToString(CultureInfo.InvariantCulture);
        //    }
        //}
    }
}
