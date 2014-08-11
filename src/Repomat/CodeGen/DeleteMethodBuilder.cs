using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class DeleteMethodBuilder : MethodBuilder
    {
        internal DeleteMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
            GenerateConnectionAndStatementHeader();

            CodeBuilder.WriteLine("cmd.CommandText = @\"delete from {0} where ", MethodDef.EntityDef.TableName);

            var equations = RepoDef.PrimaryKey.Select(c => string.Format("{0} = @{1}", c.ColumnName, c.PropertyName.Capitalize()));
            CodeBuilder.Write(string.Join(" and ", equations));
            CodeBuilder.WriteLine("\";");

            foreach (var key in RepoDef.PrimaryKey)
            {
                string parmValue = GetParmValue(string.Format("{0}.{1}", methodDef.DtoParameterOrNull.Name, key.PropertyName), key.Type);
                CodeBuilder.OpenBrace();
                CodeBuilder.WriteLine("var parm = cmd.CreateParameter();");
                CodeBuilder.WriteLine("parm.ParameterName = \"@{0}\";", key.PropertyName.Capitalize());
                CodeBuilder.WriteLine("parm.Value = {0};", parmValue);
                CodeBuilder.WriteLine("cmd.Parameters.Add(parm);");
                CodeBuilder.CloseBrace();
            }

            CodeBuilder.WriteLine("cmd.ExecuteNonQuery();");

            GenerateMethodFooter();
        }

        public override void GenerateCode()
        {
        }
    }
}
