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
        public void CreateTable_VarcharWidthIsNotSqlServer_Max()
        {
            var repoDef = CreateRepoDef<IFooRepo>();
            var code = GenerateCodeForMethod<IFooRepo>("CreateTable", repoDef, DatabaseType.SqlServer);
            StringAssert.Contains("Hello VARCHAR(MAX)", code);
        }

        public void CreateTable_VarcharWidthIsNotSpecifiedSQLite_Max()
        {
            var repoDef = CreateRepoDef<IFooRepo>();
            var code = GenerateCodeForMethod<IFooRepo>("CreateTable", repoDef, DatabaseType.SQLite);
            StringAssert.Contains("Hello VARCHAR(999)", code);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CreateTable_VarcharWidthIsSpecified_UsesSpecifiedWidth(bool useSqlServer)
        {
            var repoDef = CreateRepoDef<IFooRepo>();
            var prop = repoDef.Properties.Where(p => p.PropertyName == "Hello").First();

            PropertyBuilder propBuilder = new PropertyBuilder(prop);
            propBuilder.SetWidth(25);

            var dbType = useSqlServer ? DatabaseType.SqlServer : DatabaseType.SQLite;

            var code = GenerateCodeForMethod<IFooRepo>("CreateTable", repoDef, dbType);
            StringAssert.Contains("VARCHAR(25)", code);
        }

        private RepositoryDef CreateRepoDef<TRepo>()
        {
            return RepositoryDefBuilder.BuildRepositoryDef<TRepo>(NamingConvention.NoOp, NamingConvention.NoOp);
        }

        private string GenerateCodeForMethod<TRepo>(string method, RepositoryDef repoDef, DatabaseType database=null)
        {
            if (database == null)
            {
                database = DatabaseType.SqlServer;
            }

            var builder = new CodeBuilder();

            MethodBuilderFactory factory;
            if (database == DatabaseType.SqlServer)
            {
                factory = new SqlServerMethodBuilderFactory(builder, repoDef, false);
            }
            else if (database == DatabaseType.SQLite)
            {
                factory = new SQLiteMethodBuilderFactory(builder, repoDef, false);
            }
            else
            {
                throw new NotImplementedException();
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
