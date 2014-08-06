using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Repomat.Schema;
using System.Collections;

namespace Repomat.CodeGen
{
    internal abstract class SqlRepositoryClassBuilderBase<TType, TRepo> : RepositoryClassBuilder<TRepo>
    {
        private bool _newConnectionEveryTime;

        public SqlRepositoryClassBuilderBase(RepositoryDef repositoryDef, bool newConnectionEveryTime) : base(repositoryDef)
        {
            _newConnectionEveryTime = newConnectionEveryTime;
        }

        protected bool NewConnectionEveryTime { get { return _newConnectionEveryTime; } }

        public override string GenerateClassDefinition()
        {
            CodeBuilder builder = new CodeBuilder();

            builder.WriteLine("public class {0} : {1}", ClassName, typeof(TRepo).ToCSharp());
            builder.OpenBrace();
            if (_newConnectionEveryTime)
            {
                builder.WriteLine("private readonly System.Func<System.Data.IDbConnection> _connectionFactory;");
                builder.WriteLine("public {0}(System.Func<System.Data.IDbConnection> connectionFactory)", ClassName);
                builder.OpenBrace();
                builder.WriteLine("_connectionFactory = connectionFactory;");
                builder.CloseBrace();
            }
            else
            {
                builder.WriteLine("private readonly System.Data.IDbConnection _connection;");
                builder.WriteLine("public {0}(System.Data.IDbConnection connection)", ClassName);
                builder.OpenBrace();
                builder.WriteLine("_connection = connection;");
                builder.CloseBrace();
            }

            var factory = CreateMethodBuilderFactory(builder);

            foreach (var method in RepositoryDef.Methods)
            {
                GenerateCodeForMethod(builder, method, factory);
            }

            builder.CloseBrace();

            return builder.ToString();
        }
    }
}
