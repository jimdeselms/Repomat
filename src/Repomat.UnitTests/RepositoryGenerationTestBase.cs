using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Databases;
using NUnit.Framework;
using Repomat.Schema;

namespace Repomat.UnitTests
{
    public abstract class RepositoryGenerationTestBase
    {
        [Test]
        public void CreateTable_CreatesTable()
        {
            var repo = CreateRepo();
            repo.CreateTable();

            Assert.IsTrue(repo.TableExists());
        }

        [Test]
        public void DropTable_DropsTable()
        {
            var repo = CreateRepo();
            repo.CreateTable();
            repo.DropTable();

            Assert.IsFalse(repo.TableExists());
        }

        [Test]
        public void TableExists_TableExists_True()
        {
            var repo = CreateRepo();
            repo.CreateTable();
            Assert.IsTrue(repo.TableExists());
        }

        [Test]
        public void TableExists_TableDoesNotExist_False()
        {
            var repo = CreateRepo();
            Assert.IsFalse(repo.TableExists());
        }

        [Test]
        public void GetAll_GetsAllRows()
        {
            var repo = CreateRepoWithJimAndSusan();

            var all = repo.GetAll().ToArray();

            Assert.AreEqual(2, all.Length);
            Assert.AreEqual(1, all[0].PersonId);
            Assert.AreEqual("Jim", all[0].Name);
            Assert.AreEqual(new DateTime(1969, 3, 9), all[0].Birthday);
            Assert.AreEqual(2, all[1].PersonId);
            Assert.AreEqual("Susan", all[1].Name);
            Assert.AreEqual(new DateTime(1971, 12, 13), all[1].Birthday);
        }

        [Test]
        public void GetAll_EmptyTable_GetsZeroRows()
        {
            var repo = CreateRepo();
            repo.CreateTable();

            var all = repo.GetAll().ToArray();

            Assert.AreEqual(0, all.Length);
        }

        [Test]
        public void GetBy_FiltersResult()
        {
            var repo = CreateRepoWithJimAndSusan();

            var all = repo.GetByName("Jim").ToArray();

            Assert.AreEqual(1, all.Length);
            Assert.AreEqual(1, all[0].PersonId);
            Assert.AreEqual("Jim", all[0].Name);
            Assert.AreEqual(new DateTime(1969, 3, 9), all[0].Birthday);
        }

        [Test]
        public void FindBy_FiltersResult()
        {
            var repo = CreateRepoWithJimAndSusan();

            Person person = repo.FindByBirthday(new DateTime(1969, 3, 9));

            Assert.AreEqual(1, person.PersonId);
            Assert.AreEqual("Jim", person.Name);
            Assert.AreEqual(new DateTime(1969, 3, 9), person.Birthday);
        }

        [Test]
        public void GetSingletonBy_ReturnsSingle()
        {
            var repo = CreateRepoWithJimAndSusan();

            var susan = repo.GetSingletonByName("Susan");
            Assert.AreEqual(2, susan.PersonId);
            Assert.AreEqual("Susan", susan.Name);
        }

        [Test]
        public void GetSingletonBy_RowNotFound_Throws()
        {
            var repo = CreateRepoWithJimAndSusan();

            Assert.Throws<RepomatException>(() => repo.GetSingletonByName("Shlomo"));
        }

        [Test]
        public void TryGetBy_RowNotFound_ReturnsFalseAndNull()
        {
            var repo = CreateRepoWithJimAndSusan();

            Person person;
            Assert.IsFalse(repo.TryGet(9, out person));
            Assert.IsNull(person);
        }

