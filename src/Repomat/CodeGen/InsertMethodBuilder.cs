using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class InsertMethodBuilder : MethodBuilder
    {
        internal InsertMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
        }

        public override void GenerateCode()
        {
            GenerateConnectionAndStatementHeader();

            CodeBuilder.Write("cmd.CommandText = @\"insert into {0} (", RepoDef.TableName);
            CodeBuilder.Write(string.Join(", ", RepoDef.Properties.Select(c => c.ColumnName)));
            CodeBuilder.Write(") values (");
            CodeBuilder.Write(string.Join(", ", RepoDef.Properties.Select(c => "@" + c.PropertyName)));
            CodeBuilder.WriteLine(")\";");

            foreach (var column in RepoDef.Properties)
            {
                string parmValue = GetParmValue(string.Format("{0}.{1}", MethodDef.DtoParameterOrNull.Name, column.PropertyName), column.Type);
                CodeBuilder.OpenBrace();
                CodeBuilder.WriteLine("var parm = cmd.CreateParameter();");
                CodeBuilder.WriteLine("parm.ParameterName = \"@{0}\";", column.PropertyName);
                CodeBuilder.WriteLine("parm.Value = {0};", parmValue);
                CodeBuilder.WriteLine("cmd.Parameters.Add(parm);");
                CodeBuilder.CloseBrace();
            }

            CodeBuilder.WriteLine("            cmd.ExecuteNonQuery();");

            GenerateMethodFooter();
        }
    }
}
