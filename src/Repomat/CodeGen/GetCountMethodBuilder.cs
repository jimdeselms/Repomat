using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class GetCountMethodBuilder : MethodBuilder
    {
        internal GetCountMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
        }

        public override void GenerateCode()
        {
            var equations = MethodDef.Parameters.Select(p => string.Format("{0}=@{1}", RepoDef.FindPropertyByParameterName(p.Name).ColumnName, p.Name));
            string query = string.Format("select count(1) from {0} where {1}", RepoDef.TableName, string.Join(" AND ", equations));
            GenerateCodeForSql(query);
        }
    }
}
