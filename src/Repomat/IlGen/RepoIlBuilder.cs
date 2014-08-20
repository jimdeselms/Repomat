using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class RepoIlBuilder
    {
        private static int _nextRepoSuffix = 1;

        private static readonly ModuleBuilder _moduleBuilder;

        static RepoIlBuilder()
        {
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new System.Reflection.AssemblyName("RepomatDynamicRepos"), AssemblyBuilderAccess.Run);
            _moduleBuilder = asmBuilder.DefineDynamicModule("Repos");
        }

        private readonly TypeBuilder _typeBuilder;

        private FieldBuilder _connectionField;

        public RepoIlBuilder(Type interfaceType, RepoConnectionType repoConnectionType)
        {
            _typeBuilder = _moduleBuilder.DefineType(interfaceType.FullName + "_" + _nextRepoSuffix++);
            _typeBuilder.AddInterfaceImplementation(interfaceType);

            switch (repoConnectionType)
            {
                case RepoConnectionType.SingleConnection:
                    DefineSingleConnectionCtor();
                    break;
                case RepoConnectionType.ConnectionFactory:
                    DefineConnectionFactoryCtor();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public Type CreateType()
        {
            return _typeBuilder.CreateType();
        }

        private void DefineSingleConnectionCtor()
        {
            DefineConstructor("_connection", typeof(IDbConnection));
        }

        private void DefineConnectionFactoryCtor()
        {
            DefineConstructor("_connectionFactory", typeof(Func<IDbConnection>));
        }

        private void DefineConstructor(string fieldName, Type fieldType)
        {
            _connectionField = _typeBuilder.DefineField(fieldName, fieldType, FieldAttributes.Private);

            var ctor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new Type[] { fieldType });
            var ilBuilder = ctor.GetILGenerator();
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldarg_1);
            ilBuilder.Emit(OpCodes.Stfld, _connectionField);
            ilBuilder.Emit(OpCodes.Ret);
        }
    }

    internal enum RepoConnectionType
    {
        SingleConnection,
        ConnectionFactory,
    }
}
