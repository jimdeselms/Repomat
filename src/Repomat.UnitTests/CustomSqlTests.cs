using Repomat.Databases;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class CustomSqlTests
    {
        [Test]
        public void Create_BuildDatabaseForUnmatchedMethod_Throws()
        {
            var db = DataLayerBuilder.DefineInMemoryDatabase();
            Assert.Throws<RepomatException>(() => db.SetupRepo<Foo, IFooRepo>().CreateRepo());
        }

        [Test]
        public void ExecuteCustomNonQuerySql()
        {
            var repo = CreateRepo();
            repo.CreateTable();
            repo.MethodThatWritesARow(500);
            var foo = repo.Get(500);

            Assert.AreEqual(500, foo.Id);
            Assert.AreEqual("DingleDoodle", foo.Dingle);
        }

        [Test]
        public void ExecuteSingletonQuerySql()
        {
            var repo = CreateRepo();
            repo.CreateTable();
            repo.MethodThatWritesARow(25);
            var foo = repo.DingleSomething("DingleDoodle");

            Assert.AreEqual(25, foo.Id);
            Assert.AreEqual("DingleDoodle", foo.Dingle);
            Assert.IsNull(foo.NullableWhatsit);
            Assert.AreEqual(37.5M, foo.MoneyMoney);
        }

        [Test]
        public void MethodWithResultSetNotInSameOrderAsProperties()
        {
            // Handles the case where the columns in the result set are not
            // in the same order as the parameters of the query.
            var repo = CreateRepo();
            repo.CreateTable();
            repo.MethodThatWritesARow(25);
            var foo = repo.QueryWithDifferentOrder("DingleDoodle");

            Assert.AreEqual(25, foo.Id);
            Assert.AreEqual("DingleDoodle", foo.Dingle);
            Assert.IsNull(foo.NullableWhatsit);
            Assert.AreEqual(37.5M, foo.MoneyMoney);
        }

        [Test]
        public void ExecuteMultiRowQuery()
        {
            var repo = CreateRepo();
            repo.CreateTable();
            repo.MethodThatWritesARow(25);
            repo.MethodThatWritesARow(100);

            var result = repo.GetMultipleFoos().ToArray();
            Assert.AreEqual(25, result[0].Id);
            Assert.AreEqual(100, result[1].Id);
            Assert.AreEqual("DingleDoodle", result[0].Dingle);
            Assert.AreEqual("DingleDoodle", result[1].Dingle);
            Assert.IsNull(result[0].NullableWhatsit);
            Assert.IsNull(result[1].NullableWhatsit);
        }

        [Test]
        public void ExecuteScalarQuery()
        {
            var repo = CreateRepo();
            repo.CreateTable();
            repo.MethodThatWritesARow(2);
            Assert.AreEqual(3M, repo.GetMoneyMoney(2));
        }

        [Test]
        public void ExecuteScalarQueryThatReturnsNull()
        {
            var repo = CreateRepo();
            repo.CreateTable();
            repo.MethodThatWritesARow(2);
            Assert.IsNull(repo.GetMoneyMoney(999));
        }

        [Test]
        public void ExecuteCountScalarQuery()
        {
            var repo = CreateRepo();
            repo.CreateTable();
            repo.MethodThatWritesARow(2);
            repo.MethodThatWritesARow(3);
            repo.MethodThatWritesARow(4);
            Assert.AreEqual(3, repo.GetRowCount());
        }

        private IFooRepo CreateRepo()
        {
            var db = DataLayerBuilder.DefineInMemoryDatabase();
            var repoBuilder = db.SetupRepo<Foo, IFooRepo>();

            repoBuilder.SetupMethod("MethodThatWritesARow")
                .SetCustomSql("insert into Foo values (@someId, 'DingleDoodle', null, 1.5*@someId)");
            repoBuilder.SetupMethod("DingleSomething")
                .SetCustomSql("select Id, Dingle, NullableWhatsit, MoneyMoney from Foo where Dingle = @theString");
            repoBuilder.SetupMethod("QueryWithDifferentOrder")
                .SetCustomSql("select NullableWhatsit, MoneyMoney, Id, Dingle from Foo where Dingle = @theString");
            repoBuilder.SetupMethod("GetMultipleFoos")
                .SetCustomSql("select Id, NullableWhatsit, Dingle, MoneyMoney from Foo");
            repoBuilder.SetupMethodWithParameters("GetMoneyMoney", typeof(int))
                .SetCustomSql("select MoneyMoney from Foo where Id=@id");
            repoBuilder.SetupMethodWithParameters("GetMoneyMoney", typeof(string))
                .SetCustomSql("select MoneyMoney from Foo where Id=CAST(@id TO INT)");
            repoBuilder.SetupMethod("GetRowCount")
                .SetCustomSql("select count(*) from Foo");

            return repoBuilder.CreateRepo();
        }

        public class Foo
        {
            public int Id { get; set; }
            public string Dingle { get; set; }
            public int? NullableWhatsit { get; set; }
            public decimal MoneyMoney { get; set; }
        }

        public interface IFooRepo
        {
            void CreateTable();

            Foo Get(int id);

            void MethodThatWritesARow(int someId);

            Foo DingleSomething(string theString);
            Foo QueryWithDifferentOrder(string theString);

            List<Foo> GetMultipleFoos();

            decimal? GetMoneyMoney(int id);
            decimal GetMoneyMoney(string id);

            int GetRowCount();
        }
    }
}
