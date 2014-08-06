using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Databases;
using Repomat.Schema;

namespace Repomat.CodeGen
{
    internal class SQLiteRepositoryClassBuilder<TType, TRepo> : SqlRepositoryClassBuilderBase<TType, TRepo>
    {
        public SQLiteRepositoryClassBuilder(RepositoryDef repositoryDef, bool newConnectionEveryTime)
            : base(repositoryDef, newConnectionEveryTime)
        {
        }

        internal override MethodBuilderFactory CreateMethodBuilderFactory(CodeBuilder builder)
        {
            return new SQLiteMethodBuilderFactory(builder, RepositoryDef, NewConnectionEveryTime);
        }
    }
}
