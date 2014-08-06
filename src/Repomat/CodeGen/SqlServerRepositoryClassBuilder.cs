using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class SqlServerRepositoryClassBuilder<TType, TRepo> : SqlRepositoryClassBuilderBase<TType, TRepo>
    {
        public SqlServerRepositoryClassBuilder(RepositoryDef repositoryDef, bool newConnectionEveryTime) : base(repositoryDef, newConnectionEveryTime)
        {
        }

        internal override MethodBuilderFactory CreateMethodBuilderFactory(CodeBuilder codeBuilder)
        {
            return new SqlServerMethodBuilderFactory(codeBuilder, RepositoryDef, NewConnectionEveryTime);
        }
    }
}

