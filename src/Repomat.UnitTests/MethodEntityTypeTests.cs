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

        public interface IOnlyVoidMethods
        {
            void CreateTable();
            void DropTable();
            bool TableExists();

            int GetCountFromPerson();
        }
    }
}
