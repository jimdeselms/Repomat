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
    internal class CreateTableMethodBuilder : MethodBuilderBase
    {
        public CreateTableMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
            
        }

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
        {
            SetCommandText("select 1");

            WriteCommandText();

            IlGenerator.EmitWriteLine("This is a test.");
            IlGenerator.Emit(OpCodes.Ret);
        }
    }
}
