using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Repomat.Schema;

namespace Repomat.IlGen
{
    internal class SqlServerUpsertMethodBuilder : UpsertMethodBuilder
    {
        public SqlServerUpsertMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime) : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
        }

        protected override void GenerateIlForNonIdentityUpsert(LocalBuilder cmdVariable)
        {
            throw new NotImplementedException();
        }
    }
}
