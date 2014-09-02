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
    internal class CreateMethodBuilder : MethodBuilderBase
    {
        private readonly string _statementSeparator;
        private readonly string _scopeIdentityFunction;
        private readonly Type _scopeIdentityType;

        public CreateMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, string statementSeparator, string scopeIdentityFunction, Type scopeIdentityType)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
            _statementSeparator = statementSeparator;
            _scopeIdentityFunction = scopeIdentityFunction;
            _scopeIdentityType = scopeIdentityType;
        }

        protected override void GenerateMethodIl(LocalBuilder cmdLocal)
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
                var parm = IlGenerator.DeclareLocal(typeof(IDbDataParameter));
                AddSqlParameterFromProperty(parm, column.PropertyName, MethodDef.DtoParameterOrNull.Index, column);
            }

            ExecuteScalar();

            var newValueLocal = IlGenerator.DeclareLocal(typeof(int));
            var convertMethod = typeof(Convert).GetMethod("ToInt32", new Type[] { _scopeIdentityType });

            IlGenerator.Emit(OpCodes.Unbox_Any, _scopeIdentityType);
            IlGenerator.Emit(OpCodes.Call, convertMethod);

            IlGenerator.Emit(OpCodes.Stloc, newValueLocal);
            
            var property = EntityDef.Type.GetProperty(EntityDef.PrimaryKey[0].PropertyName);
            if (property != null)
            {
                var setter = property.GetSetMethod();
                if (setter != null)
                {
                    IlGenerator.Emit(OpCodes.Ldarg, MethodDef.DtoParameterOrNull.Index);
                    IlGenerator.Emit(OpCodes.Ldloc, newValueLocal);
                    IlGenerator.Emit(OpCodes.Call, setter);
                }
            }


            if (MethodDef.ReturnsInt)
            {
                IlGenerator.Emit(OpCodes.Ldloc, newValueLocal);
            }

            IlGenerator.Emit(OpCodes.Ret);
        }
    }
}
