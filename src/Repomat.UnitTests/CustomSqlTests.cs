using Repomat.Databases;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Schema.Validators;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class CustomSqlTests
    {
        [Test]
        public void Create_BuildDatabaseForUnmatchedMethod_Throws()
        {
            var db = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
            Assert.Throws<RepomatException>(() => db.SetupRepo<IFooRepo>().Create());
        }

        [Test]
        public void ExecuteCustomNonQuerySql()
        {
            var repo = CreateFooRepo();
            repo.CreateTheTable();
            repo.MethodThatWritesARow(500);
            var foo = repo.Get(500);

            Assert.AreEqual(500, foo.Id);
            Assert.AreEqual("DingleDoodle", foo.ColumnNameDifferentFromPropertyName);
        }

        [Test]
        public void ExecuteSingletonQuerySql()
        {
            var repo = CreateFooRepo();
            repo.CreateTheTable();
            repo.MethodThatWritesARow(25);
            var foo = repo.DingleSomething("DingleDoodle");

            Assert.AreEqual(25, foo.Id);
            Assert.AreEqual("DingleDoodle", foo.ColumnNameDifferentFromPropertyName);
            Assert.IsNull(foo.NullableWhatsit);
            Assert.AreEqual(37.5M, foo.MoneyMoney);
        }

        //[Test]
        //public void MethodWithResultSetNotInSameOrderAsProperties()
        //{
        //    // Handles the case where the columns in the result set are not
        //    // in the same order as the parameters of the query.
        //    var repo = CreateFooRepo();
        //    repo.CreateTheTable();
        //    repo.MethodThatWritesARow(25);
        //    var foo = repo.QueryWithDifferentOrder("DingleDoodle");

        //    Assert.AreEqual(25, foo.Id);
        //    Assert.AreEqual("DingleDoodle", foo.ColumnNameDifferentFromPropertyName);
        //    Assert.IsNull(foo.NullableWhatsit);
        //    Assert.AreEqual(37.5M, foo.MoneyMoney);
        //}

        //[Test]
        //public void ExecuteMultiRowQuery()
        //{
        //    var repo = CreateFooRepo();
        //    repo.CreateTheTable();
        //    repo.MethodThatWritesARow(25);
        //    repo.MethodThatWritesARow(100);

        //    var result = repo.GetMultipleFoos().ToArray();
        //    Assert.AreEqual(25, result[0].Id);
        //    Assert.AreEqual(100, result[1].Id);
        //    Assert.AreEqual("DingleDoodle", result[0].ColumnNameDifferentFromPropertyName);
        //    Assert.AreEqual("DingleDoodle", result[1].ColumnNameDifferentFromPropertyName);
        //    Assert.IsNull(result[0].NullableWhatsit);
        //    Assert.IsNull(result[1].NullableWhatsit);
        //}

        [Test]
        public void ExecuteScalarQuery()
        {
            var repo = CreateFooRepo();
            repo.CreateTheTable();
            repo.MethodThatWritesARow(2);
            Assert.AreEqual(3M, repo.GetMoneyMoney(2));
        }

        [Test]
        public void ExecuteScalarQueryThatReturnsNull()
        {
            var repo = CreateFooRepo();
            repo.CreateTheTable();
            repo.MethodThatWritesARow(2);
            Assert.IsNull(repo.GetMoneyMoney(999));
        }

        [Test]
        public void ExecuteCountScalarQuery()
        {
            var repo = CreateFooRepo();
            repo.CreateTheTable();
            repo.MethodThatWritesARow(2);
            repo.MethodThatWritesARow(3);
            repo.MethodThatWritesARow(4);
            Assert.AreEqual(3, repo.GetRowCount());
        }

        [Test]
        public void StoredProcedure_InDbThatSupportsIt_ExecutesStoredProcedure()
        {
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewSqlConnection());
            var repoBuilder = dlBuilder.SetupRepo<IStoredProcedureRepo>();

            repoBuilder.SetupMethod("CreateProc")
                .ExecutesSql("CREATE PROCEDURE \r\nSayGreeting @name varchar(100), @greeting varchar(100) AS SELECT @greeting + ', ' + @name")
                .SetEntityType(typeof(void));
            repoBuilder
                .SetupMethod("DropProc")
                .ExecutesSql("DROP PROCEDURE \nSayGreeting")
                .SetEntityType(typeof(void));

            // By default, calls the proc with the same name as the method.
            repoBuilder.SetupMethod("SayGreeting")
                .SetEntityType(typeof(void))
                .ExecutesStoredProcedure();

            // But it can also be overridden.
            repoBuilder.SetupMethod("Greet")
                .SetEntityType(typeof(void))
                .ExecutesStoredProcedure("SayGreeting");

            // Runs the same proc, but just ignores the result. Want to make sure that both queries and non-queries are handled.
            repoBuilder.SetupMethod("SameThingButNonQuery")
                .SetEntityType(typeof(void))
                .ExecutesStoredProcedure("SayGreeting");

            var repo = dlBuilder.CreateRepo<IStoredProcedureRepo>();

            try
            {
                // Just drop this in case the previous test didn't finish for some reason.
                try { repo.DropProc(); } catch { }

                repo.CreateProc();

                // Notice that the parameters are in a different order; as long as they're all named, they'll get matched
                // up with the right proc parameters.
                Assert.AreEqual("Howdy, Pardner", repo.SayGreeting("Howdy", "Pardner"));
                Assert.AreEqual("Hi, Jim", repo.Greet("Jim", "Hi"));

                // And just make sure that this one doesn't fail.
                // TODO: Just write a real test here and actually make sure this did something?
                repo.SameThingButNonQuery("Fred", "Hola");
            }
            finally
            {
                repo.DropProc();
            }
        }

        [Test]
        public void RepoThatReturnsListOfStrings()
        {
            var repo = CreateStringRepo();
            repo.CreateTheTable();

            repo.InsertString("Hello");
            repo.InsertString("World");

            var strings = repo.GetStrings();
            CollectionAssert.AreEqual(new[] { "Hello", "World" }, strings);

            var stringsAsList = repo.GetStringsAsList();
            CollectionAssert.AreEqual(new[] { "Hello", "World" }, stringsAsList);

            var stringsAsArray = repo.GetStringsAsArray();
            CollectionAssert.AreEqual(new[] { "Hello", "World" }, stringsAsArray);
        }

        [Test]
        public void ReallySimpleTest()
        {
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
            var repoBuilder = dlBuilder.SetupRepo<IReallySimple>();
            repoBuilder.SetupMethod("Doit").ExecutesSql("select 'Jim' as Name");
            var repo = dlBuilder.CreateRepo<IReallySimple>();

            SimpleClass c = repo.Doit();
        }

        private IFooRepo CreateFooRepo()
        {
            var columnNamingConvention = NamingConvention.NoOp
                .AddOverride("ColumnNameDifferentFromPropertyName", "Dingle");

            var db = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
            db.SetColumnNamingConvention(columnNamingConvention);
            var repoBuilder = db.SetupRepo<IFooRepo>();

            repoBuilder.SetupMethod("CreateTheTable")
                .ExecutesSql("create table Foo (Id int, Dingle varchar(100), NullableWhatsit int, MoneyMoney money)");

            repoBuilder.SetupMethod("MethodThatWritesARow")
                .ExecutesSql("insert into Foo \r\nvalues (@someId, 'DingleDoodle', null, 1.5*@someId)");
            repoBuilder.SetupMethod("DingleSomething")
                .ExecutesSql("select Id, Dingle, NullableWhatsit, MoneyMoney \nfrom Foo where Dingle = @theString");
//            repoBuilder.SetupMethod("QueryWithDifferentOrder")
//                .ExecutesSql("select NullableWhatsit, MoneyMoney, Id, Dingle \r\nfrom Foo where Dingle = @theString");
//            repoBuilder.SetupMethod("GetMultipleFoos")
//                .ExecutesSql("select Id, NullableWhatsit, Dingle, MoneyMoney from Foo");
            repoBuilder.SetupMethodWithParameters("GetMoneyMoney", typeof(int))
                .ExecutesSql("select MoneyMoney from Foo where Id=@id");
            repoBuilder.SetupMethodWithParameters("GetMoneyMoney", typeof(string))
                .ExecutesSql("select MoneyMoney from Foo where Id=CAST(@id TO INT)");
            repoBuilder.SetupMethod("GetRowCount")
                .ExecutesSql("select count(*) from Foo");

            return repoBuilder.Create();
        }

        private IStringRepo CreateStringRepo()
        {
            var db = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
            var repoBuilder = db.SetupRepo<IStringRepo>();
            repoBuilder.SetupMethod("CreateTheTable")
                .ExecutesSql("create table stringTable (s varchar(100))");

            repoBuilder.SetupMethod("InsertString")
                .ExecutesSql("insert into stringTable values (@value)");

            repoBuilder.SetupMethod("GetStrings")
                .ExecutesSql("select s from stringTable");

            repoBuilder.SetupMethod("GetStringsAsList")
                .ExecutesSql("select s from stringTable");

            repoBuilder.SetupMethod("GetStringsAsArray")
                .ExecutesSql("select s from stringTable");

            return db.CreateRepo<IStringRepo>();
        }

        public class Foo
        {
            public int Id { get; set; }
            public string ColumnNameDifferentFromPropertyName { get; set; }
            public int? NullableWhatsit { get; set; }
            public decimal MoneyMoney { get; set; }
        }

        public interface IFooRepo
        {
            void CreateTheTable();

            Foo Get(int id);

            void MethodThatWritesARow(int someId);

            Foo DingleSomething(string theString);
//            Foo QueryWithDifferentOrder(string theString);

//            List<Foo> GetMultipleFoos();

            decimal? GetMoneyMoney(int id);
            decimal GetMoneyMoney(string id);

            int GetRowCount();
        }

        public interface IStringRepo
        {
            void CreateTheTable();
            IEnumerable<string> GetStrings();

            string[] GetStringsAsArray();

            List<string> GetStringsAsList();

            void InsertString(string value);
        }

        public interface IStoredProcedureRepo
        {
            void CreateProc();
            void DropProc();

            string SayGreeting(string greeting, string name);
            string Greet(string name, string greeting);

            void SameThingButNonQuery(string name, string greeting);
        }

        public interface IReallySimple
        {
            SimpleClass Doit();
        }

        public class SimpleClass
        {
            public string Name { get; set; }
        }
    }
}
