using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class SQLiteRepoSqlBuilder : RepoSqlBuilder
    {
        public SQLiteRepoSqlBuilder(RepositoryDef repoDef, bool newConnectionEveryTime, RepoConnectionType repoConnectionType)
            : base(repoDef, newConnectionEveryTime, repoConnectionType)
        {
        }

        protected override SqlMethodBuilderFactory CreateMethodBuilderFactory(RepositoryDef repoDef, bool newConnectionEveryTime)
        {
            return new SQLiteMethodBuilderFactory(TypeBuilder, ConnectionField, CtorIlBuilder, repoDef, newConnectionEveryTime);
        }
    }
}
