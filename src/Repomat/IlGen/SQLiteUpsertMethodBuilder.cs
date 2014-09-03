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
    internal class SQLiteUpsertMethodBuilder : UpsertMethodBuilder
    {
        public SQLiteUpsertMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, string statementSeparator, Type scopeIdentityType, string scopeIdentityFunction)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime, statementSeparator, scopeIdentityType, scopeIdentityFunction)
        {
        }

        protected override void GenerateIlForNonIdentityUpsert(LocalBuilder cmdVariable)
        {
            GenerateIlForInsert(cmdVariable, " or replace ");
        }
    }
}
