using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Repomat.CodeGen;
using Repomat.Schema;
using System.Data.SqlClient;
using Repomat.Schema.Validators;
using Repomat.Databases;
using System.IO;
using Repomat.IlGen;

namespace Repomat
{
    public abstract class DataLayerBuilder
    {
        public static DataLayerBuilder DefineSqlDatabase(IDbConnection conn)
        {
            if (conn is SqlConnection)
            {
                return DefineSqlDatabase(conn, DatabaseType.SqlServer);
            }
            else if (conn.GetType().FullName == "System.Data.SQLite.SQLiteConnection")
            {
                return DefineSqlDatabase(conn, DatabaseType.SQLite);
            }
            else
            {
                throw new RepomatException("Can't determine SQL database type from connection {0}. Pass DatabaseType to DefineSqlDatabase.", conn.GetType().Name);
            }
        }

        public static DataLayerBuilder DefineSqlDatabase(IDbConnection conn, DatabaseType type)
        {
            return type.CreateDataLayerBuilder(conn);
        }

        public static DataLayerBuilder DefineSqlDatabase(Func<IDbConnection> conn, DatabaseType type)
        {
            return type.CreateDataLayerBuilder(conn);
        }

        public DatabaseType DatabaseType { get { return _databaseType; } }

        private NamingConvention _tableNamingConvention;
        private NamingConvention _columnNamingConvention;

        internal abstract bool NewConnectionEveryTime { get; }

        // This is where all of the repository definitions live
        private readonly Dictionary<Type, RepositoryDef> _repoDefs = new Dictionary<Type, RepositoryDef>();

        private readonly DatabaseType _databaseType;

        protected DataLayerBuilder(DatabaseType databaseType)
        {
            _tableNamingConvention = NamingConvention.NoOp;
            _columnNamingConvention = NamingConvention.NoOp;
            _databaseType = databaseType;
        }

        private void EnsureRepoIsValid(RepositoryDef repoDef)
        {
            var validator = new RepositoryDefValidator(_databaseType);
            var errors = validator.Validate(repoDef);

            if (errors.Count != 0)
            {
                string errorMessage = "The repository has some validation errors:\r\n" + string.Join(Environment.NewLine, errors);
                throw new RepomatException(errorMessage);
            }
        }

        public DataLayerBuilder SetColumnNamingConvention(NamingConvention namingConvention)
        {
            _columnNamingConvention = namingConvention;
            return this;
        }

        public DataLayerBuilder SetColumnNamingConvention(Func<string, string> namingConventionFunc)
        {
            return SetColumnNamingConvention(new NamingConvention(namingConventionFunc));
        }

        public DataLayerBuilder SetTableNamingConvention(NamingConvention namingConvention)
        {
            _tableNamingConvention = namingConvention;
            return this;
        }

        public DataLayerBuilder SetTableNamingConvention(Func<string, string> namingConventionFunc)
        {
            return SetTableNamingConvention(new NamingConvention(namingConventionFunc));
        }

        public RepositoryBuilder<TRepo> SetupRepo<TRepo>()
        {
            RepositoryDef tableDef = RepositoryDefBuilder.BuildRepositoryDef<TRepo>(_tableNamingConvention, _columnNamingConvention);

            _repoDefs.Add(typeof(TRepo), tableDef);

            return new RepositoryBuilder<TRepo>(this, tableDef);
        }

        public DataLayerBuilder UseIlGeneration()
        {
            return this;
        }

        public TRepo CreateRepo<TRepo>()
        {
            var repoDef = _repoDefs[typeof(TRepo)];

            EnsureRepoIsValid(repoDef);
            RepoSqlBuilder builder = CreateRepoSqlBuilder(repoDef, NewConnectionEveryTime);

            var type = builder.CreateType();

            return (TRepo)CreateRepoInstance(type, repoDef);
        }

        internal abstract RepoSqlBuilder CreateRepoSqlBuilder(RepositoryDef repoDef, bool newConnectionEveryTime);

        private static MethodInfo _createClassBuilder = typeof(DataLayerBuilder).GetMethod("CreateClassBuilder", BindingFlags.NonPublic | BindingFlags.Instance);

        // internal because protected will expose it to the outside.
        internal abstract object CreateRepoInstance(Type repoClass, RepositoryDef tableDef);

        private void AddReferenceToTypeAssembly(Type type, CompilerParameters parms)
        {
            parms.ReferencedAssemblies.Add(Path.GetFileName(type.Assembly.CodeBase));
        }
    }
}
