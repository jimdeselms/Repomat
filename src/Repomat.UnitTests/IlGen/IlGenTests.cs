using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using Repomat.IlGen;
using System.Data;
using System.Data.SqlClient;
using Repomat.Schema;
using Dapper;

namespace Repomat.UnitTests.IlGen
{
    [TestFixture]
    public class IlGenTests
    {
        [Test]
        public void BuildClass()
        {
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("MyAssembly"), AssemblyBuilderAccess.RunAndSave);
            var modBuilder = asmBuilder.DefineDynamicModule("MyModule");
            var typeBuilder = modBuilder.DefineType("MyClass");
            var myField = typeBuilder.DefineField("_this", typeof(int), FieldAttributes.Public);
            
            var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new Type[0]);
                        
            var ilGen = ctorBuilder.GetILGenerator();
            
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldc_I4_S, (byte)25);
            ilGen.Emit(OpCodes.Stfld, myField);
            ilGen.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType();
            var obj = type.GetConstructor(new Type[0]).Invoke(new object[0]);

            var fieldInfo = obj.GetType().GetField("_this");
            Assert.AreEqual(25, fieldInfo.GetValue(obj));
        }

        [Test]
        public void BuildConnectionBasedRepo()
        {
            var conn = new SqlConnection();

            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(conn);
            var repoBuilder = dlBuilder.SetupRepo<INothing>();
            var repo = dlBuilder.CreateRepo<INothing>();

            Assert.AreSame(conn, repo.GetType().GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(repo));
        }

        [Test]
        public void BuildConnectionFactoryBasedRepo()
        {
            var repoDef = RepositoryDefBuilder.BuildRepositoryDef<INothing>(NamingConvention.NoOp, NamingConvention.NoOp);

            RepoSqlBuilder b = new RepoSqlBuilder(repoDef, false, RepoConnectionType.ConnectionFactory);
            Type t = b.CreateType();

            var ctor = t.GetConstructor(new[] { typeof(Func<IDbConnection>) });

            Func<IDbConnection> connFactory = () => new SqlConnection();
            var repo = ctor.Invoke(new object[] { connFactory });

            Assert.AreSame(connFactory, repo.GetType().GetField("_connectionFactory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(repo));
        }

        [Test]
        public void SingletonGetTest()
        {
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewSqlConnection());
            var repoBuilder = dlBuilder.SetupRepo<IFooRepo>();
            repoBuilder.SetupMethod("InsertARow")
                .ExecutesSql("insert into Foo values (1, 'Jim')");
            var repo = dlBuilder.CreateIlRepo<IFooRepo>();

            try { repo.DropTable(); } catch { }
            repo.CreateTable();

            repo.InsertARow();

            var person = repo.Get(1);

            Assert.AreEqual("Jim", person.Name);
            Assert.AreEqual(1, person.PersonId);

            repo.DropTable();
        }

        [Test]
        public void SimpleQueryTest()
        {
            var repo = CreateSimpleQueryInterface();
            Assert.AreEqual(45, repo.Returns45());
        }

        [Test]
        public void CustomQueryWithArguments()
        {
            var repo = CreateSimpleQueryInterface();
            Assert.AreEqual(15, repo.ReturnsXMinusY(50, 35));
        }

        [Test]
        public void CustomStatementTest()
        {
            var repo = CreateSimpleQueryInterface();
            repo.InsertARow();

            Assert.AreEqual(1, repo.GetPersonCount());
        }

        [Test]
        public void CustomStatementWithParametersTest()
        {
            var repo = CreateSimpleQueryInterface();
            repo.InsertARowWithId(2);
            repo.InsertARowWithId(4);

            Assert.AreEqual(2, repo.GetPersonCount());
        }

        private IFooRepo CreatePersonRepo()
        {
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewSqlConnection());
            var repoBuilder = dlBuilder.SetupRepo<IFooRepo>();
            var repo = dlBuilder.CreateIlRepo<IFooRepo>();

            try { repo.DropTable(); }
            catch { }
            repo.CreateTable();

            repo.Get(1);

            return repo;
        }

        public interface INothing { }
        public interface IFooRepo
        {
            void DropTable();
            void CreateTable();
            Foo Get(int personId);

            void InsertARow();
        }

        public class Foo
        {
            public int PersonId { get; set; }
            public string Name { get; set; }
        }


        public interface ISimpleQuery
        {
            Person Get(int personId);
            void DropTable();
            void CreateTable();

            int Returns45();
            int ReturnsXMinusY(int x, int y);
            void InsertARow();
            void InsertARowWithId(int personId);
            int GetPersonCount();
        }

        private ISimpleQuery CreateSimpleQueryInterface()
        {
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewSqlConnection());
            var repoBuilder = dlBuilder.SetupRepo<ISimpleQuery>();
            repoBuilder.SetupMethod("Returns45")
                .ExecutesSql("select 45");
            repoBuilder.SetupMethod("ReturnsXMinusY")
                .ExecutesSql("select @x - @y");
            repoBuilder.SetupMethod("InsertARow")
                .ExecutesSql("insert into Person values (1, 'Jim', '2014-01-01', null)");
            repoBuilder.SetupMethod("InsertARowWithId")
                .ExecutesSql("insert into Person values (@personId, 'Jim', '2014-01-01', null)");
            repoBuilder.SetupMethod("GetPersonCount")
                .ExecutesSql("select count(*) from Person");
            
            var repo = dlBuilder.CreateIlRepo<ISimpleQuery>();


            try { repo.DropTable(); }
            catch { }

            repo.CreateTable();

            return repo;
        }
    }

    class FooBar
    {
        public static int DoSomeStuff()
        {
            return Convert.ToInt32((object)2345);
        }
    }

}
