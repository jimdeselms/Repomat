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
    internal abstract class SqlMethodBuilderFactory
    {
        private readonly TypeBuilder _typeBuilder;
        private readonly IlBuilder _ctorBuilder;
        private readonly FieldInfo _connectionField;

        private readonly RepositoryDef _repoDef;
        private readonly bool _newConnectionEveryTime;
        private bool _useStrictTypes = false;
        private int _customQueryIdx = 0;

        public SqlMethodBuilderFactory(TypeBuilder typeBuilder, FieldInfo connectionField, IlBuilder ctorIlBuilder, RepositoryDef repoDef, bool newConnectionEveryTime)
        {
            _typeBuilder = typeBuilder;
            _connectionField = connectionField;
            _ctorBuilder = ctorIlBuilder;
            _repoDef = repoDef;
            _newConnectionEveryTime = newConnectionEveryTime;
        }

        protected TypeBuilder TypeBuilder { get { return _typeBuilder; } }
        protected FieldInfo ConnectionField { get { return _connectionField; } }
        protected RepositoryDef RepoDef { get { return _repoDef; } }
        protected bool NewConnectionEveryTime { get { return _newConnectionEveryTime; } }
        protected int CustomQueryIndex { get { return _customQueryIdx; } }
        protected bool UseStrictTypes { get { return _useStrictTypes; } }
        protected IlBuilder CtorBuilder { get { return _ctorBuilder; } }

        public virtual MethodBuilderBase Create(MethodDef method)
        {
            switch (method.MethodType)
            {
                case MethodType.CreateTable:
                    return new CreateTableMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime, (p, b) => MapPropertyToSqlDatatype(p, b));
                case MethodType.DropTable:
                    return new DropTableMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime);
                case MethodType.Get:
                    return new GetMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime, _customQueryIdx++, this, _useStrictTypes, _ctorBuilder);
                case MethodType.Delete:
                    return new DeleteMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime);
                case MethodType.GetCount:
                    return new GetCountMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime, _customQueryIdx++, this, _useStrictTypes, _ctorBuilder);
                case MethodType.Exists:
                    return new ExistsMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime, _customQueryIdx++, this, _useStrictTypes, _ctorBuilder);
                case MethodType.Custom:
                    {
                        if (method.ReturnType == typeof(void))
                        {
                            return new CustomMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime);
                        }
                        else
                        {
                            return new GetMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime, _customQueryIdx++, this, _useStrictTypes, _ctorBuilder);
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        protected abstract string MapPropertyToSqlDatatype(PropertyDef p, bool isIdentity);
    }
}
