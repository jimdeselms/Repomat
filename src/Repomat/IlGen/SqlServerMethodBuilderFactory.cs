﻿using Repomat.CodeGen;
using Repomat.Schema;
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
            else if (method.MethodType == MethodType.Create)
            {
                return new CreateMethodBuilder(TypeBuilder, ConnectionField, RepoDef, method, NewConnectionEveryTime, "", "SCOPE_IDENTITY()", typeof(decimal));
            }
            else
            {
                return base.Create(method);
            }
        }

        protected override string MapPropertyToSqlDatatype(PropertyDef p, bool isIdentity)
        {
            string width = p.StringWidthOrNull == null ? "MAX" : p.StringWidthOrNull.ToString();

            return PrimitiveTypeInfo.Get(p.Type).GetSqlDatatype(isIdentity, width);
        }
    }
}
