using NUnit.Framework;
using Repomat.CodeGen;
using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests.CodeGen
{
    [TestFixture]
    class SqlCodeGenerationTests
    {
        [TestCase(DatabaseType.SqlServer, "Hello VARCHAR(MAX)")]
        [TestCase(DatabaseType.SQLite, "Hello VARCHAR(999)")]
        public void CreateTable_VarcharWidthIsNotSpecified_Max(DatabaseType databaseType, string expected)
        {
            var repoDef = CreateRepoDef<Foo, IFooRepo>();
            var code = GenerateCodeForMethod<Foo, IFooRepo>("CreateTable", repoDef, databaseType);
            StringAssert.Contains(expected, code);
        }

        [TestCase(DatabaseType.SqlServer)]
        [TestCase(DatabaseType.SQLite)]
        public void CreateTable_VarcharWidthIsSpecified_UsesSpecifiedWidth(DatabaseType databaseType)
        {
            var repoDef = CreateRepoDef<Foo, IFooRepo>();
            var prop = repoDef.Properties.Where(p => p.PropertyName == "Hello").First();

            PropertyBuilder propBuilder = new PropertyBuilder(prop);
            propBuilder.SetWidth(25);

            var code = GenerateCodeForMethod<Foo, IFooRepo>("CreateTable", repoDef, databaseType);
            StringAssert.Contains("VARCHAR(25)", code);
        }

        private RepositoryDef CreateRepoDef<TType, TRepo>()
        {
            return RepositoryDefBuilder.BuildRepositoryDef<TType, TRepo>(NamingConvention.NoOp, NamingConvention.NoOp);
        }

        private string GenerateCodeForMethod<TType, TRepo>(string method, RepositoryDef repoDef, DatabaseType database = DatabaseType.SqlServer)
        {
            var builder = new CodeBuilder();

            SqlMethodBuilderFactory factory;
            switch (database)
            {
                case DatabaseType.SqlServer: 
                    factory = new SqlServerMethodBuilderFactory(builder, repoDef, false);
                    break;
                case DatabaseType.SQLite: 
                    factory = new SQLiteMethodBuilderFactory(builder, repoDef, false);
                    break;
                default: throw new NotImplementedException();
            }

            var codeGenerator = factory.Create(FindMethod(repoDef, method), null);
            codeGenerator.GenerateCode();

            return builder.ToString();
        }

        private MethodDef FindMethod(RepositoryDef repoDef, string methodName)
        {
            return repoDef.Methods.First(m => m.MethodName == methodName);
        }

        private class Foo
        {
            public int FooId { get; set; }
            public string Hello { get; set; }
        }

        private interface IFooRepo
        {
            void CreateTable();
            Foo Get(int fooId);
        }
    }
}
