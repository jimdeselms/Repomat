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
            var equations = MethodDef.Parameters.Select(p => string.Format("{0}=@{1}", EntityDef.FindPropertyByParameterName(p.Name).ColumnName, p.Name)).ToArray();

            string whereClause = "";
            if (equations.Length > 0)
            {
                whereClause = string.Format(" where {0}", string.Join(" AND ", equations));
            }

            string query = string.Format("select count(1) from {0}{1}", EntityDef.TableName, whereClause);
            GenerateCodeForSql(query);
        }
    }
}
