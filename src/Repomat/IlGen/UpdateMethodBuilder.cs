﻿using Repomat.CodeGen;
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
    internal class UpdateMethodBuilder : InsertCreateUpdateMethodBuilderBase
    {
        internal UpdateMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, string statementSeparator, Type scopeIdentityType, string scopeIdentityFunction)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime, statementSeparator, scopeIdentityType, scopeIdentityFunction)
        {
        }

        protected override void GenerateMethodIl(LocalBuilder cmdVariable)
        {
            GenerateIlForUpdate(cmdVariable);
        }
    }
}
