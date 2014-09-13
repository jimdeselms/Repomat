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
    internal class SQLiteTableExistsMethodBuilder : GetMethodBuilder
    {
        internal SQLiteTableExistsMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, int customQueryIdx, SqlMethodBuilderFactory methodBuilderFactory, bool useStrictTyping, IlBuilder ctorIlBuilder)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime, customQueryIdx, methodBuilderFactory, useStrictTyping, ctorIlBuilder)
        {
        }

        protected override void WriteSqlStatement(PropertyDef[] columnsToGet)
        {
            SetCommandText(string.Format("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{0}'", EntityDef.TableName));
        }
    }
}
