using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class CreateMethodBuilder : InsertCreateUpdateMethodBuilderBase
    {
        private readonly string _statementSeparator;
        private readonly string _scopeIdentityFunction;
        private readonly Type _scopeIdentityType;

        public CreateMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, string statementSeparator, string scopeIdentityFunction, Type scopeIdentityType)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime, statementSeparator, scopeIdentityType, scopeIdentityFunction)
        {
            _statementSeparator = statementSeparator;
            _scopeIdentityFunction = scopeIdentityFunction;
            _scopeIdentityType = scopeIdentityType;
        }

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
        {
            GenerateIlForCreate(cmdLocal);
        }
    }
}
