using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class MethodEntityTypeTests
    {
        [Test]
        public void OnlyFunctionsWithoutEntityType()
        {
            // If an interface only has standard void functions, then you explicitly have to specify the type.
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewSqlConnection());
            var repoBuilder = dlBuilder.SetupRepo<IOnlyVoidMethods>();
            repoBuilder.SetupMethod("CreateTable").SetEntityType(typeof(Person));
            repoBuilder.SetupMethod("DropTable").SetEntityType(typeof(Person));
            repoBuilder.SetupMethod("TableExists").SetEntityType(typeof(Person));
            repoBuilder.SetupMethod("GetCountFromPerson").ExecutesSql("select count(*) from person");

            var repo = dlBuilder.CreateRepo<IOnlyVoidMethods>();

            if (repo.TableExists())
            {
                repo.DropTable();
            }
            repo.CreateTable();

            Assert.AreEqual(0, repo.GetCountFromPerson());

            repo.DropTable();
        }

        [Test]
        public void InterfaceThatHasTwoEntityTypes()
        {
            // If an interface only has standard void functions, then you explicitly have to specify the type.
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewSqlConnection());
            var repoBuilder = dlBuilder.SetupRepo<ITwoEntities>();
            repoBuilder.SetupMethod("CreateTablePerson").SetEntityType(typeof(Person));
            repoBuilder.SetupMethod("DropTablePerson").SetEntityType(typeof(Person));
            repoBuilder.SetupMethod("TableExistsPerson").SetEntityType(typeof(Person));
            repoBuilder.SetupMethod("GetCountFromPerson").SetEntityType(typeof(Person));
            repoBuilder.SetupMethod("CreateTableFoo").SetEntityType(typeof(Foo));
            repoBuilder.SetupMethod("DropTableFoo").SetEntityType(typeof(Foo));
            repoBuilder.SetupMethod("TableExistsFoo").SetEntityType(typeof(Foo));
            repoBuilder.SetupMethod("GetCountFromFoo").SetEntityType(typeof(Foo));

            var repo = dlBuilder.CreateRepo<ITwoEntities>();

            if (repo.TableExistsPerson())
            {
                repo.DropTablePerson();
            }
            repo.CreateTablePerson();

            if (repo.TableExistsFoo())
            {
                repo.DropTableFoo();
            }
            repo.CreateTableFoo();

            Foo f = new Foo { Bar = "blah" };
            repo.Insert(f);

            Assert.AreEqual(0, repo.GetCountFromPerson());
            Assert.AreEqual(1, repo.GetCountFromFoo());

            repo.DropTableFoo();
            repo.DropTablePerson();
        }

        public interface IOnlyVoidMethods
        {
            void CreateTable();
            void DropTable();
            bool TableExists();

            int GetCountFromPerson();
        }

        public interface ITwoEntities
        {
            void CreateTablePerson();
            void DropTablePerson();
            bool TableExistsPerson();

            void CreateTableFoo();
            void DropTableFoo();
            bool TableExistsFoo();

            void Insert(Person p);
            void Insert(Foo f);

            int GetCountFromPerson();
            int GetCountFromFoo();
        }

        public class Foo
        {
            public string Bar { get; set; }
        }
    }
}
