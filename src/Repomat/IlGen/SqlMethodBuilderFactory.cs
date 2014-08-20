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
                    return new CreateTableMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
