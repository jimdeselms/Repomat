using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Repomat.Schema;

namespace Repomat.CodeGen
{
    internal abstract class RepositoryClassBuilder<TRepo>
    {
        private readonly string _className;
        private readonly RepositoryDef _repositoryDef;

        protected RepositoryClassBuilder(RepositoryDef repositoryDef)
        {
            _repositoryDef = repositoryDef;
            _className = BuildClassName();
        }

        private string BuildClassName()
        {
            string guidToAlnum = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");

            return string.Format("__{0}_{1}", typeof(TRepo).Name, guidToAlnum);
        }

        public string ClassName { get { return _className; } }
        
        protected RepositoryDef RepositoryDef { get { return _repositoryDef; } }

        public abstract string GenerateClassDefinition();

        internal void GenerateCodeForMethod(CodeBuilder builder, MethodDef details, MethodBuilderFactory methodBuilderFactory)
        {
            var methodBuilder = methodBuilderFactory.Create(details);
            methodBuilder.GenerateCode();

            switch (details.MethodType)
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
                default: throw new RepomatException("Unrecognized method pattern " + details.MethodName);
            }
        }

        internal abstract MethodBuilderFactory CreateMethodBuilderFactory(CodeBuilder codeBuilder);

        protected ParameterInfo GetTransactionParameterOrNull(MethodInfo methodInfo)
        {
            return methodInfo.GetParameters().FirstOrDefault(p => p.ParameterType == typeof(System.Data.IDbTransaction));
        }
    }
}
