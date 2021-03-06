﻿// Turn this on if you want to generate the assembly so that you can look at the generated IL
// and decompile it.
#define OUTPUT_ASSEMBLY


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
    internal abstract class RepoSqlBuilder
    {
        private static int _nextRepoSuffix = 1;

        private static readonly AssemblyBuilder _assemblyBuilder;
        private static readonly ModuleBuilder _moduleBuilder;

        private static bool _outputAssemblyHasBeenSaved = false;

        static RepoSqlBuilder()
        {
#if OUTPUT_ASSEMBLY
                _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("RepomatDynamicRepos"), AssemblyBuilderAccess.RunAndSave);
                _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Repos", "temp.dll");
#else
                _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("RepomatDynamicRepos"), AssemblyBuilderAccess.Run);
                _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Repos");
#endif
        }

        private readonly TypeBuilder _typeBuilder;
        private readonly IlBuilder _ctorIlBuilder;
        private Type _type = null;

        private FieldBuilder _connectionField;

        public RepoSqlBuilder(RepositoryDef repoDef, bool newConnectionEveryTime)
        {
            var repoConnectionType = newConnectionEveryTime ? RepoConnectionType.ConnectionFactory : RepoConnectionType.SingleConnection;

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

        protected TypeBuilder TypeBuilder { get { return _typeBuilder; } }
        protected IlBuilder CtorIlBuilder { get { return _ctorIlBuilder; } }
        protected FieldBuilder ConnectionField { get { return _connectionField; } }

        protected abstract SqlMethodBuilderFactory CreateMethodBuilderFactory(RepositoryDef repoDef, bool newConnectionEveryTime);

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
                case MethodType.Upsert:
                    break;
                default: throw new RepomatException("Unrecognized method pattern " + method.MethodName);
            }
        }

        public Type CreateType()
        {
            if (_type == null)
            {
                // Finish off the constructor.
                _ctorIlBuilder.Ret();

                _type = _typeBuilder.CreateType();
                
#if OUTPUT_ASSEMBLY
                if (!_outputAssemblyHasBeenSaved)
                {
                    _assemblyBuilder.Save("temp.dll");
                    _outputAssemblyHasBeenSaved = true;
                }
#endif
            }
            return _type;
        }

        private IlBuilder DefineSingleConnectionCtor()
        {
            return DefineConstructor("_connection", typeof(IDbConnection));
        }

        private IlBuilder DefineConnectionFactoryCtor()
        {
            return DefineConstructor("_connectionFactory", typeof(Func<IDbConnection>));
        }

        private IlBuilder DefineConstructor(string fieldName, Type fieldType)
        {
            _connectionField = _typeBuilder.DefineField(fieldName, fieldType, FieldAttributes.Private);

            var ctor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new Type[] { fieldType });
            var ilBuilder = new IlBuilder(ctor, new Type[] { fieldType });
            ilBuilder.Ldarg(0);
            ilBuilder.Ldarg(1);
            ilBuilder.Stfld(_connectionField);

            return ilBuilder;
        }
    }

    internal enum RepoConnectionType
    {
        SingleConnection,
        ConnectionFactory,
    }
}
