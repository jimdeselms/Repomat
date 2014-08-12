﻿using Repomat.Schema;
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
            var equations = MethodDef.Parameters.Select(p => string.Format("{0}=@{1}", EntityDef.FindPropertyByParameterName(p.Name).ColumnName, p.Name)).ToArray();

            string whereClause = "";
            if (equations.Length > 0)
            {
                whereClause = string.Format(" where {0}", string.Join(" AND ", equations));
            }

            string query = string.Format("select case count(1) when 0 then 0 else 1 end from {0}{1}", EntityDef.TableName, whereClause);
            GenerateCodeForSql(query);
        }
    }
}
