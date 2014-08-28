using Repomat.CodeGen;
using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class DeleteMethodBuilder : MethodBuilderBase
    {
        internal DeleteMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
        }

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
        {
            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("delete from [{0}] where ", MethodDef.EntityDef.TableName);

            var equations = EntityDef.PrimaryKey.Select(c => string.Format("[{0}] = @{1}", c.ColumnName, c.PropertyName.Capitalize()));
            sql.AppendFormat(string.Join(" and ", equations));

            SetCommandText(sql.ToString());

            int propIndex = 0;
            for (int i = 0; i < MethodDef.Parameters.Count; i++)
            {
                if (MethodDef.Parameters[i].Type == EntityDef.Type)
                {
                    // Add 1, since the first entry is "this".
                    propIndex = i + 1;
                }
            }

            foreach (var key in EntityDef.PrimaryKey)
            {
                IlGenerator.BeginScope();

                var parm = IlGenerator.DeclareLocal(typeof(IDbDataParameter));

                AddSqlParameterFromProperty(parm, key.PropertyName, propIndex, key);

                IlGenerator.EndScope();
            }

            ExecuteNonQuery();
        }
    }
}
