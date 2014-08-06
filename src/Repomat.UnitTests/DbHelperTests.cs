using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class DbHelperTests
    {
        [TearDown]
        public void TearDown()
        {
            if (Db.Sqlite.TableExists("foobar"))
            {
                Db.Sqlite.ExecuteNonQuery("drop table foobar");
            }
        }

        [Test]
        public void TableExistsTests()
        {
            Assert.IsFalse(Db.Sqlite.TableExists("foobar"));
            Db.Sqlite.ExecuteNonQuery("create table foobar (i int)");
            Assert.IsTrue(Db.Sqlite.TableExists("foobar"));
        }

        [Test]
        public void QueryWithParameters()
        {
            Db.Sqlite.ExecuteNonQuery("create table parmTest (name varchar(100), age int)");
            Db.Sqlite.ExecuteNonQuery("insert into parmTest values (@name, @age)", new { name = "Jim", age = 45 });
            Assert.AreEqual("Jim", Db.Sqlite.ExecuteScalar<string>("select name from parmTest where age = @age", new { age = 45 }));
            Assert.AreEqual(45, Db.Sqlite.ExecuteScalar<int>("select age from parmTest where name = @name", new { name = "Jim" }));

        }

    }
}
