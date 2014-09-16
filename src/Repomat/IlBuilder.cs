﻿using Repomat.Schema;
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

        private Stack<Type> _evalStack = new Stack<Type>();

        public IlBuilder(EmitMethodBuilder methodBuilder, ParameterDetails[] parameterTypes)
            : this(methodBuilder.GetILGenerator(), methodBuilder.ReturnType, parameterTypes)
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
            : this(ctor.GetILGenerator(), typeof(void), types.Select(t => new ParameterDetails(t, "x", false, 0)).ToArray())
        {
            
        }
       
        private IlBuilder(ILGenerator ilGen, Type returnType, ParameterDetails[] parms)
        {
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

        public void Call(MethodInfo method)
        {
            if (method.IsVirtual)
            {
                _ilGen.Emit(OpCodes.Callvirt, method);
            }
            else
            {
                _ilGen.Emit(OpCodes.Call, method);
            }
        }

        public void Call(MethodInfo method, params Type[] types)
        {
            if (method.IsVirtual)
            {
                _ilGen.EmitCall(OpCodes.Callvirt, method, types);
            }
            else
            {
                _ilGen.EmitCall(OpCodes.Call, method, types);
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

        public void EndExceptionBlock()
        {
            _ilGen.EndExceptionBlock();
        }

        public void EndScope()
        {
            _ilGen.EndScope();
        }

        public void Initobj(Type t)
        {
            _ilGen.Emit(OpCodes.Initobj, t);
        }

        public void Ldc(int i)
        {
            _ilGen.Emit(OpCodes.Ldc_I4, i);
//            _evalStack.Push(typeof(int));
        }

        public void Ldc(long l)
        {
            _ilGen.Emit(OpCodes.Ldc_I8, l);
//            _evalStack.Push(typeof(long));
        }

        public void Ldarg(int i)
        {
            _ilGen.Emit(OpCodes.Ldarg, i);

//            var parm = _parameterTypes[i];
//            _evalStack.Push(parm.Type);
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
//            _evalStack.Push(typeof(string));
        }

        public void MarkLabel(Label label)
        {
            _ilGen.MarkLabel(label);
        }

        public void Newobj(ConstructorInfo ctor)
        {
            _ilGen.Emit(OpCodes.Newobj, ctor);
        }

        public void Ret()
        {
            //int expectedStackSize = _returnType == typeof(void) ? 0 : 1;

            //if (expectedStackSize != _evalStack.Count)
            //{
            //    throw new RepomatException("Invalid stack size {0}, expected {1}", _evalStack.Count, expectedStackSize);
            //}

            //if (_returnType != typeof(void))
            //{
            //    HandleBoxing(_returnType);

            //    var topType = _evalStack.Peek();
            //    if (!_returnType.IsAssignableFrom(topType))
            //    {
            //        throw new RepomatException("Cannot convert type {0} into type {1}", topType, _returnType);
            //    }

            //}

            _ilGen.Emit(OpCodes.Ret);
        }

        public void Pop()
        {
            _ilGen.Emit(OpCodes.Pop);
//            _evalStack.Pop();
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

        public void IfTrue(Action ifTrue)
        {
            var skip = _ilGen.DefineLabel();

            _ilGen.Emit(OpCodes.Brfalse, skip);
            //            _evalStack.Pop();

            Stack<Type> stackBefore = new Stack<Type>(_evalStack);

            ifTrue();

            _ilGen.MarkLabel(skip);

            //            EnsureStacksAreSame(stackBefore.ToArray(), _evalStack.ToArray());
        }

        public void IfFalse(Action ifFalse)
        {
            var skip = _ilGen.DefineLabel();

            _ilGen.Emit(OpCodes.Brtrue, skip);
            //            _evalStack.Pop();

            Stack<Type> stackBefore = new Stack<Type>(_evalStack);

            ifFalse();

            _ilGen.MarkLabel(skip);

            //            EnsureStacksAreSame(stackBefore.ToArray(), _evalStack.ToArray());
        }

        public void If(Action ifTrue, Action ifFalse)
        {
            var skipTrue = _ilGen.DefineLabel();
            var skipFalse = _ilGen.DefineLabel();

            _ilGen.Emit(OpCodes.Brfalse, skipTrue);
//            _evalStack.Pop();

//            Stack<Type> stackBefore = new Stack<Type>(_evalStack);

            ifTrue();
            _ilGen.Emit(OpCodes.Br, skipFalse);

//            Stack<Type> stackAfterIf = new Stack<Type>(_evalStack);
//            _evalStack = stackBefore;

            _ilGen.MarkLabel(skipTrue);
            ifFalse();
            _ilGen.MarkLabel(skipFalse);

//            Stack<Type> stackAfterElse = new Stack<Type>(_evalStack);

//            EnsureStacksAreSame(stackAfterIf.ToArray(), stackAfterElse.ToArray());
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

        private void HandleBoxing(Type targetType)
        {
            Type topType = _evalStack.Peek();

            if (topType.IsValueType && !targetType.IsValueType)
            {
                _ilGen.Emit(OpCodes.Box, topType);
                _evalStack.Pop();
                _evalStack.Push(targetType);
            }

            if (!topType.IsValueType && targetType.IsValueType)
            {
                _ilGen.Emit(OpCodes.Unbox_Any, targetType);
                _evalStack.Pop();
                _evalStack.Push(targetType);
            }
        }

        private void EnsureStacksAreSame(Type[] branch1, Type[] branch2)
        {
            if (branch1.Length != branch2.Length)
            {
                throw new RepomatException("Evaluation stack must be of same length after both branches of if statement.");
            }

            for (int i = 0; i < branch2.Length; i++)
            {
                if (branch1[i] != branch2[i])
                {
                    throw new RepomatException("Evaluation stack must have same types after both branches of if statement.");
                }
            }
        }

    }
}
