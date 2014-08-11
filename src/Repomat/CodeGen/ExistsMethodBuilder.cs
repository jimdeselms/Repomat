using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class ExistsMethodBuilder : MethodBuilder
    {
        internal ExistsMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
        }

        public override void GenerateCode()
        {
            var equations = MethodDef.Parameters.Select(p => string.Format("{0}=@{1}", RepoDef.FindPropertyByParameterName(p.Name).ColumnName, p.Name));
            string query = string.Format("select case count(1) when 0 then 0 else 1 end from {0} where {1}", EntityDef.TableName, string.Join(" AND ", equations));
            GenerateCodeForSql(query);
        }
    }
}
