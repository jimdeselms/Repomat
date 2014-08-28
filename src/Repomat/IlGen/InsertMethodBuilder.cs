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
    internal class InsertMethodBuilder : MethodBuilderBase
    {
        internal InsertMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
        }

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
        {
            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("insert into [{0}] (", EntityDef.TableName);
            sql.AppendFormat(string.Join(", ", EntityDef.Properties.Select(c => string.Format("[{0}]", c.ColumnName))));
            sql.AppendFormat(") values (");
            sql.AppendFormat(string.Join(", ", EntityDef.Properties.Select(c => "@" + c.PropertyName)));
            sql.AppendFormat(")");

            SetCommandText(sql.ToString());

            int propIndex = 0;
            for (int i=0; i < MethodDef.Parameters.Count; i++)
            {
                if (MethodDef.Parameters[i].Type == EntityDef.Type)
                {
                    // Add 1, since the first entry is "this".
                    propIndex = i+1;
                }
            }

            foreach (var column in EntityDef.Properties)
            {
                IlGenerator.BeginScope();

                var parm = IlGenerator.DeclareLocal(typeof(IDbDataParameter));

                AddSqlParameterFromProperty(parm, column.PropertyName, propIndex, column);

                IlGenerator.EndScope();
            }

            ExecuteNonQuery();
        }
    }
}
