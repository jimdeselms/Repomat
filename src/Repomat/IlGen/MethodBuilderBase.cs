using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Emit = System.Reflection.Emit;
using Repomat.Schema;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Repomat.CodeGen;
using System.Threading;

namespace Repomat.IlGen
{
    internal abstract class MethodBuilderBase
    {
        private readonly RepositoryDef _repoDef;
        private readonly MethodDef _methodDef;
        private readonly bool _newConnectionEveryTime;
        private readonly TypeBuilder _typeBuilder;
        private readonly IlBuilder _ilBuilder;
        private readonly LocalBuilder _commandLocal;
        private readonly LocalBuilder _returnValueLocal;

        private readonly LocalBuilder _testLocal;

        private readonly FieldInfo _connectionField;
        private static readonly MethodInfo _createCommandMethod;
        private static readonly MethodInfo _commandTextSetMethod;
        private static readonly MethodInfo _commandTextGetMethod;
        private static readonly MethodInfo _createParameterMethod;
        private static readonly MethodInfo _parameterNameSetMethod;
        private static readonly MethodInfo _dbTypeSetMethod;
        private static readonly MethodInfo _valueSetMethod;
        private static readonly MethodInfo _parametersGetMethod;
        private static readonly MethodInfo _parametersAddMethod;
        private static readonly MethodInfo _executeNonQueryMethod;
        private static readonly MethodInfo _executeScalarMethod;
        private static readonly MethodInfo _disposeMethod;
        private static readonly MethodInfo _commandTypeSet;

        protected MethodInfo CommandTextSetMethod { get { return _commandTextSetMethod; } }

        static MethodBuilderBase()
        {
            _createCommandMethod = typeof(IDbConnection).GetMethod("CreateCommand");
            _commandTextSetMethod = typeof (IDbCommand).GetProperty("CommandText").GetSetMethod();
            _commandTextGetMethod = typeof (IDbCommand).GetProperty("CommandText").GetGetMethod();
            _createParameterMethod = typeof(IDbCommand).GetMethod("CreateParameter");
            _parameterNameSetMethod = typeof(IDataParameter).GetProperty("ParameterName").GetSetMethod();
            _valueSetMethod = typeof(IDataParameter).GetProperty("Value").GetSetMethod();
            _parametersGetMethod = typeof(IDbCommand).GetProperty("Parameters").GetGetMethod();
            _parametersAddMethod = typeof(IList).GetMethod("Add");
            _dbTypeSetMethod     = typeof(IDataParameter).GetProperty("DbType").GetSetMethod();
            _executeNonQueryMethod = typeof(IDbCommand).GetMethod("ExecuteNonQuery", Type.EmptyTypes);
            _executeScalarMethod = typeof(IDbCommand).GetMethod("ExecuteScalar", Type.EmptyTypes);
            _disposeMethod = typeof(IDisposable).GetMethod("Dispose", Type.EmptyTypes);
            _commandTypeSet = typeof(IDbCommand).GetProperty("CommandType").GetSetMethod();
        }

        protected MethodBuilderBase(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime)
        {
            _typeBuilder = typeBuilder;
            _connectionField = connectionField;
            _repoDef = repoDef;
            _methodDef = methodDef;
            _newConnectionEveryTime = newConnectionEveryTime;

            var methodBuilder = CreateMethod();
            _ilBuilder = new IlBuilder(methodBuilder, _methodDef.MethodInfo.GetParameters());

            _commandLocal = IlBuilder.DeclareLocal(typeof(IDbCommand));
            _testLocal = IlBuilder.DeclareLocal(typeof (string));
            _returnValueLocal = MethodDef.ReturnType == typeof(void) ? null : IlBuilder.DeclareLocal(MethodDef.ReturnType);
        }

        protected void SetCommandText(string commandText, bool isStoredProcedure=false)
        {
            // cmd.CommandText = commandText
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldstr, commandText);
            IlBuilder.Call(_commandTextSetMethod);

