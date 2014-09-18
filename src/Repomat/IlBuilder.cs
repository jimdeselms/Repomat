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

        private Stack<Type> _evalStack = new Stack<Type>();

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
            PopType();
            PushType(t);
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

            if (!method.IsStatic)
            {
                PopType();
            }

            PopType(method.GetParameters().Length);

            if (method.ReturnType != typeof(void))
            {
                PushType(method.ReturnType);
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
            PushType(PeekType());
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

            var originalStack = CopyStack();

            ifFalse();

            var currentStack = CopyStack();

            EnsureStacksAreSame(originalStack.ToArray(), currentStack.ToArray());

            _ilGen.MarkLabel(skipFalse);
        }

        private void If(OpCode opcode, Action ifTrue, Action ifFalse)
        {
            var skipTrue = _ilGen.DefineLabel();
            var skipFalse = _ilGen.DefineLabel();

            _ilGen.Emit(opcode, skipTrue);
            PopType();

            var originalStack = CopyStack();

            ifTrue();
            _ilGen.Emit(OpCodes.Br, skipFalse);

            _ilGen.MarkLabel(skipTrue);

            var stackAfterTrue = CopyStack();
            ReplaceStack(originalStack);

            ifFalse();

            var stackAfterFalse = CopyStack();

            _ilGen.MarkLabel(skipFalse);

            EnsureStacksAreSame(stackAfterTrue.ToArray(), stackAfterFalse.ToArray());
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
            PopType();
        }

        public void Ldc(int i)
        {
            _ilGen.Emit(OpCodes.Ldc_I4, i);
            PushType(typeof(int));
        }

        public void Ldc(long l)
        {
            _ilGen.Emit(OpCodes.Ldc_I8, l);
            PushType(typeof(long));
        }

        public void Ldarg(int i)
        {
            _ilGen.Emit(OpCodes.Ldarg, i);

            if (_isStatic)
            {
                PushType(_parameterTypes[i].Type);
            }
            else
            {
                if (i == 0)
                {
                    PushType(_declaringType);
                }
                else
                {
                    PushType(_parameterTypes[i - 1].Type);
                }
            }
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
                PopType();
            }

            PushType(field.FieldType);
        }

        public void Ldloc(LocalBuilder local)
        {
            _ilGen.Emit(OpCodes.Ldloc, local);
            PushType(local.LocalType);
        }

        public void Ldloca(LocalBuilder local)
        {
            _ilGen.Emit(OpCodes.Ldloca, local);
            PushType(local.LocalType.MakeByRefType());
        }

        public void Ldnull()
        {
            _ilGen.Emit(OpCodes.Ldnull);
            PushType(null);
        }

        public void Ldstr(string s)
        {
            _ilGen.Emit(OpCodes.Ldstr, s);
            PushType(typeof(string));
        }

        public void MarkLabel(Label label)
        {
            _ilGen.MarkLabel(label);
        }

        public void Newarr(Type elementType)
        {
            _ilGen.Emit(OpCodes.Newarr, elementType);

            PopType();
            PushType(elementType.MakeArrayType());
        }

        public void Newobj(ConstructorInfo ctor)
        {
            _ilGen.Emit(OpCodes.Newobj, ctor);

            PopType(ctor.GetParameters().Length);
            PushType(ctor.DeclaringType);
        }

        public void Ret()
        {
            //int expectedStackSize = _returnType == typeof(void) ? 0 : 1;

            //if (expectedStackSize != _evalStack.Count)
            //{
            //    throw new RepomatException("Invalid stack size {0}, expected {1}", _evalStack.Count, expectedStackSize);
            //}

            if (_returnType != typeof(void))
            {
                //HandleBoxing(_returnType);

                //var topType = PeekType();
                //if (!_returnType.IsAssignableFrom(topType))
                //{
                //    throw new RepomatException("Cannot convert type {0} into type {1}", topType, _returnType);
                //}
            }

            if (_returnType != typeof(void))
            {
                PopType();
            }

            _ilGen.Emit(OpCodes.Ret);
        }

        public void Pop()
        {
            _ilGen.Emit(OpCodes.Pop);
            PopType();
        }

        public void Stind_Ref()
        {
            _ilGen.Emit(OpCodes.Stind_Ref);
            PopType();
            PopType();
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
                PopType();
            }

            PopType();
        }

        public void Stloc(LocalBuilder local)
        {
            _ilGen.Emit(OpCodes.Stloc, local);
            PopType();
        }

        public void Throw()
        {
            _ilGen.Emit(OpCodes.Throw);
        }

        public void Unbox(Type type)
        {
            _ilGen.Emit(OpCodes.Unbox_Any, type);
            PopType();
            PushType(type);
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
//            if (branch1.Length != branch2.Length)
//            {
////                throw new RepomatException("Evaluation stack must be of same length after both branches of if statement.");
//            }

//            for (int i = 0; i < branch2.Length; i++)
//            {
//                if (!branch1[i].Equals(branch2[i]))
//                {
//  //                  throw new RepomatException("Evaluation stack must have same types after both branches of if statement.");
//                }
//            }
        }

        private void PushType(Type t)
        {
            _evalStack.Push(t);
        }

        private void PopType(int count=1)
        {
            for (int i = 0; i < count; i++)
            {
                _evalStack.Pop();
            }
        }

        private Type PeekType()
        {
            return _evalStack.Peek();
        }

        private Stack<Type> CopyStack()
        {
            return new Stack<Type>(_evalStack);
        }

        private void ReplaceStack(Stack<Type> stack)
        {
            _evalStack = stack;
        }
    }
}
