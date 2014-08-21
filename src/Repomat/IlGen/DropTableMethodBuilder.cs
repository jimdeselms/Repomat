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
    internal class DropTableMethodBuilder : MethodBuilderBase
    {
        public DropTableMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
        }

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
        {
            SetCommandText(string.Format("drop table [{0}]", MethodDef.EntityDef.TableName));
            ExecuteNonQuery();
        }
    }
}
