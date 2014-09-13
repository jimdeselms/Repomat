using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using EmitMethodBuilder = System.Reflection.Emit.MethodBuilder;

namespace Repomat
{
    internal class IlBuilder
    {
        private readonly EmitMethodBuilder _methodBuilder;
        private readonly ILGenerator _ilGen;
        private readonly Type _returnType;

        private Stack<Type> _evalStack = new Stack<Type>();

        public IlBuilder(EmitMethodBuilder methodBuilder)
        {
            _methodBuilder = methodBuilder;
            _ilGen = _methodBuilder.GetILGenerator();
            _returnType = methodBuilder.ReturnType;
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
            }

            _ilGen.Emit(OpCodes.Ret);
        }

        private void HandleBoxing(Type targetType)
        {
            if (_evalStack.Peek().IsValueType && !targetType.IsValueType)
            {
                _ilGen.Emit(OpCodes.Box, _evalStack.Peek());
                _evalStack.Pop();
                _evalStack.Push(targetType);
            }
        }

    }
}