            if (MethodDef.CustomSqlIsStoredProcedure)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, CommandLocal);
                IlBuilder.Ldc((int)CommandType.StoredProcedure);
                IlBuilder.Call(_commandTypeSet);
            }
        }

        protected void WriteCommandText()
        {
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlBuilder.Call(_commandTextGetMethod);

            IlBuilder.ILGenerator.Emit(OpCodes.Stloc, 1);
            IlBuilder.ILGenerator.EmitWriteLine(_testLocal);
        }

        protected void AddSqlParameterFromArgument(LocalBuilder sqlParameter, string name, int argumentIndex, Type parmCSharpType)
        {
            AddSqlParameter(sqlParameter, name, parmCSharpType, () =>
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, sqlParameter);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, argumentIndex);
            });
        }

        public static readonly FieldInfo DBNULL_VALUE = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);

        protected void AddSqlParameterFromProperty(LocalBuilder sqlParameter, string name, int entityArgumentIndex, PropertyDef property)
        {
            AddSqlParameter(sqlParameter, name, property.Type, () =>
            {
                var propGet = EntityDef.Type.GetProperty(name).GetGetMethod();
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, sqlParameter);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, entityArgumentIndex);
                IlBuilder.Call(propGet);
            });
        }

        private void AddSqlParameter(LocalBuilder sqlParameter, string name, Type type, Action getParameterValue)
        {
            var typeInfo = PrimitiveTypeInfo.Get(type);

            // parm = cmd.CreateParameter();
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlBuilder.Call(_createParameterMethod);
            IlBuilder.ILGenerator.Emit(OpCodes.Stloc, sqlParameter);

            // parm.ParameterName = name
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, sqlParameter);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldstr, name);
            IlBuilder.Call(_parameterNameSetMethod);

            // parm.DbType = blah
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, sqlParameter);
            IlBuilder.Ldc((int)typeInfo.DbType);

            IlBuilder.Call(_dbTypeSetMethod);

            getParameterValue();

            if (type.IsValueType)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Box, type);
            }

            if (typeInfo.CanBeNull)
            {
                var nullCheckStore = IlBuilder.DeclareLocal(typeof(object));
                var skipDbNullReplacement = IlBuilder.DefineLabel();

                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, nullCheckStore);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, nullCheckStore);
                IlBuilder.ILGenerator.Emit(OpCodes.Brtrue, skipDbNullReplacement);

                IlBuilder.ILGenerator.Emit(OpCodes.Ldsfld, DBNULL_VALUE);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, nullCheckStore);

                IlBuilder.MarkLabel(skipDbNullReplacement);

                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, nullCheckStore);
            }

            IlBuilder.Call(_valueSetMethod);

            //// cmd.Paramters.Add(parm);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlBuilder.Call(_parametersGetMethod);
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, sqlParameter);
            IlBuilder.Call(_parametersAddMethod);
            IlBuilder.ILGenerator.Emit(OpCodes.Pop);
        }

        protected void ExecuteNonQuery()
        {
            // cmd.ExecuteNonQuery();
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlBuilder.Call(_executeNonQueryMethod);
            IlBuilder.ILGenerator.Emit(OpCodes.Pop); // Pop the unused result
        }

        protected void ExecuteScalar()
        {
            // cmd.ExecuteScalar();
            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlBuilder.Call(_executeScalarMethod);
        }

        private static readonly ConstructorInfo _repomatExceptionCtor = typeof(RepomatException).GetConstructor(new Type[] { typeof(string), typeof(object[]) });

        protected void ThrowRepomatException(string format, params object[] args)
        {
            IlBuilder.ILGenerator.Emit(OpCodes.Ldstr, string.Format(format, args));
            IlBuilder.Ldc(0);
            IlBuilder.ILGenerator.Emit(OpCodes.Newarr, typeof(object));
            IlBuilder.ILGenerator.Emit(OpCodes.Newobj, _repomatExceptionCtor);
            IlBuilder.ILGenerator.Emit(OpCodes.Throw);
        }

        private static MethodInfo _monitorEnterMethod = typeof(Monitor).GetMethod("Enter", new Type[] { typeof(object), typeof(bool).MakeByRefType() });
        private static MethodInfo _monitorExitMethod = typeof(Monitor).GetMethod("Exit", new Type[] { typeof(object) });
        private static MethodInfo _dbConnFuncInvokeMethod = typeof(Func<>).MakeGenericType(typeof(IDbConnection)).GetMethod("Invoke", Type.EmptyTypes);
        private static MethodInfo _dbConnOpenMethod = typeof(IDbConnection).GetMethod("Open", Type.EmptyTypes);

        public void GenerateIl()
        {
            int? passedConnectionIndex = GetArgumentIndex(typeof(IDbConnection));
            int? passedTransactionIndex = GetArgumentIndex(typeof(IDbTransaction));

            bool lockConnection = passedConnectionIndex.HasValue || passedTransactionIndex.HasValue || !_newConnectionEveryTime;

            var connectionLocal = IlBuilder.DeclareLocal(typeof(IDbConnection));
            LocalBuilder lockTakenLocal = null;

            if (lockConnection)
            {
                lockTakenLocal = IlBuilder.DeclareLocal(typeof(bool));

                IlBuilder.Ldc(0);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, lockTakenLocal);
            }

            IlBuilder.BeginExceptionBlock();

            if (passedConnectionIndex.HasValue)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, passedConnectionIndex.Value);
            }
            else if (passedTransactionIndex.HasValue)
            {
                var connProperty = typeof(IDbTransaction).GetProperty("Connection").GetGetMethod();
                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, passedTransactionIndex.Value);
                IlBuilder.Call(connProperty);
            }
            else if (_newConnectionEveryTime)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg_0);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldfld, _connectionField);

                IlBuilder.Call(_dbConnFuncInvokeMethod);
                IlBuilder.ILGenerator.Emit(OpCodes.Stloc, connectionLocal);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, connectionLocal);
                IlBuilder.Call(_dbConnOpenMethod);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, connectionLocal);
            }
            else // use the _connectionField
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg_0);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldfld, _connectionField);
            }

            IlBuilder.ILGenerator.Emit(OpCodes.Dup);
            IlBuilder.ILGenerator.Emit(OpCodes.Stloc, connectionLocal);

            if (lockConnection)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloca, lockTakenLocal);
                IlBuilder.Call(_monitorEnterMethod);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, connectionLocal);
            }

            IlBuilder.ILGenerator.EmitCall(OpCodes.Callvirt, _createCommandMethod, Type.EmptyTypes);
            IlBuilder.ILGenerator.Emit(OpCodes.Stloc, _commandLocal);

            if (passedTransactionIndex.HasValue)
            {
                var setTransactionProp = typeof(IDbCommand).GetProperty("Transaction").GetSetMethod();
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _commandLocal);
                IlBuilder.ILGenerator.Emit(OpCodes.Ldarg, passedTransactionIndex.Value);
                IlBuilder.Call(setTransactionProp);
            }

            IlBuilder.BeginExceptionBlock();

            GenerateMethodIl(_commandLocal);

            IlBuilder.BeginFinallyBlock();

            IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlBuilder.Call(_disposeMethod);

            IlBuilder.EndExceptionBlock();

            IlBuilder.BeginFinallyBlock();

            if (lockConnection)
            {
                var lockNotTakenLabel = IlBuilder.DefineLabel();
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, lockTakenLocal);
                IlBuilder.ILGenerator.Emit(OpCodes.Brfalse, lockNotTakenLabel);

                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, connectionLocal);
                IlBuilder.Call(_monitorExitMethod);
                IlBuilder.MarkLabel(lockNotTakenLabel);
            }
            else
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, connectionLocal);
                IlBuilder.Call(_disposeMethod);
            }

            IlBuilder.EndExceptionBlock();

            if (_returnValueLocal != null)
            {
                IlBuilder.ILGenerator.Emit(OpCodes.Ldloc, _returnValueLocal);
            }
            IlBuilder.Ret();
        }

        private int? GetArgumentIndex(Type t)
        {
            return MethodDef.Parameters.Select((p, i) => new { p, i }).Where(p => p.p.Type == t).Select(p => (int?)p.i).FirstOrDefault() + 1;
        }

        protected void WriteParameterAssignmentsFromArgList()
        {
            for (int argIndex = 0; argIndex < MethodDef.Parameters.Count; argIndex++)
            {
                ParameterDetails arg = MethodDef.Parameters[argIndex];

                PropertyDef column = null;

                if (EntityDef != null)
                {
                    column = EntityDef.Properties.FirstOrDefault(c => c.PropertyName == arg.Name.Capitalize());
                }

                if (column == null)
                {
                    if (MethodDef.CustomSqlOrNull != null)
                    {
                        column = new PropertyDef(arg.Name, arg.Name, typeof(void));
                    }
                    else
                    {
                        continue;
                    }
                }

                IlBuilder.BeginScope();

                var parmLocal = IlBuilder.DeclareLocal(typeof(IDbDataParameter));

                // Add one to the argument index; the first one is "this"
                AddSqlParameterFromArgument(parmLocal, arg.Name, argIndex + 1, arg.Type);

                IlBuilder.EndScope();
            }
        }

        protected FieldBuilder DefineField<T>(string name)
        {
            return TypeBuilder.DefineField(name, typeof(T), FieldAttributes.Private);
        }

        protected FieldBuilder DefineStaticField<T>(string name)
        {
            return TypeBuilder.DefineField(name, typeof(T), FieldAttributes.Private | FieldAttributes.Static);
        }

        protected EntityDef EntityDef
        {
            get { return _methodDef.EntityDef; }
        }

        protected abstract void GenerateMethodIl(LocalBuilder cmdVariable);

        protected TypeBuilder TypeBuilder { get { return _typeBuilder; } }
        protected RepositoryDef RepoDef { get { return _repoDef; } }
        protected MethodDef MethodDef { get { return _methodDef; } }
        protected bool NewConnectionEveryTime { get { return _newConnectionEveryTime; } }
        protected IlBuilder IlBuilder { get { return _ilBuilder; } }

        protected LocalBuilder CommandLocal { get { return _commandLocal; } }
        protected LocalBuilder ReturnValueLocal { get { return _returnValueLocal; } }

        private Emit.MethodBuilder CreateMethod()
        {
            MethodAttributes attrs = 
                MethodAttributes.Public | 
                MethodAttributes.NewSlot |
                MethodAttributes.HideBySig | 
                MethodAttributes.Final |
//                MethodAttributes.SpecialName | 
                MethodAttributes.Virtual;

            CallingConventions conventions =
                CallingConventions.Standard |
                CallingConventions.HasThis;

            return TypeBuilder.DefineMethod(
                MethodDef.MethodName,
                attrs,
                conventions,
                MethodDef.ReturnType,
                MethodDef.Parameters.Select(p => p.Type).ToArray());
        }
    }
}
