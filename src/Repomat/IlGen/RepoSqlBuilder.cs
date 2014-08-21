using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Repomat.Schema;

namespace Repomat.IlGen
{
    internal class RepoSqlBuilder
    {
        private static int _nextRepoSuffix = 1;

        private static readonly ModuleBuilder _moduleBuilder;
        
        static RepoSqlBuilder()
        {
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("RepomatDynamicRepos"), AssemblyBuilderAccess.Run);
            _moduleBuilder = asmBuilder.DefineDynamicModule("Repos");
        }

        private readonly TypeBuilder _typeBuilder;
        private readonly ILGenerator _ctorIlBuilder;
        private Type _type = null;

        private FieldBuilder _connectionField;

        public RepoSqlBuilder(RepositoryDef repoDef, bool newConnectionEveryTime, RepoConnectionType repoConnectionType)
        {
            _typeBuilder = _moduleBuilder.DefineType(repoDef.RepositoryType.FullName + "_" + _nextRepoSuffix++);
            _typeBuilder.AddInterfaceImplementation(repoDef.RepositoryType);

            switch (repoConnectionType)
            {
                case RepoConnectionType.SingleConnection:
                    _ctorIlBuilder = DefineSingleConnectionCtor();
                    break;
                case RepoConnectionType.ConnectionFactory:
                    _ctorIlBuilder = DefineConnectionFactoryCtor();
                    break;
                default:
                    throw new NotImplementedException();
            }

            var factory = CreateMethodBuilderFactory(repoDef, newConnectionEveryTime);

            foreach (var method in repoDef.Methods)
            {
                GenerateIlForMethod(method, factory);
            }
        }

        private SqlMethodBuilderFactory CreateMethodBuilderFactory(RepositoryDef repoDef, bool newConnectionEveryTime)
        {
            return new SqlMethodBuilderFactory(_typeBuilder, _connectionField, _ctorIlBuilder, repoDef, newConnectionEveryTime);
        }

        private void GenerateIlForMethod(MethodDef method, SqlMethodBuilderFactory factory)
        {
            var methodBuilder = factory.Create(method);
            methodBuilder.GenerateIl();

            switch (method.MethodType)
            {
                case MethodType.Delete:
                case MethodType.CreateTable:
                case MethodType.DropTable:
                case MethodType.Insert:
                case MethodType.Update:
                case MethodType.TableExists:
                case MethodType.Get:
                case MethodType.Custom:
                case MethodType.GetCount:
                case MethodType.Exists:
                case MethodType.Create:
                    break;
                default: throw new RepomatException("Unrecognized method pattern " + method.MethodName);
            }
        }

        public Type CreateType()
        {
            if (_type == null)
            {
                // Finish off the constructor.
                _ctorIlBuilder.Emit(OpCodes.Ret);

                _type = _typeBuilder.CreateType();
            }
            return _type;
        }

        private ILGenerator DefineSingleConnectionCtor()
        {
            return DefineConstructor("_connection", typeof(IDbConnection));
        }

        private ILGenerator DefineConnectionFactoryCtor()
        {
            return DefineConstructor("_connectionFactory", typeof(Func<IDbConnection>));
        }

        private ILGenerator DefineConstructor(string fieldName, Type fieldType)
        {
            _connectionField = _typeBuilder.DefineField(fieldName, fieldType, FieldAttributes.Private);

            var ctor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new Type[] { fieldType });
            var ilBuilder = ctor.GetILGenerator();
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldarg_1);
            ilBuilder.Emit(OpCodes.Stfld, _connectionField);

            return ilBuilder;
        }
    }

    internal enum RepoConnectionType
    {
        SingleConnection,
        ConnectionFactory,
    }
}
