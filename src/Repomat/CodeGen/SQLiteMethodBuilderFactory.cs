using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class SQLiteMethodBuilderFactory : SqlMethodBuilderFactory
    {
        public SQLiteMethodBuilderFactory(CodeBuilder codeBuilder, RepositoryDef repoDef, bool newConnectionEveryTime)
            : base(codeBuilder, repoDef, newConnectionEveryTime)
        {
        }

        protected override string MapTypeToSqlDatatype(Type t, bool isIdentity)
        {
            if (t == typeof(string))
            {
                return "VARCHAR(999)" + (isIdentity ? " IDENTITY" : "");
            }
            else
            {
                return base.MapTypeToSqlDatatype(t, isIdentity);
            }
        }

        public override MethodBuilder Create(MethodDef methodDef, MethodType? methodType=null)
        {
            var type = methodType ?? methodDef.MethodType;

            if (type == MethodType.TableExists)
            {
                return new SQLiteTableExistsMethodBuilder(CodeBuilder, RepoDef, methodDef, NewConnectionEveryTime, this);
            }

            return base.Create(methodDef, methodType);
        }

        protected override string StatementSeparator { get { return "; "; } }
        protected override string ScopeIdentityFunction { get { return "last_insert_rowid()"; } }
        protected override string ScopeIdentityDatatype { get { return "long"; } }
    }
}
