using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    public static class Db
    {
        private static readonly Lazy<DbHelper> _sqlite = new Lazy<DbHelper>(() => new SqliteHelper());
        private static readonly Lazy<DbHelper> _sqlServer = new Lazy<DbHelper>(() => new SqlHelper());

        public static DbHelper Sqlite { get { return _sqlite.Value; } }

        public static DbHelper SqlServer { get { return _sqlServer.Value; } }
    }
}
