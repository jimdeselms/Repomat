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

            CodeBuilder.Write("cmd.CommandText = @\"insert into {0} (", EntityDef.TableName);
            CodeBuilder.Write(string.Join(", ", RepoDef.Properties.Select(c => c.ColumnName)));
            CodeBuilder.Write(") values (");
            CodeBuilder.Write(string.Join(", ", RepoDef.Properties.Select(c => "@" + c.PropertyName)));
            CodeBuilder.WriteLine(")\";");

            foreach (var column in RepoDef.Properties)
            {
                AddParameterToParameterList(column);
            }

            CodeBuilder.WriteLine("            cmd.ExecuteNonQuery();");

            GenerateMethodFooter();
        }
    }
}
