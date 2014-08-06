using Repomat.CodeGen;
using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class SqlServerMethodBuilderFactory : SqlMethodBuilderFactory
    {
        public SqlServerMethodBuilderFactory(CodeBuilder codeBuilder, RepositoryDef repoDef, bool newConnectionEveryTime)
            : base(codeBuilder, repoDef, newConnectionEveryTime)
        {
        }

        protected override string StatementSeparator { get { return ""; } }
        protected override string ScopeIdentityFunction { get { return "SCOPE_IDENTITY()"; } }
        protected override string ScopeIdentityDatatype { get { return "decimal"; } }
    }
}
