using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class TableExistsMethodBuilder : MethodBuilder
    {
        internal TableExistsMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
        }

        public override void GenerateCode()
        {
            GenerateConnectionAndStatementHeader();

            CodeBuilder.WriteLine("cmd.CommandText = \"if exists (select 1 from information_schema.tables where table_type='BASE TABLE' and table_name=@t) SELECT 1 ELSE SELECT 0\";", RepoDef.TableName);
            CodeBuilder.WriteLine("var parm = cmd.CreateParameter();");
            CodeBuilder.WriteLine("parm.ParameterName = \"@t\";");
            CodeBuilder.WriteLine("parm.Value = \"{0}\";", RepoDef.TableName);
            CodeBuilder.WriteLine("cmd.Parameters.Add(parm);");
            CodeBuilder.WriteLine("return (int)cmd.ExecuteScalar() == 1;");

            GenerateMethodFooter();
        }
    }
}
