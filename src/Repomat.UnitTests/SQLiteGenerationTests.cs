using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using NUnit.Framework;
using Repomat.Databases;
using Repomat.CodeGen;
using System.IO;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class SQLiteGenerationTests : RepositoryGenerationTestBase
    {
        protected override System.Data.IDbConnection CreateConnection(bool open = true)
        {
            return Connections.NewSQLiteConnection(open);
        }

        protected override DataLayerBuilder CreateFactory(Func<System.Data.IDbConnection> func)
        {
            return DataLayerBuilder.DefineSqlDatabase(func, DatabaseType.SQLite);
        }
    }
}
