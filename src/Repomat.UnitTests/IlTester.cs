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
    public class IlTester<T> 
    {
        private static int _typeIdx = 1;
        private static bool _hasBeenSaved = false;

        private static readonly AssemblyBuilder _assemblyBuilder;
        private static readonly ModuleBuilder _moduleBuilder;

        private readonly TypeBuilder _typeBuilder;
        private readonly Emit.MethodBuilder _methodBuilder;
        private readonly ILGenerator _ilGenerator;

        static IlTester()
        {
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new System.Reflection.AssemblyName("IlTester.Assembly"), AssemblyBuilderAccess.RunAndSave);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("IlTester", "temp.dll", false);
        }

        public IlTester(params Type[] parmTypes)
        {
            string typeName = "IlTestType" + _typeIdx++;
            _typeBuilder = _moduleBuilder.DefineType(typeName);
            _methodBuilder = _typeBuilder.DefineMethod("IlTestMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(T), parmTypes);
            _ilGenerator = _methodBuilder.GetILGenerator();
        }

        public ILGenerator IL { get { return _ilGenerator; } }

        public T Invoke(params object[] arguments) 
        {
            var type = _typeBuilder.CreateType();

            if (!_hasBeenSaved)
            {
                _hasBeenSaved = true;
                _assemblyBuilder.Save("temp.dll");
            }

            var method = type.GetMethod("IlTestMethod");

            return (T)method.Invoke(null, arguments);
        }
    }
}
