using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class UpdateMethodBuilder : MethodBuilder
    {
        internal UpdateMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
        }

        public override void GenerateCode()
        {
            GenerateConnectionAndStatementHeader();

            CodeBuilder.Write("cmd.CommandText = @\"update {0} set ", EntityDef.TableName);

            var whereColumns = EntityDef.PrimaryKey.ToArray();
            var columnsToSet = EntityDef.Properties.Where(c => whereColumns.All(p => p.ColumnName != c.ColumnName)).ToArray();

            var setEquations = columnsToSet.Select(c => string.Format("{0} = @{1}", c.ColumnName, c.PropertyName.Capitalize()));
            CodeBuilder.Write(string.Join(", ", setEquations));

            CodeBuilder.Write(" WHERE ");

            var whereEquations = whereColumns.Select(c => string.Format("{0} = @{1}", c.ColumnName, c.PropertyName.Capitalize()));
            CodeBuilder.Write(string.Join(" AND ", whereEquations));
            CodeBuilder.WriteLine("\";");

            foreach (var parm in EntityDef.Properties)
            {
                AddParameterToParameterList(parm);
            }

            CodeBuilder.WriteLine("cmd.ExecuteNonQuery();");

            GenerateMethodFooter();
        }
    }
}
