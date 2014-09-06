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
    internal class CustomMethodBuilder : MethodBuilderBase
    {
        internal CustomMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
        }

        protected override void GenerateMethodIl(LocalBuilder localBuilder)
        {
            SetCommandText(MethodDef.CustomSqlOrNull, MethodDef.CustomSqlIsStoredProcedure);
            WriteParameterAssignmentsFromArgList();

            ExecuteNonQuery();
        }
    }
}
