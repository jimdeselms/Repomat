using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using EmitMethodBuilder = System.Reflection.Emit.MethodBuilder;

namespace Repomat
{
    internal class IlBuilder
    {
        private readonly ILGenerator _ilGen;
        private readonly Type _returnType;
        private readonly ParameterDetails[] _parameterTypes;
        private readonly Type _declaringType;
        private readonly bool _isStatic;

        public IlBuilder(EmitMethodBuilder methodBuilder, ParameterDetails[] parameterTypes)
            : this(methodBuilder.DeclaringType, methodBuilder.IsStatic, methodBuilder.GetILGenerator(), methodBuilder.ReturnType, parameterTypes)
        {
        }

        public IlBuilder(EmitMethodBuilder methodBuilder, ParameterInfo[] parameterTypes)
            : this(methodBuilder, parameterTypes.Select(p => new ParameterDetails(p, 0)).ToArray())
        {
        }

        // This is just for testing
        internal IlBuilder(EmitMethodBuilder methodBuilder, Type[] parameterTypes)
            : this(methodBuilder, parameterTypes.Select(t => new ParameterDetails(t, "x", false, 0)).ToArray())
        {
        }

        public IlBuilder(ConstructorBuilder ctor, Type[] types)
            : this(ctor.DeclaringType, false, ctor.GetILGenerator(), typeof(void), types.Select(t => new ParameterDetails(t, "x", false, 0)).ToArray())
        {
        }
       
        private IlBuilder(Type declaringType, bool isStatic, ILGenerator ilGen, Type returnType, ParameterDetails[] parms)
        {
            _declaringType = declaringType;
            _isStatic = isStatic;
            _ilGen = ilGen;
            _returnType = returnType;
            _parameterTypes = parms;
        }

        internal ILGenerator ILGenerator { get { return _ilGen; } }

        public void BeginExceptionBlock()
        {
            _ilGen.BeginExceptionBlock();
        }

        public void BeginFinallyBlock()
        {
            _ilGen.BeginFinallyBlock();
        }

        public void BeginScope()
        {
            _ilGen.BeginScope();
        }

        public void Box(Type t)
        {
            _ilGen.Emit(OpCodes.Box, t);
        }

        public void Call(MethodInfo method, params Type[] types)
        {
            OpCode opcode = method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call;

            if (types.Length == 0)
            {
                _ilGen.Emit(opcode, method);
            }
            else
            {
                _ilGen.EmitCall(opcode, method, types);
            }
        }

        public LocalBuilder DeclareLocal(Type t)
        {
            return _ilGen.DeclareLocal(t);
        }

        public Label DefineLabel()
        {
            return _ilGen.DefineLabel();
        }

        public void Dup()
        {
            _ilGen.Emit(OpCodes.Dup);
        }

        public void EndExceptionBlock()
        {
            _ilGen.EndExceptionBlock();
        }

        public void EndScope()
        {
            _ilGen.EndScope();
        }

        private void If(OpCode opcode, Action ifFalse)
        {
            var skipFalse = _ilGen.DefineLabel();

            _ilGen.Emit(opcode, skipFalse);

            ifFalse();

            _ilGen.MarkLabel(skipFalse);
        }

        private void If(OpCode opcode, Action ifTrue, Action ifFalse)
        {
            var skipTrue = _ilGen.DefineLabel();
            var skipFalse = _ilGen.DefineLabel();

            _ilGen.Emit(opcode, skipTrue);

            ifTrue();
            _ilGen.Emit(OpCodes.Br, skipFalse);

            _ilGen.MarkLabel(skipTrue);

            ifFalse();

            _ilGen.MarkLabel(skipFalse);
        }


        public void IfTrue(Action ifTrue)
        {
            If(OpCodes.Brfalse, ifTrue);
        }

        public void IfFalse(Action ifFalse)
        {
            If(OpCodes.Brtrue, ifFalse);
        }

        public void If(Action ifTrue, Action ifFalse)
        {
            If(OpCodes.Brfalse, ifTrue, ifFalse);
        }

        public void Ifeq(Action ifEqual, Action ifNotEqual)
        {
            If(OpCodes.Beq, ifNotEqual, ifEqual);
        }

        public void Ifne(Action ifNotEqual)
        {
            If(OpCodes.Beq, ifNotEqual);
        }

        public void Initobj(Type t)
        {
            _ilGen.Emit(OpCodes.Initobj, t);
        }

        public void Ldc(int i)
        {
            _ilGen.Emit(OpCodes.Ldc_I4, i);
        }

        public void Ldc(long l)
        {
            _ilGen.Emit(OpCodes.Ldc_I8, l);
        }

        public void Ldarg(int i)
        {
            _ilGen.Emit(OpCodes.Ldarg, i);
        }

        public void Ldfld(FieldInfo field)
        {
            if (field.IsStatic)
            {
                _ilGen.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                _ilGen.Emit(OpCodes.Ldfld, field);
            }
        }

        public void Ldloc(LocalBuilder local)
        {
            _ilGen.Emit(OpCodes.Ldloc, local);
        }

        public void Ldloca(LocalBuilder local)
        {
            _ilGen.Emit(OpCodes.Ldloca, local);
        }

        public void Ldnull()
        {
            _ilGen.Emit(OpCodes.Ldnull);
        }

        public void Ldstr(string s)
        {
            _ilGen.Emit(OpCodes.Ldstr, s);
        }

        public void MarkLabel(Label label)
        {
            _ilGen.MarkLabel(label);
        }

        public void Newarr(Type elementType)
        {
            _ilGen.Emit(OpCodes.Newarr, elementType);
        }

        public void Newobj(ConstructorInfo ctor)
        {
            _ilGen.Emit(OpCodes.Newobj, ctor);
        }

        public void Ret()
        {
            _ilGen.Emit(OpCodes.Ret);
        }

        public void Pop()
        {
            _ilGen.Emit(OpCodes.Pop);
        }

        public void Stind_Ref()
        {
            _ilGen.Emit(OpCodes.Stind_Ref);
        }

        public void Stfld(FieldInfo field)
        {
            if (field.IsStatic)
            {
                _ilGen.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                _ilGen.Emit(OpCodes.Stfld, field);
            }
        }

        public void Stloc(LocalBuilder local)
        {
            _ilGen.Emit(OpCodes.Stloc, local);
        }

        public void Throw()
        {
            _ilGen.Emit(OpCodes.Throw);
        }

        public void Unbox(Type type)
        {
            _ilGen.Emit(OpCodes.Unbox_Any, type);
        }

        public void While(Action loadValue, Action whileTrue)
        {
            var loopStart = _ilGen.DefineLabel();
            var loopEnd = _ilGen.DefineLabel();

            _ilGen.MarkLabel(loopStart);
            loadValue();

            _ilGen.Emit(OpCodes.Brfalse, loopEnd);
                
            whileTrue();

            _ilGen.Emit(OpCodes.Br, loopStart);

            _ilGen.MarkLabel(loopEnd);
        }

        //private void HandleBoxing(Type targetType)
        //{
        //    Type topType = _evalStack.Peek();

        //    if (topType.IsValueType && !targetType.IsValueType)
        //    {
        //        _ilGen.Emit(OpCodes.Box, topType);
        //        _evalStack.Pop();
        //        _evalStack.Push(targetType);
        //    }

        //    if (!topType.IsValueType && targetType.IsValueType)
        //    {
        //        _ilGen.Emit(OpCodes.Unbox_Any, targetType);
        //        _evalStack.Pop();
        //        _evalStack.Push(targetType);
        //    }
        //}
    }
}
