using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Repomat.UnitTests
{
    [TestFixture]
    class MethodTypeOverrideTests
    {
        [Test]
        public void CreateDropTable()
        {
            var repo = CreateRepo();

            if (repo.DoesTheTableExist())
            {
                repo.GetRidOfTheTable();
            }

            repo.CauseTheTableToBeCreated();
            Assert.IsTrue(repo.DoesTheTableExist());

            repo.GetRidOfTheTable();

            Assert.IsFalse(repo.DoesTheTableExist());
        }

        [Test]
        public void CruDMethods()
        {
            var repo = CreateRepo();

            Person p1 = new Person {PersonId = 1, Birthday = new DateTime(2000, 1, 1), Name = "Jim"};
            repo.AddAPersonToTheDatabase(p1);

            Person result;
            Assert.IsTrue(repo.TryToSearchForThePerson(1, out result));
            Assert.AreEqual("Jim", result.Name);

            p1.Name = "Joey";
            repo.ChangeThePerson(p1);

            Assert.IsTrue(repo.TryToSearchForThePerson(1, out result));
            Assert.AreEqual("Joey", result.Name);

            repo.RemoveThePerson(p1);
            Assert.IsFalse(repo.TryToSearchForThePerson(1, out p1));
        }
        
        private INonStandardMethods CreateRepo()
        {
            var dlBuilder = DataLayerBuilder.DefineInMemoryDatabase();
            var repoBuilder = dlBuilder.SetupRepo<INonStandardMethods>();

            repoBuilder.SetupMethod("CauseTheTableToBeCreated")
                .SetMethodType(MethodType.CreateTable);

            repoBuilder.SetupMethod("DoesTheTableExist")
                .SetMethodType(MethodType.TableExists);

            repoBuilder.SetupMethod("GetRidOfTheTable")
                .SetMethodType(MethodType.DropTable);

            repoBuilder.SetupMethod("TryToSearchForThePerson")
                .SetMethodType(MethodType.Get);

            repoBuilder.SetupMethod("AddAPersonToTheDatabase")
                .SetMethodType(MethodType.Insert);

            repoBuilder.SetupMethod("RemoveThePerson")
                .SetMethodType(MethodType.Delete);

            repoBuilder.SetupMethod("ChangeThePerson")
                .SetMethodType(MethodType.Update);

            // TODO: This is lame; we shouldn't have to explicitly override the
            // primary key just because there isn't a singleton "Get" method.
            repoBuilder.SetupEntity<Person>()
                .HasPrimaryKey("PersonId");

            var repo = repoBuilder.Repo;

            if (repo.DoesTheTableExist())
            {
                repo.GetRidOfTheTable();
            }
            repo.CauseTheTableToBeCreated();

            return repo;
        }
    }

    public interface INonStandardMethods
    {
        void CauseTheTableToBeCreated();
        bool DoesTheTableExist();
        void GetRidOfTheTable();

        bool TryToSearchForThePerson(int personId, out Person p);
        void AddAPersonToTheDatabase(Person p);
        void RemoveThePerson(Person p);
        void ChangeThePerson(Person p);
    }
}
