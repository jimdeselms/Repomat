using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class SqlServerRepoSqlBuilder : RepoSqlBuilder
    {
        public SqlServerRepoSqlBuilder(RepositoryDef repoDef, bool newConnectionEveryTime)
            : base(repoDef, newConnectionEveryTime)
        {
        }

        protected override SqlMethodBuilderFactory CreateMethodBuilderFactory(RepositoryDef repoDef, bool newConnectionEveryTime)
        {
            return new SqlServerMethodBuilderFactory(TypeBuilder, ConnectionField, CtorIlBuilder, repoDef, newConnectionEveryTime);
        }
    }
}
