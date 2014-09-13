using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Repomat.Schema;

namespace Repomat.IlGen
{
    internal abstract class InsertCreateUpdateMethodBuilderBase : MethodBuilderBase
    {
        private readonly string _statementSeparator;
        private readonly Type _scopeIdentityType;
        private readonly string _scopeIdentityFunction;

        protected InsertCreateUpdateMethodBuilderBase(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, string statementSeparator, Type scopeIdentityType, string scopeIdentityFunction) 
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
            _statementSeparator = statementSeparator;
            _scopeIdentityType = scopeIdentityType;
            _scopeIdentityFunction = scopeIdentityFunction;
        }

        protected void GenerateIlForInsert(LocalBuilder cmdLocal, string orReplace="")
        {
            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("insert {0}into [{1}] (", orReplace, EntityDef.TableName);
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
                IlBuilder.BeginScope();

                var parm = IlBuilder.DeclareLocal(typeof(IDbDataParameter));

                AddSqlParameterFromProperty(parm, column.PropertyName, propIndex, column);

                IlBuilder.EndScope();
            }

            ExecuteNonQuery();
        }

        protected void GenerateIlForCreate(LocalBuilder cmdLocal)
        {
            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("insert into [{0}] (", EntityDef.TableName);
            sql.AppendFormat(string.Join(", ", EntityDef.NonPrimaryKeyColumns.Select(c => c.ColumnName)));
            sql.AppendFormat(") values (");
            sql.AppendFormat(string.Join(", ", EntityDef.NonPrimaryKeyColumns.Select(c => "@" + c.PropertyName)));
            sql.AppendFormat("){0} SELECT {1}", _statementSeparator, _scopeIdentityFunction);

            SetCommandText(sql.ToString());

            foreach (var column in EntityDef.NonPrimaryKeyColumns)
            {
                var parm = IlBuilder.DeclareLocal(typeof(IDbDataParameter));
                AddSqlParameterFromProperty(parm, column.PropertyName, MethodDef.DtoParameterOrNull.Index, column);
            }

            ExecuteScalar();

            var newValueLocal = IlBuilder.DeclareLocal(typeof(int));
            var convertMethod = typeof(Convert).GetMethod("ToInt32", new Type[] { _scopeIdentityType });

            IlBuilder.ILGenerator.Emit(OpCodes.Unbox_Any, _scopeIdentityType);
            IlBuilder.ILGenerator.Emit(OpCodes.Call, convertMethod);

            IlBuilder.ILGenerator.Emit(OpCodes.Stloc, newValueLocal);

            var property = EntityDef.Type.GetProperty(EntityDef.PrimaryKey[0].PropertyName);
            if (property != null)
            {
                var setter = property.GetSetMethod();
                if (setter != null)
                {
                    IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, MethodDef.DtoParameterOrNull.Index);
                    IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, newValueLocal);
                    IlBuilder.ILGenerator.Emit(OpCodes.Call, setter);
                }
            }


            if (MethodDef.ReturnsInt)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, newValueLocal);
            }

            IlBuilder.Ret();
        }

        protected void GenerateIlForUpdate(LocalBuilder cmdLocal)
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
                IlBuilder.BeginScope();

                var parm = IlBuilder.DeclareLocal(typeof(IDbDataParameter));

                AddSqlParameterFromProperty(parm, column.PropertyName, propIndex, column);

                IlBuilder.EndScope();
            }

            ExecuteNonQuery();
        }
    }
}
