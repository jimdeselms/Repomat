using Repomat.CodeGen;
using Repomat.DatabaseTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat
{
    public abstract class DatabaseType
    {
        public static readonly DatabaseType SqlServer = new SqlServerDatabaseType();
        public static readonly DatabaseType SQLite = new SQLiteDatabaseType();

        private readonly bool _supportsStoredProcedures;
        private readonly string _name;

        public bool SupportsStoredProcedures { get { return _supportsStoredProcedures; } }
        public string Name { get { return _name; } }

        public abstract DataLayerBuilder CreateDataLayerBuilder(IDbConnection conn);
        public abstract DataLayerBuilder CreateDataLayerBuilder(Func<IDbConnection> conn);

        protected DatabaseType(string name, bool supportsStoredProcedures)
        {
            _name = name;
            _supportsStoredProcedures = supportsStoredProcedures;
        }
    }
}
