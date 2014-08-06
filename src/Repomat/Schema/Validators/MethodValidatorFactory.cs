using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal static class MethodValidatorFactory
    {
        public static MethodValidator Create(RepositoryDef repoDef, MethodDef methodDef, IList<ValidationError> errors)
        {
            switch (methodDef.MethodType)
            {
                case MethodType.Get: return new GetMethodValidator(repoDef, methodDef, errors);
                case MethodType.Insert: return new InsertMethodValidator(repoDef, methodDef, errors);
                case MethodType.Delete: return new DeleteMethodValidator(repoDef, methodDef, errors);
                case MethodType.Update: return new UpdateMethodValidator(repoDef, methodDef, errors);
                case MethodType.Create: return new CreateMethodValidator(repoDef, methodDef, errors);
                case MethodType.CreateTable: return new CreateTableMethodValidator(repoDef, methodDef, errors);
                case MethodType.DropTable: return new DropTableMethodValidator(repoDef, methodDef, errors);
                case MethodType.TableExists: return new TableExistsMethodValidator(repoDef, methodDef, errors);
                case MethodType.GetCount: return new GetCountMethodValidator(repoDef, methodDef, errors);
                case MethodType.Exists: return new ExistsMethodValidator(repoDef, methodDef, errors);
                case MethodType.Custom: return new CustomMethodValidator(repoDef, methodDef, errors);
                default: throw new RepomatException("Unknown method type {0}", methodDef.MethodType, errors);
            }
        }
    }
}
