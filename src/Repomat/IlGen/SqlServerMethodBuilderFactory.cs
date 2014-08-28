﻿using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class SqlServerMethodBuilderFactory : SqlMethodBuilderFactory
    {
        public SqlServerMethodBuilderFactory(TypeBuilder typeBuilder, FieldInfo connectionField, ILGenerator ctorIlBuilder, RepositoryDef repoDef, bool newConnectionEveryTime)
            : base(typeBuilder, connectionField, ctorIlBuilder, repoDef, newConnectionEveryTime)
        {
        }

        public override MethodBuilderBase Create(MethodDef method)
        {
            if (method.MethodType == MethodType.TableExists)
            {
                return new SqlServerTableExistsMethodBuilder(TypeBuilder, ConnectionField, RepoDef, method, NewConnectionEveryTime, CustomQueryIndex, this, UseStrictTypes, CtorBuilder);
            }
            else
            {
                return base.Create(method);
            }
        }
    }
}
