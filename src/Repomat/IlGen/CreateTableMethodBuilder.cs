using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Repomat.Schema;
using System.Data;

namespace Repomat.IlGen
{
    internal class CreateTableMethodBuilder : MethodBuilderBase
    {
        private readonly Func<PropertyDef, bool, string> _sqlPropertyMapFunc;

        public CreateTableMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, Func<PropertyDef, bool, string> sqlPropertyMapFunc)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
            _sqlPropertyMapFunc = sqlPropertyMapFunc;
        }

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("create table [{0}] (", MethodDef.EntityDef.TableName);

            List<string> columns = new List<string>();
            foreach (var property in MethodDef.EntityDef.Properties)
            {
                bool isIdentity = MethodDef.EntityDef.HasIdentity && MethodDef.EntityDef.PrimaryKey[0].ColumnName == property.ColumnName;
                columns.Add(string.Format("[{0}] {1}", property.ColumnName, _sqlPropertyMapFunc(property, isIdentity)));
            }
            sql.AppendFormat(string.Join(", ", columns));

            if (MethodDef.EntityDef.PrimaryKey.Count > 0)
            {
                sql.AppendFormat(", CONSTRAINT [pk_{0}] PRIMARY KEY ({1})", MethodDef.EntityDef.TableName, string.Join(", ", MethodDef.EntityDef.PrimaryKey.Select(pk => string.Format("[{0}]", pk.ColumnName))));
            }

            sql.AppendFormat(")");

            SetCommandText(sql.ToString());

            ExecuteNonQuery();
        }
    }
}
