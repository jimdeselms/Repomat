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
    internal class SqlServerTableExistsMethodBuilder : GetMethodBuilder
    {
        internal SqlServerTableExistsMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, int customQueryIdx, SqlMethodBuilderFactory methodBuilderFactory, bool useStrictTyping, IlBuilder ctorIlBuilder)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime, customQueryIdx, methodBuilderFactory, useStrictTyping, ctorIlBuilder)
        {
        }

        protected override void WriteSqlStatement(PropertyDef[] columnsToGet)
        {
            SetCommandText(string.Format("if exists (select 1 from information_schema.tables where table_type='BASE TABLE' and table_name='{0}') SELECT 1 ELSE SELECT 0", EntityDef.TableName));
        }
    }
}   
