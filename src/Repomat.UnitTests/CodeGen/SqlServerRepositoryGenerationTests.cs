using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Databases;
using Repomat.CodeGen;
using NUnit.Framework;

namespace Repomat.UnitTests.CodeGen
{
    /// <summary>
    /// How to create the database for these tests
    /// 1) Create a database called "scratch" on the local machine's server
    /// 2) Create a user called "Repomat_test" on the database instance
    /// 3) Map the user to dbo.
    /// </summary>
    [TestFixture]
    public class SqlServerRepositoryGenerationTests : RepositoryGenerationTestBase
    {
        protected override IDbConnection CreateConnection(bool open=true)
        {
            return Connections.NewSqlConnection(open);
        }

        protected override DataLayerBuilder CreateFactory(Func<IDbConnection> func)
        {
            return new SqlServerDataLayerBuilder(func).UseIlGeneration();
        }

    }
}
