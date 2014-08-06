using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal abstract class MethodBuilderFactory
    {
        private readonly CodeBuilder _codeBuilder;
        private readonly RepositoryDef _repoDef;

        protected MethodBuilderFactory(CodeBuilder codeBuilder, RepositoryDef repoDef)
        {
            _codeBuilder = codeBuilder;
            _repoDef = repoDef;
        }

        protected CodeBuilder CodeBuilder { get { return _codeBuilder; } }
        protected RepositoryDef RepoDef { get { return _repoDef; } }

        public abstract MethodBuilder Create(MethodDef methodDef, MethodType? methodType=null);
    }
}
