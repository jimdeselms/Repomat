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
using System.Data.SQLite;
using Repomat.Schema.Validators;
using Repomat.Databases;

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
            else if (conn is SQLiteConnection)
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
            switch (type)
            {
                case DatabaseType.SqlServer: return new SqlServerDataLayerBuilder(conn);
                case DatabaseType.SQLite: return new SQLiteDataLayerBuilder(conn);
                default: throw new RepomatException("DatabaseType {0} is not a SQL database", type);
            }
        }

        public static DataLayerBuilder DefineSqlDatabase(Func<IDbConnection> conn, DatabaseType type)
        {
            switch (type)
            {
                case DatabaseType.SqlServer: return new SqlServerDataLayerBuilder(conn);
                case DatabaseType.SQLite: return new SQLiteDataLayerBuilder(conn);
                default: throw new RepomatException("DatabaseType {0} is not a SQL database", type);
            }
        }

        public static DataLayerBuilder DefineInMemoryDatabase()
        {
            return InMemoryDatabase.Create();
        }

        private NamingConvention _tableNamingConvention;
        private NamingConvention _columnNamingConvention;

        private readonly Dictionary<Type, RepositoryDef> _repoDefs = new Dictionary<Type, RepositoryDef>();

        protected DataLayerBuilder()
        {
            _tableNamingConvention = NamingConvention.NoOp;
            _columnNamingConvention = NamingConvention.NoOp;
        }

        internal TRepo CreateRepoFromTableDef<TRepo>(RepositoryDef repoDef)
        {
            EnsureRepoIsValid(repoDef);

            string className;
            string classCode = GenerateClassCode<TRepo>(repoDef, out className);

            CSharpCodeProvider p = new CSharpCodeProvider();
            CompilerParameters parms = new CompilerParameters();
            AddReferenceToTypeAssembly(typeof(DataLayerBuilder), parms);
            AddReferenceToTypeAssembly(repoDef.EntityType, parms);
            AddReferenceToTypeAssembly(typeof(TRepo), parms);
            parms.ReferencedAssemblies.Add("System.Core.dll");
            parms.ReferencedAssemblies.Add("System.Data.dll");
            parms.ReferencedAssemblies.Add("System.dll");
            parms.ReferencedAssemblies.Add("System.Data.SQLite.dll");

            parms.GenerateInMemory = true;

            var result = p.CompileAssemblyFromSource(parms, new[] { classCode });
            if (result.Errors.HasErrors)
            {
                StringBuilder errorList = new StringBuilder();
                foreach (var error in result.Errors)
                {
                    errorList.AppendLine(error.ToString());
                }
                throw new RepomatException("Compilation Errors:\n" + string.Join("\n", errorList));
            }
            var asm = result.CompiledAssembly;
            var generatedType = asm.GetType(className);

            return CreateRepoInstance<TRepo>(generatedType, repoDef);
        }

        private void EnsureRepoIsValid(RepositoryDef repoDef)
        {
            var validator = new RepositoryDefValidator();
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

        public RepositoryBuilder<TType, TRepo> SetupRepo<TType, TRepo>()
        {
            RepositoryDef tableDef = RepositoryDefBuilder.BuildRepositoryDef<TType, TRepo>(_tableNamingConvention, _columnNamingConvention);

            _repoDefs.Add(typeof(TRepo), tableDef);

            return new RepositoryBuilder<TType, TRepo>(this, tableDef);
        }

        public TRepo CreateRepo<TRepo>()
        {
            var tableDef = _repoDefs[typeof(TRepo)];
            return CreateRepoFromTableDef<TRepo>(tableDef);
        }

        private static MethodInfo _createClassBuilder = typeof(DataLayerBuilder).GetMethod("CreateClassBuilder", BindingFlags.NonPublic | BindingFlags.Instance);

        private string GenerateClassCode<TRepo>(RepositoryDef tableDef, out string className)
        {
            var classBuilder = (RepositoryClassBuilder<TRepo>)_createClassBuilder.MakeGenericMethod(tableDef.EntityType, typeof(TRepo)).Invoke(this, new[] { tableDef });

            className = classBuilder.ClassName;
            return classBuilder.GenerateClassDefinition();
        }

        // Internal so that I don't have to expose it to the outside by making it protected.
        internal abstract RepositoryClassBuilder<TRepo> CreateClassBuilder<TType, TRepo>(RepositoryDef tableDef);

        // internal because protected will expose it to the outside.
        internal abstract TRepo CreateRepoInstance<TRepo>(Type repoClass, RepositoryDef tableDef);

        private void AddReferenceToTypeAssembly(Type type, CompilerParameters parms)
        {
            parms.ReferencedAssemblies.Add(System.IO.Path.GetFileName(type.Assembly.CodeBase));
        }

    }
}
