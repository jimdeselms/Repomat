using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Repomat.Databases;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class InMemoryDatabaseTests
    {
        [Test]
        public void Create_CreateTwoFactories_EachFactoryHasSeparateInMemoryDatabase()
        {
            var repo1 = DataLayerBuilder.DefineInMemoryDatabase()
                .SetupRepo<IPersonRepository>()
                .CreateRepo();
            
            repo1.CreateTable();

            var repo2 = DataLayerBuilder.DefineInMemoryDatabase()
                .SetupRepo<IPersonRepository>()
                .CreateRepo();

            // The second repository won't know about the table.
            Assert.IsFalse(repo2.TableExists());

            // But the first still will.
        }
    }
}

