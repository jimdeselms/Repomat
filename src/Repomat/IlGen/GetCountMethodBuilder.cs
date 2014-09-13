using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class GetCountMethodBuilder : GetMethodBuilder
    {
        public GetCountMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, int customQueryIdx, SqlMethodBuilderFactory methodBuilderFactory, bool useStrictTyping, IlBuilder ctorIlBuilder)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime, customQueryIdx, methodBuilderFactory, useStrictTyping, ctorIlBuilder)
        {
        }

        protected override void WriteSqlStatement(Schema.PropertyDef[] columnsToGet)
        {
            var equations = MethodDef.Parameters.Select(p => string.Format("[{0}]=@{1}", EntityDef.FindPropertyByParameterName(p.Name).ColumnName, p.Name)).ToArray();

            string whereClause = "";
            if (equations.Length > 0)
            {
                whereClause = string.Format(" where {0}", string.Join(" AND ", equations));
            }

            string query = string.Format("select count(1) from [{0}]{1}", EntityDef.TableName, whereClause);
            SetCommandText(query);
        }
    }
}
