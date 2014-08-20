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

        protected MethodInfo CommandTextSetMethod { get { return _commandTextSetMethod; } }

        static MethodBuilderBase()
        {
            _createCommandMethod = typeof(IDbConnection).GetMethod("CreateCommand");
            _commandTextSetMethod = typeof (IDbCommand).GetProperty("CommandText").GetSetMethod();
            _commandTextGetMethod = typeof (IDbCommand).GetProperty("CommandText").GetGetMethod();
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
