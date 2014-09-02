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
    internal abstract class UpsertMethodBuilder : InsertCreateUpdateMethodBuilderBase
    {
        public UpsertMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime) : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
        }

        protected override void GenerateMethodIl(LocalBuilder cmdVariable)
        {
            // If it's identity based, then "Upsert" means either "Create" or "Update".
            if (EntityDef.HasIdentity)
            {
                // If the primary key is 0, then we're doing a Create, otherwise an update.
                var propName = EntityDef.PrimaryKey[0].PropertyName;
                var prop = EntityDef.Type.GetProperty(propName).GetGetMethod();

                var doUpdate = IlGenerator.DefineLabel();
                var afterOperation = IlGenerator.DefineLabel();

                IlGenerator.Emit(OpCodes.Ldarg, MethodDef.DtoParameterOrNull.Index);
                IlGenerator.Emit(OpCodes.Call, prop);
                IlGenerator.Emit(OpCodes.Brfalse, doUpdate);

                GenerateIlForCreate(cmdVariable);

                IlGenerator.Emit(OpCodes.Br, afterOperation);
                IlGenerator.MarkLabel(doUpdate);

                GenerateIlForUpdate(cmdVariable);

                IlGenerator.MarkLabel(afterOperation);
            }
            else
            {
                GenerateIlForNonIdentityUpsert(cmdVariable);
            }
        }

        protected abstract void GenerateIlForNonIdentityUpsert(LocalBuilder cmdVariable);
    }
}
