﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Repomat.Schema;
using Repomat.CodeGen;

namespace Repomat.IlGen
{
    internal class SqlMethodBuilderFactory
    {
        private readonly TypeBuilder _typeBuilder;
        private readonly FieldInfo _connectionField;

        private readonly RepositoryDef _repoDef;
        private readonly bool _newConnectionEveryTime;

        public SqlMethodBuilderFactory(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, bool newConnectionEveryTime)
        {
            _typeBuilder = typeBuilder;
            _connectionField = connectionField;
            _repoDef = repoDef;
            _newConnectionEveryTime = newConnectionEveryTime;
        }

        public MethodBuilderBase Create(MethodDef method)
        {
            switch (method.MethodType)
            {
                case MethodType.CreateTable:
                    return new CreateTableMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime, (p, b) => MapPropertyToSqlDatatype(p, b));
                case MethodType.DropTable:
                    return new DropTableMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime);
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual string MapPropertyToSqlDatatype(PropertyDef p, bool isIdentity)
        {
            string width = p.StringWidthOrNull == null ? "MAX" : p.StringWidthOrNull.ToString();

            return PrimitiveTypeInfo.Get(p.Type).GetSqlDatatype(isIdentity, width);
        }
    }
}