        [Test]
        public void Insert_MultipleRowsConcurrently_BlockWhenUsingSingleConnection()
        {
            // If we're dealing with a repository that shares a single connection, it's critical that we don't allow multiple 
            // threads to access that connection. If we don't, it causes deadlocks, or it causes you to execute multiple readers,
            // etc.
            var repo = CreateRepoWithJimAndSusan();

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 250; i++)
            {
                Person p = new Person { PersonId = i + 5, Name = "foo" + i, Birthday = new DateTime(2012, 1, 1) };
                tasks.Add(Task.Factory.StartNew(() => repo.Insert(p)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [Test]
        public void Insert_NewConnectionEveryTime_Works()
        {
            var repo = CreateRepoWithJimAndSusan(newConnectionEveryTime: true);

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 250; i++)
            {
                Person p = new Person { PersonId = i + 5, Name = "foo" + i, Birthday = new DateTime(2012, 1, 1) };
                tasks.Add(Task.Factory.StartNew(() => repo.Insert(p)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [Test]
        public void Insert_WithTransaction_Works()
        {
            var repo = CreateRepoWithJimAndSusan();

            var txn = _connection.BeginTransaction();

            Person myPerson = new Person { PersonId = 999, Birthday = new DateTime(2014, 5, 5), Name = "Furby" };
            repo.Insert(myPerson, txn);

            txn.Commit();

            Person otherPerson = repo.Get(999);
            Assert.AreEqual(999, otherPerson.PersonId);
            Assert.AreEqual(new DateTime(2014, 5, 5), otherPerson.Birthday);
            Assert.AreEqual("Furby", otherPerson.Name);
        }

        [Test]
        public void Insert_WithPassedConnection_UsesPassedConnection()
        {
            var repo = CreateRepoWithJimAndSusan();

            // We're passing in a connection, but it's not open.
            // This will fail with an exception, which proves that it used
            // the bad connection.
            using (var conn = CreateConnection(open: false))
            {
                Assert.Throws<InvalidOperationException>(() => repo.TableExists(conn));
            }
        }

        [Test]
        public void TryGetBy_RowFound_ReturnsTrueAndRow()
        {
            var repo = CreateRepoWithJimAndSusan();

            Person person;
            Assert.IsTrue(repo.TryGet(1, out person));
            Assert.AreEqual("Jim", person.Name);
            Assert.AreEqual(1, person.PersonId);
        }

        [Test]
        public void TryGetBy_TooManyFound_Throws()
        {
            var repo = CreateRepoWithJimAndSusan();

            Person dupe = new Person { PersonId = 9, Name = "Jim", Birthday = new DateTime(2011, 1, 1) };
            repo.Insert(dupe);

            // Even though this is a TryGet, the situation where there are two matching values is still an exception situation.
            // If the user wanted this to be successful, they'd have to call a non-singleton method.
            Person person;
            Assert.Throws<RepomatException>(() => repo.TryGetByName("Jim", out person));
        }

        [Test]
        public void GetSingletonBy_TooManyFound_Throws()
        {
            var repo = CreateRepoWithJimAndSusan();

            var jimTwo = new Person { PersonId = 3, Name = "Jim", Birthday = new DateTime(2000, 1, 1) };
            repo.Insert(jimTwo);

            Assert.Throws<RepomatException>(() => repo.GetSingletonByName("Jim"));
        }

        [Test]
        public void GetSingletonBy_TooManyFoundWithLooseBehavior_ReturnsFirst()
        {
            var repo = CreateRepoWithJimAndSusan(getMethodBehavior: SingletonGetMethodBehavior.Loose);

            var jimTwo = new Person { PersonId = 3, Name = "Jim", Birthday = new DateTime(2000, 1, 1) };
            repo.Insert(jimTwo);

            // The first row is returned, and it should match the row that was already in the database
            var first = repo.GetSingletonByName("Jim");
            Assert.AreEqual(1, first.PersonId);
        }

        [Test]
        public void GetSingletonBy_RowNotFoundWithLooseBehavior_ReturnsNull()
        {
            var repo = CreateRepoWithJimAndSusan(getMethodBehavior: SingletonGetMethodBehavior.Loose);

            Assert.IsNull(repo.GetSingletonByName("Shlomo"));
        }

        [Test]
        public void TryGetBy_TooManyFoundWithLooseBehavior_ReturnsFirst()
        {
            var repo = CreateRepoWithJimAndSusan(getMethodBehavior: SingletonGetMethodBehavior.Loose);

            Person dupe = new Person { PersonId = 9, Name = "Jim", Birthday = new DateTime(2011, 1, 1) };
            repo.Insert(dupe);

            // The first row is returned, and it should match the row that was already in the database
            Person person;
            Assert.IsTrue(repo.TryGetByName("Jim", out person));
            Assert.AreEqual(1, person.PersonId);
        }

        [Test]
        public void Delete_DeletesRow()
        {
            var repo = CreateRepoWithJimAndSusan();

            var jim = new Person { PersonId = 1 };

            repo.Delete(jim);

            Person susan = null;

            Assert.IsFalse(repo.TryGet(1, out jim));
            Assert.IsTrue(repo.TryGet(2, out susan));
        }

        [Test]
        public void Update_UpdatesRow()
        {
            var repo = CreateRepoWithJimAndSusan();

            var susan = new Person { PersonId = 2, Name = "Suzie", Birthday = new DateTime(2000, 1, 1) };

            repo.Update(susan);

            Person susan2 = repo.Get(2);

            Assert.AreEqual(2, susan2.PersonId);
            Assert.AreEqual("Suzie", susan2.Name);
            Assert.AreEqual(new DateTime(2000, 1, 1), susan2.Birthday);
        }

        [Test]
        public void Create_CreatesNewRow()
        {
            var repo = CreateRepoWithCreateMethod();

            var micah = new Person { Name = "Micah", Birthday = new DateTime(2004, 6, 8) };
            var nelly = new Person { Name = "Nelly", Birthday = new DateTime(2009, 1, 9) };

            repo.Create(micah);
            repo.Create(nelly);

            Assert.AreEqual(1, micah.PersonId);
            Assert.AreEqual(2, nelly.PersonId);
        }

        [Test]
        public void Create_CreatesReturningInt_ReturnsNewValue()
        {
            var repo = CreateRepoWithCreateMethod();

            var micah = new Person { Name = "Micah", Birthday = new DateTime(2004, 6, 8) };
            var nelly = new Person { Name = "Nelly", Birthday = new DateTime(2009, 1, 9) };

            var micahId = repo.CreateReturningInt(micah);
            var nellyId = repo.CreateReturningInt(nelly);

            Assert.AreEqual(1, micahId);
            Assert.AreEqual(2, nellyId);
        }

        [Test]
        public void CreateRepository_ConstructorInjectedClass()
        {
            var repo = CreateFactory(_connection)
                .SetupRepo<ConstructorInjected, IConstructorInjectedRepository>()
                .CreateRepo();
            if (repo.TableExists())
            {
                repo.DropTable();
            }
            repo.CreateTable();

            ConstructorInjected jim = new ConstructorInjected(1, "Jim", new DateTime(2000, 1, 1));
            repo.Insert(jim);

            var jim2 = repo.Get(1);
            Assert.AreEqual(1, jim.PersonId);
            Assert.AreEqual("Jim", jim.Name);
            Assert.AreEqual(new DateTime(2000, 1, 1), jim.Birthday);
        }

        [Test]
        public void ClassWithEnum_InsertAndGet_WorkCorrectly()
        {
            var repo = CreateFactory(_connection)
                .SetupRepo<ColorThing, IColorThingRepo>()
                .CreateRepo();

            if (repo.TableExists())
            {
                repo.DropTable();
            }
            repo.CreateTable();

            ColorThing t1 = new ColorThing(123, Color.Blue, BigColor.BigWhite, LittleColor.LittleRed, Color.White);
            repo.Insert(t1);

            ColorThing t2 = new ColorThing(333, Color.Red, BigColor.BigBlue, LittleColor.LittleWhite, null);
            repo.Insert(t2);

            ColorThing other = repo.Get(123);

            Assert.AreEqual(123, other.Id);
            Assert.AreEqual(Color.Blue, other.Color);
            Assert.AreEqual(BigColor.BigWhite, other.BigColor);
            Assert.AreEqual(LittleColor.LittleRed, other.LittleColor);
            Assert.AreEqual(Color.White, other.NullableColor);
        }

        [Test]
        public void GetCount_GetsCount()
        {
            var repo = CreateRepoWithJimAndSusan();
            var person1 = new Person() { Name = "Fred", PersonId = 20, Birthday = DateTime.Now };
            var person2 = new Person() { Name = "Fred", PersonId = 30, Birthday = DateTime.Now };
            var person3 = new Person() { Name = "Fred", PersonId = 40, Birthday = DateTime.Now };

            repo.Insert(person1);
            repo.Insert(person2);
            repo.Insert(person3);

            Assert.AreEqual(1, repo.GetCountByName("Jim"));
            Assert.AreEqual(0, repo.GetCountByName("Ernie"));
        }

        [Test]
        public void Exists_TrueIfExistsFalseIfNot()
        {
            var repo = CreateRepoWithJimAndSusan();

            Assert.IsTrue(repo.GetExistsByName("Jim"));
            Assert.IsFalse(repo.GetExistsByName("Freddy"));
        }

        private IDbConnection _connection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _connection = CreateConnection();
        }

        protected abstract IDbConnection CreateConnection(bool open=true);

        protected virtual DataLayerBuilder CreateFactory(IDbConnection conn)
        {
            return DataLayerBuilder.DefineSqlDatabase(conn);
        }

        protected abstract DataLayerBuilder CreateFactory(Func<IDbConnection> func);

        [TestFixtureTearDown]
        public void TearDown()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        private IPersonRepository CreateRepo(bool newConnectionEveryTime = false, SingletonGetMethodBehavior? getBehavior=null)
        {
            DataLayerBuilder factory;
            if (newConnectionEveryTime)
            {
                Func<IDbConnection> func = () => CreateConnection(open: false);
                factory = CreateFactory(func);
            }
            else
            {
                factory = CreateFactory(_connection);
            }

            var builder = factory.SetupRepo<Person, IPersonRepository>();
            if (getBehavior.HasValue)
            {
                builder.SetupMethod("GetSingletonByName").SetSingletonGetMethodBehavior(getBehavior.Value);
                builder.SetupMethod("TryGetByName").SetSingletonGetMethodBehavior(getBehavior.Value);
            }
            var repo = builder.CreateRepo();

            if (repo.TableExists())
            {
                repo.DropTable();
            }

            return repo;
        }

        private IPersonRepository CreateRepoWithJimAndSusan(bool newConnectionEveryTime = false, SingletonGetMethodBehavior? getMethodBehavior=null)
        {
            var repo = CreateRepo(newConnectionEveryTime, getMethodBehavior);
            repo.CreateTable();

            Person jim = new Person { PersonId = 1, Name = "Jim", Birthday = new DateTime(1969, 3, 9) };
            repo.Insert(jim);

            Person susan = new Person { PersonId = 2, Name = "Susan", Birthday = new DateTime(1971, 12, 13) };
            repo.Insert(susan);

            return repo;
        }

        private IPersonRepositoryWithCreate CreateRepoWithCreateMethod()
        {
            var repo = CreateFactory(_connection)
                .SetupRepo<Person, IPersonRepositoryWithCreate>()
                .CreateRepo();

            if (repo.TableExists())
            {
                repo.DropTable();
            }
            repo.CreateTable();

            return repo;
        }
    }
}
