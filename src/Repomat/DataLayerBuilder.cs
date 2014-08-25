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
            return type.CreateDataLayerBuilder(conn);
        }

        public static DataLayerBuilder DefineSqlDatabase(Func<IDbConnection> conn, DatabaseType type)
        {
            return type.CreateDataLayerBuilder(conn);
        }

        public static DataLayerBuilder DefineInMemoryDatabase()
        {
            return InMemoryDatabase.Create();
        }

        public DatabaseType DatabaseType { get { return _databaseType; } }

        private NamingConvention _tableNamingConvention;
        private NamingConvention _columnNamingConvention;

        // This is where all of the repository definitions live
        private readonly Dictionary<Type, RepositoryDef> _repoDefs = new Dictionary<Type, RepositoryDef>();

        // This is where all of the generated repositories are cached.
        private readonly Dictionary<Type, object> _repoInstances = new Dictionary<Type, object>();

        private readonly DatabaseType _databaseType;

        protected DataLayerBuilder(DatabaseType databaseType)
        {
            _tableNamingConvention = NamingConvention.NoOp;
            _columnNamingConvention = NamingConvention.NoOp;
            _databaseType = databaseType;
        }

        internal void CreateReposFromTableDefs(IEnumerable<RepositoryDef> repoDefs)
        {
            // Build up a list of all of the source code for all of the repositories.
            var codeAndClassNames = new List<dynamic>();

            foreach (var repoDef in repoDefs)
            {
                string className;
                string classCode = GenerateClassCode(repoDef, out className);

                codeAndClassNames.Add(new { RepoDef = repoDef, ClassName = className, ClassCode = classCode });
            }

            CSharpCodeProvider p = new CSharpCodeProvider();
            CompilerParameters parms = new CompilerParameters();

            var assemblies = GetDistinctAssembliesFromRepoDefs(codeAndClassNames.Select(x => (RepositoryDef)x.RepoDef));

            foreach (var assembly in assemblies)
            {
                parms.ReferencedAssemblies.Add(Path.GetFileName(assembly.CodeBase));
            }
            parms.ReferencedAssemblies.Add(Path.GetFileName(typeof(DataLayerBuilder).Assembly.CodeBase));
            parms.ReferencedAssemblies.Add("System.Core.dll");
            parms.ReferencedAssemblies.Add("System.Data.dll");
            parms.ReferencedAssemblies.Add("System.dll");
            parms.ReferencedAssemblies.Add("System.Data.SQLite.dll");

            parms.GenerateInMemory = true;

            // Compile all of the waiting repositories in one shot; this is way faster
            // then doing them one at a time.
            var result = p.CompileAssemblyFromSource(parms, codeAndClassNames.Select(x => (string)x.ClassCode).ToArray());
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

            foreach (var entry in codeAndClassNames)
            {
                var repoType = entry.RepoDef.RepositoryType;
                var generatedType = asm.GetType(entry.ClassName);
                _repoInstances[repoType] = CreateRepoInstance(generatedType, _repoDefs[repoType]);
            }
        }

        private IEnumerable<Assembly> GetDistinctAssembliesFromRepoDefs(IEnumerable<RepositoryDef> repoDefs)
        {
            HashSet<Assembly> asms = new HashSet<Assembly>();
            foreach (var repoDef in repoDefs)
            {
                foreach (var method in repoDef.Methods)
                {
                    if (method.EntityDef != null)
                    {
                        asms.Add(method.EntityDef.Type.Assembly);
                    }
                }
                asms.Add(repoDef.RepositoryType.Assembly);
            }

            return asms;
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

        public TRepo CreateRepo<TRepo>()
        {
            var repoDef = _repoDefs[typeof(TRepo)];

            object repo;
            if (_repoInstances.TryGetValue(typeof(TRepo), out repo))
            {
                return (TRepo)repo;
            }
            else
            {
                EnsureRepoIsValid(repoDef);

                var reposThatNeedToBeBuilt = _repoDefs.Values.Where(rd => !_repoInstances.Keys.Contains(rd.RepositoryType));

                CreateReposFromTableDefs(reposThatNeedToBeBuilt);

                return (TRepo)_repoInstances[typeof(TRepo)];
            }
        }

        public TRepo CreateIlRepo<TRepo>()
        {
            var repoDef = _repoDefs[typeof(TRepo)];
            RepoSqlBuilder builder = new RepoSqlBuilder(repoDef, false, RepoConnectionType.SingleConnection);

            var type = builder.CreateType();

            return (TRepo)CreateRepoInstance(typeof(TRepo), repoDef);
        }

        private static MethodInfo _createClassBuilder = typeof(DataLayerBuilder).GetMethod("CreateClassBuilder", BindingFlags.NonPublic | BindingFlags.Instance);

        private string GenerateClassCode(RepositoryDef tableDef, out string className)
        {
            var classBuilder = CreateClassBuilder(tableDef);

            className = classBuilder.ClassName;
            return classBuilder.GenerateClassDefinition();
        }

        // Internal so that I don't have to expose it to the outside by making it protected.
        internal abstract RepositoryClassBuilder CreateClassBuilder(RepositoryDef tableDef);

        // internal because protected will expose it to the outside.
        internal abstract object CreateRepoInstance(Type repoClass, RepositoryDef tableDef);

        private void AddReferenceToTypeAssembly(Type type, CompilerParameters parms)
        {
            parms.ReferencedAssemblies.Add(Path.GetFileName(type.Assembly.CodeBase));
        }

        // This is only here for unit testing.
        internal object[] __GetRepositoryInstances()
        {
            return _repoInstances.Values.ToArray();
        }
    }
}
