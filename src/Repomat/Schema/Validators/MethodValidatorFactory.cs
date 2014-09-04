using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema.Validators
{
    internal static class MethodValidatorFactory
    {
        public static MethodValidator Create(RepositoryDef repoDef, MethodDef methodDef, DatabaseType databaseType, IList<ValidationError> errors)
        {
            switch (methodDef.MethodType)
            {
                case MethodType.Get: return new GetMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.Insert: return new InsertMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.Delete: return new DeleteMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.Update: return new UpdateMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.Create: return new CreateMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.CreateTable: return new CreateTableMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.DropTable: return new DropTableMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.TableExists: return new TableExistsMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.GetCount: return new GetCountMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.Exists: return new ExistsMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.Custom: return new CustomMethodValidator(repoDef, methodDef, databaseType, errors);
                case MethodType.Upsert: return new UpsertMethodValidator(repoDef, methodDef, databaseType, errors);
                default: throw new RepomatException("Unknown method type {0}", methodDef.MethodType, databaseType, errors);
            }
        }
    }
}
