using System;
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
        private readonly ILGenerator _ctorBuilder;
        private readonly FieldInfo _connectionField;

        private readonly RepositoryDef _repoDef;
        private readonly bool _newConnectionEveryTime;
        private bool _useStrictTypes = false;
        private int _customQueryIdx = 0;

        public SqlMethodBuilderFactory(TypeBuilder typeBuilder, FieldInfo connectionField, ILGenerator ctorIlBuilder, RepositoryDef repoDef, bool newConnectionEveryTime)
        {
            _typeBuilder = typeBuilder;
            _connectionField = connectionField;
            _ctorBuilder = ctorIlBuilder;
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
                case MethodType.Get:
                    return new GetMethodBuilder(_typeBuilder, _connectionField, _repoDef, method, _newConnectionEveryTime, _customQueryIdx++, this, _useStrictTypes, _ctorBuilder);
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
