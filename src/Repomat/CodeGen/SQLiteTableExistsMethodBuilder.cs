using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class SQLiteTableExistsMethodBuilder : TableExistsMethodBuilder
    {
        internal SQLiteTableExistsMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
        }

        public override void GenerateCode()
        {
            GenerateConnectionAndStatementHeader();

            CodeBuilder.WriteLine("cmd.CommandText = \"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{0}';\";", EntityDef.TableName);
            CodeBuilder.WriteLine("return (long)cmd.ExecuteScalar() == 1L;");

            GenerateMethodFooter();
        }
    }
}
