﻿using System;
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
        private readonly ILGenerator _ilGenerator;
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
        }

        protected MethodBuilderBase(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime)
        {
            _typeBuilder = typeBuilder;
            _connectionField = connectionField;
            _repoDef = repoDef;
            _methodDef = methodDef;
            _newConnectionEveryTime = newConnectionEveryTime;

            var methodBuilder = CreateMethod();
            _ilGenerator = methodBuilder.GetILGenerator();

            _commandLocal = IlGenerator.DeclareLocal(typeof(IDbCommand));
            _testLocal = IlGenerator.DeclareLocal(typeof (string));
            _returnValueLocal = MethodDef.ReturnType == typeof(void) ? null : IlGenerator.DeclareLocal(MethodDef.ReturnType);
        }

        protected void SetCommandText(string commandText)
        {
            // cmd.CommandText = commandText
            IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlGenerator.Emit(OpCodes.Ldstr, commandText);
            IlGenerator.Emit(OpCodes.Callvirt, _commandTextSetMethod);
        }

        protected void WriteCommandText()
        {
            IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlGenerator.Emit(OpCodes.Callvirt, _commandTextGetMethod);

            IlGenerator.Emit(OpCodes.Stloc, 1);
            IlGenerator.EmitWriteLine(_testLocal);
        }

        protected void AddSqlParameterFromArgument(LocalBuilder sqlParameter, string name, int argumentIndex, Type parmCSharpType)
        {
            AddSqlParameter(sqlParameter, name, parmCSharpType, () =>
            {
                IlGenerator.Emit(OpCodes.Ldloc, sqlParameter);
                IlGenerator.Emit(OpCodes.Ldarg, argumentIndex);
            });
        }

        public static readonly FieldInfo DBNULL_VALUE = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);

        protected void AddSqlParameterFromProperty(LocalBuilder sqlParameter, string name, int entityArgumentIndex, PropertyDef property)
        {
            AddSqlParameter(sqlParameter, name, property.Type, () =>
            {
                var propGet = EntityDef.Type.GetProperty(name).GetGetMethod();
                IlGenerator.Emit(OpCodes.Ldloc, sqlParameter);
                IlGenerator.Emit(OpCodes.Ldarg, entityArgumentIndex);
                IlGenerator.Emit(OpCodes.Call, propGet);
            });
        }

        private void AddSqlParameter(LocalBuilder sqlParameter, string name, Type type, Action getParameterValue)
        {
            var typeInfo = PrimitiveTypeInfo.Get(type);

            // parm = cmd.CreateParameter();
            IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlGenerator.Emit(OpCodes.Callvirt, _createParameterMethod);
            IlGenerator.Emit(OpCodes.Stloc, sqlParameter);

            // parm.ParameterName = name
            IlGenerator.Emit(OpCodes.Ldloc, sqlParameter);
            IlGenerator.Emit(OpCodes.Ldstr, name);
            IlGenerator.Emit(OpCodes.Callvirt, _parameterNameSetMethod);

            // parm.DbType = blah
            IlGenerator.Emit(OpCodes.Ldloc, sqlParameter);
            IlGenerator.Emit(OpCodes.Ldc_I4, (int)typeInfo.DbType);

            IlGenerator.Emit(OpCodes.Callvirt, _dbTypeSetMethod);

            getParameterValue();

            if (type.IsValueType)
            {
                IlGenerator.Emit(OpCodes.Box, type);
            }

            if (typeInfo.CanBeNull)
            {
                var nullCheckStore = IlGenerator.DeclareLocal(typeof(object));
                var skipDbNullReplacement = IlGenerator.DefineLabel();

                IlGenerator.Emit(OpCodes.Stloc, nullCheckStore);
                IlGenerator.Emit(OpCodes.Ldloc, nullCheckStore);
                IlGenerator.Emit(OpCodes.Brtrue, skipDbNullReplacement);

                IlGenerator.Emit(OpCodes.Ldsfld, DBNULL_VALUE);
                IlGenerator.Emit(OpCodes.Stloc, nullCheckStore);

                IlGenerator.MarkLabel(skipDbNullReplacement);

                IlGenerator.Emit(OpCodes.Ldloc, nullCheckStore);
            }

            IlGenerator.Emit(OpCodes.Callvirt, _valueSetMethod);

            //// cmd.Paramters.Add(parm);
            IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlGenerator.Emit(OpCodes.Callvirt, _parametersGetMethod);
            IlGenerator.Emit(OpCodes.Ldloc, sqlParameter);
            IlGenerator.Emit(OpCodes.Callvirt, _parametersAddMethod);
            IlGenerator.Emit(OpCodes.Pop);
        }

        protected void ExecuteNonQuery()
        {
            // cmd.ExecuteNonQuery();
            IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlGenerator.Emit(OpCodes.Callvirt, _executeNonQueryMethod);
            IlGenerator.Emit(OpCodes.Pop); // Pop the unused result
        }

        protected void ExecuteScalar()
        {
            // cmd.ExecuteScalar();
            IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlGenerator.Emit(OpCodes.Callvirt, _executeScalarMethod);
        }

        private static readonly ConstructorInfo _repomatExceptionCtor = typeof(RepomatException).GetConstructor(new Type[] { typeof(string), typeof(object[]) });

        protected void ThrowRepomatException(string format, params object[] args)
        {
            IlGenerator.Emit(OpCodes.Ldstr, string.Format(format, args));
            IlGenerator.Emit(OpCodes.Ldc_I4_0);
            IlGenerator.Emit(OpCodes.Newarr, typeof(object));
            IlGenerator.Emit(OpCodes.Newobj, _repomatExceptionCtor);
            IlGenerator.Emit(OpCodes.Throw);
        }

        private static MethodInfo _monitorEnterMethod = typeof(Monitor).GetMethod("Enter", new Type[] { typeof(object), typeof(bool).MakeByRefType() });
        private static MethodInfo _monitorExitMethod = typeof(Monitor).GetMethod("Exit", new Type[] { typeof(object) });

        public void GenerateIl()
        {
            // var cmd = _connection.CreateCommand();
            // try
            // finally
            // cmd.Disose();
            var connectionLocal = IlGenerator.DeclareLocal(typeof(IDbConnection));
            var lockTakenLocal = IlGenerator.DeclareLocal(typeof(bool));

            IlGenerator.Emit(OpCodes.Ldc_I4_0);
            IlGenerator.Emit(OpCodes.Stloc, lockTakenLocal);

            IlGenerator.BeginExceptionBlock();

            int? passedConnectionIndex = GetArgumentIndex(typeof(IDbConnection));
            int? passedTransactionIndex = GetArgumentIndex(typeof(IDbTransaction));
            if (passedConnectionIndex.HasValue)
            {
                IlGenerator.Emit(OpCodes.Ldarg, passedConnectionIndex.Value);
            }
            else if (passedTransactionIndex.HasValue)
            {
                var connProperty = typeof(IDbTransaction).GetProperty("Connection").GetGetMethod();
                IlGenerator.Emit(OpCodes.Ldarg, passedTransactionIndex.Value);
                IlGenerator.Emit(OpCodes.Call, connProperty);
            }
            else // use the _connectionField
            {
                IlGenerator.Emit(OpCodes.Ldarg_0);
                IlGenerator.Emit(OpCodes.Ldfld, _connectionField);
            }
            IlGenerator.Emit(OpCodes.Dup);
            IlGenerator.Emit(OpCodes.Stloc, connectionLocal);

            IlGenerator.Emit(OpCodes.Ldloca, lockTakenLocal);
            IlGenerator.Emit(OpCodes.Call, _monitorEnterMethod);

            IlGenerator.Emit(OpCodes.Ldloc, connectionLocal);
            IlGenerator.EmitCall(OpCodes.Callvirt, _createCommandMethod, Type.EmptyTypes);
            IlGenerator.Emit(OpCodes.Stloc, _commandLocal);

            if (passedTransactionIndex.HasValue)
            {
                var setTransactionProp = typeof(IDbCommand).GetProperty("Transaction").GetSetMethod();
                IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
                IlGenerator.Emit(OpCodes.Ldarg, passedTransactionIndex.Value);
                IlGenerator.Emit(OpCodes.Call, setTransactionProp);
            }

            IlGenerator.BeginExceptionBlock();

            GenerateMethodIl(_commandLocal);

            IlGenerator.BeginFinallyBlock();

            IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlGenerator.Emit(OpCodes.Callvirt, _disposeMethod);

            IlGenerator.EndExceptionBlock();

            IlGenerator.BeginFinallyBlock();
                
            var lockNotTakenLabel = IlGenerator.DefineLabel();
            IlGenerator.Emit(OpCodes.Ldloc, lockTakenLocal);
            IlGenerator.Emit(OpCodes.Brfalse, lockNotTakenLabel);

            IlGenerator.Emit(OpCodes.Ldloc, connectionLocal);
            IlGenerator.Emit(OpCodes.Call, _monitorExitMethod);

            IlGenerator.MarkLabel(lockNotTakenLabel);

            IlGenerator.EndExceptionBlock();

            if (_returnValueLocal != null)
            {
                IlGenerator.Emit(OpCodes.Ldloc, _returnValueLocal);
            }
            IlGenerator.Emit(OpCodes.Ret);
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

                var column = EntityDef.Properties.FirstOrDefault(c => c.PropertyName == arg.Name.Capitalize());
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

                IlGenerator.BeginScope();

                var parmLocal = IlGenerator.DeclareLocal(typeof(IDbDataParameter));

                // Add one to the argument index; the first one is "this"
                AddSqlParameterFromArgument(parmLocal, arg.Name, argIndex + 1, arg.Type);

                IlGenerator.EndScope();
            }
        }

        protected FieldBuilder DefineField<T>(string name)
        {
            return TypeBuilder.DefineField(name, typeof(T), FieldAttributes.Private);
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
        protected ILGenerator IlGenerator { get { return _ilGenerator; } }

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
