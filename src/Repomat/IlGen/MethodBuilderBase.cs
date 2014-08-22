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

        private readonly LocalBuilder _testLocal;

        private readonly FieldInfo _connectionField;
        private static readonly MethodInfo _createCommandMethod;
        private static readonly MethodInfo _commandTextSetMethod;
        private static readonly MethodInfo _commandTextGetMethod;
        private static readonly MethodInfo _createParameterMethod;
        private static readonly MethodInfo _parameterNameSetMethod;
        private static readonly MethodInfo _valueSetMethod;
        private static readonly MethodInfo _parametersGetMethod;
        private static readonly MethodInfo _parametersAddMethod;
        private static readonly MethodInfo _executeNonQueryMethod;
        private static readonly MethodInfo _executeScalarMethod;

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
            _executeNonQueryMethod = typeof(IDbCommand).GetMethod("ExecuteNonQuery");
            _executeScalarMethod = typeof(IDbCommand).GetMethod("ExecuteScalar");
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

        protected void AddSqlParameter(LocalBuilder sqlParameter, string name, int argumentIndex)
        {
            // parm = cmd.CreateParameter();
            IlGenerator.Emit(OpCodes.Ldloc, _commandLocal);
            IlGenerator.Emit(OpCodes.Callvirt, _createParameterMethod);
            IlGenerator.Emit(OpCodes.Stloc, sqlParameter);

            // parm.ParameterName = name
            IlGenerator.Emit(OpCodes.Ldloc, sqlParameter);
            IlGenerator.Emit(OpCodes.Ldstr, name);
            IlGenerator.Emit(OpCodes.Callvirt, _parameterNameSetMethod);

            // parm.Value = argX;
            IlGenerator.Emit(OpCodes.Ldloc, sqlParameter);
            IlGenerator.Emit(OpCodes.Ldarg, argumentIndex);
            IlGenerator.Emit(OpCodes.Callvirt, _valueSetMethod);

            // cmd.Paramters.Add(parm);
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

        public void GenerateIl()
        {
            // var cmd = _connection.CreateCommand();
            // try
            // finally
            // cmd.Disose();

            IlGenerator.Emit(OpCodes.Ldarg_0);
            IlGenerator.Emit(OpCodes.Ldfld, _connectionField);
            IlGenerator.EmitCall(OpCodes.Callvirt, _createCommandMethod, Type.EmptyTypes);
            IlGenerator.Emit(OpCodes.Stloc, _commandLocal.LocalIndex);

            IlGenerator.BeginExceptionBlock();

            GenerateMethodIl(_commandLocal);

            IlGenerator.BeginFinallyBlock();
            IlGenerator.EndExceptionBlock();

            IlGenerator.Emit(OpCodes.Ret);
        }

        protected FieldBuilder DefineField<T>(string name)
        {
            return TypeBuilder.DefineField(name, typeof(T), FieldAttributes.Private);
        }

        protected abstract void GenerateMethodIl(LocalBuilder cmdVariable);

        protected TypeBuilder TypeBuilder { get { return _typeBuilder; } }
        protected RepositoryDef RepositoryDef { get { return _repoDef; } }
        protected MethodDef MethodDef { get { return _methodDef; } }
        protected bool NewConnectionEveryTime { get { return _newConnectionEveryTime; } }
        protected ILGenerator IlGenerator { get { return _ilGenerator; } }

        private Emit.MethodBuilder CreateMethod()
        {
            return TypeBuilder.DefineMethod(
                MethodDef.MethodName,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                CallingConventions.Standard | CallingConventions.HasThis,
                MethodDef.ReturnType,
                MethodDef.Parameters.Select(p => p.Type).ToArray());
        }
    }
}
