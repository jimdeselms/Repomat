using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class GetMethodBuilder : MethodBuilder
    {
        private readonly int _customQueryIdx;
        private bool _useStrictTyping;

        internal GetMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, int customQueryIdx, MethodBuilderFactory methodBuilderFactory, bool useStrictTyping)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
            _customQueryIdx = customQueryIdx;
            _useStrictTyping = useStrictTyping;
        }

        public override void GenerateCode()
        {
            if (MethodDef.CustomSqlOrNull != null && !MethodDef.IsSimpleQuery)
            {
                CodeBuilder.WriteLine("private bool _query{0}_columnIndexesAssigned = false;", _customQueryIdx);
                foreach (var col in RepositoryDefBuilder.GetAssignableColumnsForType(NamingConvention.NoOp, MethodDef.ReturnType))
                {
                    CodeBuilder.WriteLine("private int _query{0}_column{1}Idx = 0;", _customQueryIdx, col.PropertyName);
                }
            }

            if (MethodDef.ReturnType.Equals(typeof(IEnumerable<>).MakeGenericType(RepoDef.EntityType)))
            {
                GenerateCodeForEnumerableGetMethod();
            }
            else
            {
                GenerateConnectionAndStatementHeader();
                GenerateGetMethodBody(_customQueryIdx);
                GenerateMethodFooter();
            }
        }

        private void GenerateCodeForEnumerableGetMethod()
        {
            GenerateConnectionAndStatementHeader();
            GenerateGetMethodBody(_customQueryIdx);
            GenerateMethodFooter();

            List<string> args = new List<string>();

            var wrapperMethod = MethodDef.CloneWithNewName(MethodDef.MethodName + "_Implementation");
            CodeBuilder.WriteLine(wrapperMethod.ToString());
            CodeBuilder.OpenBrace();

            CodeBuilder.WriteLine("return new Repomat.Runtime.ConcurrentlyLoadedCollection<{0}>({1}_Implementation({2}));\n",
                RepoDef.EntityType.ToCSharp(),
                MethodDef.MethodName,
                string.Join(", ", MethodDef.Parameters.Select(p => p.Name)));

            CodeBuilder.CloseBrace();
        }

        private void GenerateGetMethodBody(int queryIdx)
        {
            ParameterDetails tryGetOutColumn = null;
            if (MethodDef.IsTryGet)
            {
                tryGetOutColumn = MethodDef.OutParameterOrNull;
            }

            PropertyDef[] columnsToGet;
            if (MethodDef.CustomSqlOrNull != null)
            {
                Type typeToGet = MethodDef.ReturnType.GetCoreType();

                // TODO: Get this naming convention from the database instead of using noop.
                if (MethodDef.IsSimpleQuery)
                {
                    columnsToGet = new PropertyDef[0];
                }
                else
                {
                    columnsToGet = RepositoryDefBuilder.GetAssignableColumnsForType(NamingConvention.NoOp, typeToGet).ToArray();
                }
                CodeBuilder.Write("cmd.CommandText = \"{0}\";", MethodDef.CustomSqlOrNull.Replace("\"", "\"\""));
                if (MethodDef.CustomSqlIsStoredProcedure)
                {
                    CodeBuilder.WriteLine("cmd.CommandType = System.Data.CommandType.StoredProcedure;");
                }
            }
            else
            {
                columnsToGet = RepoDef.Properties.Where(c => !MethodDef.Properties.Select(p => p.Name.Capitalize()).Contains(c.ColumnName)).ToArray();
                CodeBuilder.Write("cmd.CommandText = \"select ");

                CodeBuilder.Write(string.Join(", ", columnsToGet.Select(c => c.ColumnName.Capitalize())));

                CodeBuilder.Write(" from {0} ", RepoDef.TableName);

                var argumentProperties = MethodDef.Parameters
                    .Select(p => RepoDef.Properties.FirstOrDefault(c => c.PropertyName == p.Name.Capitalize()))
                    .Where(p => p != null)
                    .ToArray();

                var equations = argumentProperties.Select(p => string.Format("{0} = @{1}", p.ColumnName, p.PropertyName.Uncapitalize())).ToArray();
                if (equations.Length > 0)
                {
                    CodeBuilder.Write(" where " + string.Join(" AND ", equations));
                }
                CodeBuilder.WriteLine("\";");
            }

            foreach (var arg in MethodDef.Parameters)
            {
                var column = RepoDef.Properties.FirstOrDefault(c => c.PropertyName == arg.Name.Capitalize());
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

                CodeBuilder.OpenBrace();
                CodeBuilder.WriteLine("var parm = cmd.CreateParameter();");
                CodeBuilder.WriteLine("parm.ParameterName = \"@{0}\";", column.PropertyName.Capitalize());
                CodeBuilder.WriteLine("parm.Value = {0};", GetParmValue(column.PropertyName.Uncapitalize(), column.Type));
                CodeBuilder.WriteLine("cmd.Parameters.Add(parm);");
                CodeBuilder.CloseBrace();
            }

            if (MethodDef.IsSimpleQuery)
            {
                CodeBuilder.WriteLine("var ___result = cmd.ExecuteScalar();");
                CodeBuilder.WriteLine("return {0};", GetScalarConvertExpression(MethodDef.ReturnType, "___result"));
            }
            else
            {
                CodeBuilder.WriteLine("using (var reader = cmd.ExecuteReader())");

                CodeBuilder.OpenBrace();

                if (MethodDef.IsSingleton)
                {
                    if (!MethodDef.IsSimpleQuery && MethodDef.CustomSqlOrNull != null)
                    {
                        CodeBuilder.WriteLine("if (!_query{0}_columnIndexesAssigned)", queryIdx);
                        CodeBuilder.OpenBrace();
                        CodeBuilder.WriteLine("Repomat.Runtime.ReaderHelper.VerifyFieldsAreUnique(reader);");
                        foreach (var columnToGet in columnsToGet)
                        {
                            CodeBuilder.WriteLine("_query{0}_column{1}Idx = Repomat.Runtime.ReaderHelper.GetIndexForColumn(reader, \"{1}\");", queryIdx, columnToGet.PropertyName);
                        }
                        CodeBuilder.CloseBrace();
                    }
                    CodeBuilder.WriteLine("if (reader.Read())");
                    CodeBuilder.OpenBrace();
                    if (MethodDef.CustomSqlOrNull != null)
                    {
                        AppendObjectSerialization(CodeBuilder, columnsToGet.ToList(), Enumerable.Empty<ParameterDetails>(), queryIdx);
                    }
                    else
                    {
                        AppendObjectSerialization(CodeBuilder, columnsToGet.ToList(), MethodDef.Properties, null);
                    }
                    if ((MethodDef.SingletonGetMethodBehavior & SingletonGetMethodBehavior.FailIfMultipleRowsFound) != 0)
                    {
                        CodeBuilder.WriteLine("if (reader.Read())");
                        CodeBuilder.OpenBrace();
                        CodeBuilder.WriteLine("throw new Repomat.RepomatException(\"More than one row returned from singleton query\");");
                        CodeBuilder.CloseBrace();
                    }

                    if (MethodDef.IsTryGet)
                    {
                        CodeBuilder.WriteLine("{0} = newObj;", tryGetOutColumn.Name);
                        CodeBuilder.WriteLine("return true;");
                        CodeBuilder.CloseBrace();
                        CodeBuilder.WriteLine("{0} = default({1});", tryGetOutColumn.Name, RepoDef.EntityType.ToCSharp());
                        CodeBuilder.WriteLine("return false;");
                    }
                    else
                    {
                        CodeBuilder.WriteLine("return newObj;");
                        CodeBuilder.CloseBrace();

                        if ((MethodDef.SingletonGetMethodBehavior & SingletonGetMethodBehavior.FailIfNoRowFound) != 0)
                        {
                            CodeBuilder.WriteLine("throw new Repomat.RepomatException(\"No rows returned from singleton query\");");
                        }
                        else
                        {
                            CodeBuilder.WriteLine("return default({0});", RepoDef.EntityType.ToCSharp());
                        }
                    }
                }
                else
                {
                    if (!MethodDef.IsSimpleQuery && MethodDef.CustomSqlOrNull != null)
                    {
                        CodeBuilder.WriteLine("if (!_query{0}_columnIndexesAssigned)", queryIdx);
                        CodeBuilder.OpenBrace();
                        CodeBuilder.WriteLine("Repomat.Runtime.ReaderHelper.VerifyFieldsAreUnique(reader);");
                        foreach (var columnToGet in columnsToGet)
                        {
                            CodeBuilder.WriteLine("_query{0}_column{1}Idx = Repomat.Runtime.ReaderHelper.GetIndexForColumn(reader, \"{1}\");", queryIdx, columnToGet.PropertyName);
                        }
                        CodeBuilder.CloseBrace();
                    }
                    bool isEnumerable = MethodDef.ReturnType.IsIEnumerableOfType(RepoDef.EntityType);
                    if (!isEnumerable)
                    {
                        CodeBuilder.WriteLine("var result = new System.Collections.Generic.List<{0}>();", RepoDef.EntityType.ToCSharp());
                    }
                    CodeBuilder.WriteLine("while (reader.Read())");
                    CodeBuilder.OpenBrace();
                    if (MethodDef.CustomSqlOrNull != null)
                    {
                        AppendObjectSerialization(CodeBuilder, columnsToGet.ToList(), Enumerable.Empty<ParameterDetails>(), queryIdx);
                    }
                    else
                    {
                        AppendObjectSerialization(CodeBuilder, columnsToGet.ToList(), MethodDef.Properties, null);
                    }

                    if (isEnumerable)
                    {
                        CodeBuilder.WriteLine("yield return newObj;");
                    }
                    else
                    {
                        CodeBuilder.WriteLine("result.Add(newObj);");
                    }
                    CodeBuilder.CloseBrace();

                    if (!isEnumerable)
                    {
                        CodeBuilder.WriteLine("return result;");
                    }
                }
                CodeBuilder.CloseBrace();
            }
        }

        private void AppendObjectSerialization(CodeBuilder body, IReadOnlyList<PropertyDef> selectColumns, IEnumerable<ParameterDetails> argColumns, int? queryIndexOrNull)
        {
            if (RepoDef.CreateClassThroughConstructor)
            {
                body.WriteLine("var newObj = new {0}(", RepoDef.EntityType.ToCSharp());

                var argToExprMap = new Dictionary<string, string>();
                for (int i = 0; i < selectColumns.Count; i++)
                {
                    var col = selectColumns[i];
                    string indexExpr = GetIndexExpr(i, col.PropertyName, queryIndexOrNull);
                    argToExprMap[col.PropertyName.Uncapitalize()] = GetReaderGetExpression(col.Type, indexExpr);
                }
                foreach (var arg in argColumns)
                {
                    argToExprMap[arg.Name] = arg.Name;
                }

                var arguments = new List<string>();
                foreach (var prop in RepoDef.Properties)
                {
                    arguments.Add(argToExprMap[prop.PropertyName.Uncapitalize()]);
                }

                body.Write(string.Join(", ", arguments));

                body.WriteLine(");");
            }
            else
            {
                body.WriteLine("var newObj = new {0}();", RepoDef.EntityType.ToCSharp());

                for (int i = 0; i < selectColumns.Count; i++)
                {
                    string indexExpr = GetIndexExpr(i, selectColumns[i].PropertyName, queryIndexOrNull);
                    body.WriteLine("newObj.{0} = {1};", selectColumns[i].PropertyName, GetReaderGetExpression(selectColumns[i].Type, indexExpr));
                }
                foreach (var arg in argColumns)
                {
                    body.WriteLine("newObj.{0} = {1};", arg.Name.Capitalize(), arg.Name);
                }
            }
        }

        private string GetScalarConvertExpression(Type t, string input)
        {
            return PrimitiveTypeInfo.Get(t).GetScalarConvertExpr(input);
        }

        private string GetReaderGetExpression(Type t, string index)
        {
            return PrimitiveTypeInfo.Get(t).GetReaderGetExpr(index, _useStrictTyping);
        }

        private string GetIndexExpr(int index, string propertyName, int? queryIndexOrNull)
        {
            if (queryIndexOrNull.HasValue)
            {
                return string.Format("_query{0}_column{1}Idx", queryIndexOrNull.Value, propertyName);
            }
            else
            {
                return index.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
