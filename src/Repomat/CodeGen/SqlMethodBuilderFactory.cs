using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal abstract class SqlMethodBuilderFactory : MethodBuilderFactory
    {
        private readonly bool _newConnectionEveryTime;
        private int _customQueryIdx = 1;
        private bool _useStrictTypes = false;

        public SqlMethodBuilderFactory(CodeBuilder codeBuilder, RepositoryDef repoDef, bool newConnectionEveryTime)
            : base(codeBuilder, repoDef)
        {
            _newConnectionEveryTime = newConnectionEveryTime;
        }

        protected bool NewConnectionEveryTime { get { return _newConnectionEveryTime; } }

        public override MethodBuilder Create(MethodDef methodDef, MethodType? methodType)
        {
            switch (methodType ?? methodDef.MethodType)
            {
                case MethodType.Create: return new CreateMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, StatementSeparator, ScopeIdentityFunction, ScopeIdentityDatatype, this);
                case MethodType.CreateTable: return new CreateTableMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, (p,b) => MapPropertyToSqlDatatype(p,b), this);
                case MethodType.Custom: return new CustomMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, this);
                case MethodType.Delete: return new DeleteMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, this);
                case MethodType.DropTable: return new DropTableMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, this);
                case MethodType.Exists: return new ExistsMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, this);
                case MethodType.Get: return new GetMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, _customQueryIdx++, this, _useStrictTypes);
                case MethodType.GetCount: return new GetCountMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, this);
                case MethodType.Insert: return new InsertMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, this);
                case MethodType.TableExists: return new TableExistsMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, this);
                case MethodType.Update: return new UpdateMethodBuilder(CodeBuilder, RepoDef, methodDef, _newConnectionEveryTime, this);
                default: throw new RepomatException("Unhandled method type {0}", methodDef.MethodType);
            }
        }

        /// <summary>
        /// Turns on strict typing; this improves query performance, but will cause exceptions if a column's datatype is
        /// not the exact same type of the corresponding property. For example, if a column is BIGINT, but the column's
        /// datatype is byte, this will fail if strict typing is turned on.
        /// </summary>
        public void UseStrictTyping()
        {
            _useStrictTypes = true;
        }

        protected virtual string MapPropertyToSqlDatatype(PropertyDef p, bool isIdentity)
        {
            string width = p.StringWidthOrNull == null ? "MAX" : p.StringWidthOrNull.ToString();

            return PrimitiveTypeInfo.Get(p.Type).GetSqlDatatype(isIdentity, width);
        }

        protected abstract string StatementSeparator { get; }
        protected abstract string ScopeIdentityFunction { get; }
        protected abstract string ScopeIdentityDatatype { get; }
    }
}
