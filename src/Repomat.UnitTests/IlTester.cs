//#define SAVE_ASSEMBLY

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Emit = System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Repomat.UnitTests.IlGen;

namespace Repomat.UnitTests
{
    public class IlTester
    {
        private static int _typeIdx = 1;
        private static bool _hasBeenSaved = false;

        private static readonly AssemblyBuilder _assemblyBuilder;
        private static readonly ModuleBuilder _moduleBuilder;

        private readonly TypeBuilder _typeBuilder;
        private readonly Emit.MethodBuilder _methodBuilder;
        private readonly IlBuilder _ilBuilder;

        private readonly Type _returnType;
        
        static IlTester()
        {
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new System.Reflection.AssemblyName("IlTester.Assembly"), AssemblyBuilderAccess.RunAndSave);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("IlTester", "temp.dll", false);
        }

        public static IlTester Create<T>(params Type[] parmTypes)
        {
            return new IlTester(typeof(T), parmTypes);
        }

        public static IlTester CreateVoid(params Type[] parmTypes)
        {
            return new IlTester(typeof(void), parmTypes);
        }

        private IlTester(Type returnType, params Type[] parmTypes)
        {
            _returnType = returnType;

            string typeName = "IlTestType" + _typeIdx++;
            _typeBuilder = _moduleBuilder.DefineType(typeName);
            _methodBuilder = _typeBuilder.DefineMethod("IlTestMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, _returnType, parmTypes);
            _ilBuilder = new IlBuilder(_methodBuilder, parmTypes);
        }

        internal ILGenerator ILGenerator { get { return _ilBuilder.ILGenerator; } }

        internal IlBuilder IL { get { return _ilBuilder; } }

        public T Invoke<T>(params object[] arguments)
        {
            var type = _typeBuilder.CreateType();

#if SAVE_ASSEMBLY
            if (!_hasBeenSaved)
            {
                _hasBeenSaved = true;
                _assemblyBuilder.Save("temp.dll");
            }
#endif

            var method = type.GetMethod("IlTestMethod");

            return (T)method.Invoke(null, arguments);
        }

        public void InvokeVoid(params object[] arguments)
        {
            Invoke<object>(arguments);
        }
    }
}
