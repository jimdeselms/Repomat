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

        private Stack<Type> _evalStack = new Stack<Type>();

        public IlBuilder(EmitMethodBuilder methodBuilder, ParameterDetails[] parameterTypes)
        {
            _ilGen = methodBuilder.GetILGenerator();
            _returnType = methodBuilder.ReturnType;
            _parameterTypes = parameterTypes;
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

        internal ILGenerator ILGenerator { get { return _ilGen; } }

        public void Ldc(int i)
        {
            _ilGen.Emit(OpCodes.Ldc_I4, i);
            _evalStack.Push(typeof(int));
        }

        public void Ldc(long l)
        {
            _ilGen.Emit(OpCodes.Ldc_I8, l);
            _evalStack.Push(typeof(long));
        }

        public void Ldarg(int i)
        {
            _ilGen.Emit(OpCodes.Ldarg, i);

            var parm = _parameterTypes[i];
            _evalStack.Push(parm.Type);
        }

        public void Ldstr(string s)
        {
            _ilGen.Emit(OpCodes.Ldstr, s);
            _evalStack.Push(typeof(string));
        }

        public LocalBuilder DeclareLocal(Type t)
        {
            return _ilGen.DeclareLocal(t);
        }

        public void Ret()
        {
            int expectedStackSize = _returnType == typeof(void) ? 0 : 1;

            if (expectedStackSize != _evalStack.Count)
            {
                throw new RepomatException("Invalid stack size {0}, expected {1}", _evalStack.Count, expectedStackSize);
            }

            if (_returnType != typeof(void))
            {
                HandleBoxing(_returnType);

                var topType = _evalStack.Peek();
                if (!_returnType.IsAssignableFrom(topType))
                {
                    throw new RepomatException("Cannot convert type {0} into type {1}", topType, _returnType);
                }

            }

            _ilGen.Emit(OpCodes.Ret);
        }

        public void Pop()
        {
            _ilGen.Emit(OpCodes.Pop);
            _evalStack.Pop();
        }

        public void If(Action ifTrue)
        {
            var skip = _ilGen.DefineLabel();

            _ilGen.Emit(OpCodes.Brfalse, skip);
            _evalStack.Pop();

            Stack<Type> stackBefore = new Stack<Type>(_evalStack);

            ifTrue();

            _ilGen.MarkLabel(skip);

            EnsureStacksAreSame(stackBefore.ToArray(), _evalStack.ToArray());
        }

        public void If(Action ifTrue, Action ifFalse)
        {
            var skipTrue = _ilGen.DefineLabel();
            var skipFalse = _ilGen.DefineLabel();

            _ilGen.Emit(OpCodes.Brfalse, skipTrue);
            _evalStack.Pop();

            Stack<Type> stackBefore = new Stack<Type>(_evalStack);

            ifTrue();
            _ilGen.Emit(OpCodes.Br, skipFalse);

            Stack<Type> stackAfterIf = new Stack<Type>(_evalStack);
            _evalStack = stackBefore;

            _ilGen.MarkLabel(skipTrue);
            ifFalse();
            _ilGen.MarkLabel(skipFalse);

            Stack<Type> stackAfterElse = new Stack<Type>(_evalStack);

            EnsureStacksAreSame(stackAfterIf.ToArray(), stackAfterElse.ToArray());
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
