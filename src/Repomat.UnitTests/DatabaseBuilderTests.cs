using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Repomat;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class DatabaseBuilderTests
    {
        [Test]
        public void ChangeColumnName_NameIsDifferentInDatabase()
        {
            using (var conn = Connections.NewInMemoryConnection())
            {
                var factory = DataLayerBuilder.DefineSqlDatabase(conn);
                var builder = factory.SetupRepo<IPersonRepository>();
                builder.SetupEntity<Person>().SetupProperty("Name").SetColumnName("person_name");
                builder.SetupEntity<Person>().SetupProperty("PersonId").SetColumnName("id");
                var repo = builder.Repo;

                var entityDef = builder.RepoDef.Methods.First().EntityDef;

                Assert.AreEqual("person_name", entityDef.Properties.First(c => c.PropertyName == "Name").ColumnName);
                Assert.AreEqual("id", entityDef.Properties.First(c => c.PropertyName == "PersonId").ColumnName);
                Assert.AreEqual("id", entityDef.PrimaryKey.First(c => c.PropertyName == "PersonId").ColumnName);

                RunAllCommandsAgainstRepo(repo);
            }
        }

        [Test]
        public void NamingConvention_NamesIsDifferentInDatabase()
        {
            using (var conn = Connections.NewInMemoryConnection())
            {
                var tableConvention = NamingConvention.NoOp
                    .AddOverride("Person", "person_table");

                var columnConvention = NamingConvention.UppercaseWords
                    .AddOverride("Name", "NAME_COLUMN");

                var factory = DataLayerBuilder.DefineSqlDatabase(conn)
                    .SetColumnNamingConvention(columnConvention)
                    .SetTableNamingConvention(tableConvention);
                var builder = factory.SetupRepo<IPersonRepository>();
                var repo = builder.Repo;

                var entityDef = builder.RepoDef.Methods.First().EntityDef;

                Assert.AreEqual("person_table", entityDef.TableName);
                Assert.AreEqual("NAME_COLUMN", entityDef.Properties.First(c => c.PropertyName == "Name").ColumnName);
                Assert.AreEqual("PERSON_ID", entityDef.Properties.First(c => c.PropertyName == "PersonId").ColumnName);
                Assert.AreEqual("PERSON_ID", entityDef.PrimaryKey.First(c => c.PropertyName == "PersonId").ColumnName);

                RunAllCommandsAgainstRepo(repo);
            }
        }

        /// <summary>
        /// Go through all the commands to make sure that the correct text is being emitted for each command
        /// </summary>
        /// <param name="repo"></param>
        private void RunAllCommandsAgainstRepo(IPersonRepository repo)
        {
            repo.CreateTable();
            Assert.IsTrue(repo.TableExists());

            Person p = new Person {Birthday = new DateTime(2010, 1, 2), Name = "Fred", PersonId = 123};

            repo.Insert(p);
            p.Birthday = new DateTime(2011, 1, 2);
            repo.Update(p);
            repo.Get(123);
            repo.GetAll();
            repo.GetByName("Fred");
            repo.Delete(p);

            repo.DropTable();

        }
    }
}
