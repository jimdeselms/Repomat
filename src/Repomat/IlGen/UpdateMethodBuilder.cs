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
    internal class UpdateMethodBuilder : MethodBuilderBase
    {
        internal UpdateMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
        }

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
        {
            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("update [{0}] set ", EntityDef.TableName);

            var whereColumns = EntityDef.PrimaryKey.ToArray();
            var columnsToSet = EntityDef.Properties.Where(c => whereColumns.All(p => p.ColumnName != c.ColumnName)).ToArray();

            var setEquations = columnsToSet.Select(c => string.Format("[{0}] = @{1}", c.ColumnName, c.PropertyName.Capitalize()));
            sql.AppendFormat(string.Join(", ", setEquations));

            sql.AppendFormat(" WHERE ");

            var whereEquations = whereColumns.Select(c => string.Format("[{0}] = @{1}", c.ColumnName, c.PropertyName.Capitalize()));
            sql.AppendFormat(string.Join(" AND ", whereEquations));

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
