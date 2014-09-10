using NUnit.Framework;
using Repomat.Schema;
using Repomat.Schema.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class ValidationTests
    {
        [Test]
        public void Validate_NoErrors()
        {
            Validate<IPersonRepository>();
        }

        [Test]
        public void Validate_TryGetWithoutBoolReturn()
        {
            Validate<ITryGetWithoutBoolReturn>(
                Error("MultiGetReturningWrongType", "expected enumerable return type Repomat.UnitTests.Person, returns string instead"),
                Error("TryGetReturnWrongType", "expected return type bool, returns string instead"));
        }

        [Test]
        public void Validate_GetArgumentsDontMapToProperties()
        {
            Validate<IGetWithParameterThatDoesntMap>(
                Error("ParameterDoesntHaveProperty", "found parameter badParam that does not map to a settable property BadParam"));
        }

        [Test]
        public void Validate_GetArgumentsParameterTypeDoesntMatchProperty()
        {
            Validate<IGetWithParameterOfDifferentType>(
                Error("ParameterDoesntHaveProperty", "parameter birthday is not the same type as property Birthday. It must be System.DateTime"));
        }

        [Test]
        public void Validate_CreateAndInsertMutuallyExclusive()
        {
            Validate<IRepoWithCreateAndInsert>(
                Error("BothCreateAndInsert", "Create and Insert methods are mutually exclusive. Please choose one"));
        }

        [Test]
        public void Validate_CustomMethodDoesntHaveSqlDefined()
        {
            Validate<ICustomMethodWithoutCustomSql>(
                Error("CustomMethodWithoutSql", "Method looks custom, but does not have SQL defined. Call SetCustomSql() to define the SQL"));
        }

        [Test]
        public void Validate_StoredProcInDbThatDoesntSupportIt()
        {

            try 
            {
                var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
                var repoBuilder = dlBuilder.SetupRepo<IProcRepo>();
                repoBuilder.SetupMethod("Foo").ExecutesStoredProcedure();

                dlBuilder.CreateRepo<IProcRepo>();
                Assert.Fail();
            }
            catch (RepomatException e) 
            {
                StringAssert.Contains("Stored procedures not supported in SQLite", e.Message);
            }
        }

        [Test]
        public void Validate_MoreThanOneSingletonGetMethod_Fail()
        {
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
            var repoBuilder = dlBuilder.SetupRepo<ITwoGets>();

            try
            {
                var ignored = repoBuilder.Repo;
                Assert.Fail();
            }
            catch (RepomatException e)
            {
                StringAssert.Contains("Entity with more than one singleton get. call UsesPrimaryKey() to define primary key.", e.Message);
            }
        }

        [Test]
        public void Validate_MoreThanOneSingleGetButExplicitPrimaryKey_Success()
        {
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
            var repoBuilder = dlBuilder.SetupRepo<ITwoGets>();
            repoBuilder
                .SetupEntity<Person>()
                .HasPrimaryKey("PersonId");

            var ignored = repoBuilder.Repo;
        }

        [Test]
        public void Validate_MoreThanOneSingleGetButSameType_Success()
        {
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
            var repoBuilder = dlBuilder.SetupRepo<ITwoGets>();
            repoBuilder
                .SetupEntity<Person>()
                .HasPrimaryKey("PersonId");

            var ignored = repoBuilder.Repo;
        }

        private interface ITryGetWithoutBoolReturn
        {
            string TryGet(int personId, out Person p);
        }

        private interface ITryGetWithoutOutParam
        {
            bool TryGet(int personId, Person p);
        }

        private interface IGetWithParameterThatDoesntMap
        {
            Person Get(int personId, string badParam);
        }

        private interface IGetWithParameterOfDifferentType
        {
            Person Get(int personId, string birthday);
        }

        public interface IProcRepo
        {
            void Foo();
        }

        private interface IRepoWithCreateAndInsert
        {
            void Create(Person p);
            void Insert(Person p);
        }

        private interface ICustomMethodWithoutCustomSql
        {
            void DoSomethingSpecial();
        }

        public interface ITwoGets
        {
            Person GetById(int personId);
            Person GetByName(string name);
        }

        public interface ITwoGetsSameTime
        {
            Person GetById(int personId);
            Person GetById2(int personId);
        }

        #region Helpers

        private void Validate<TRepo>(params ValidationError[] expectedErrors)
        {
            var repoDef = RepositoryDefBuilder.BuildRepositoryDef<TRepo>(NamingConvention.NoOp, NamingConvention.NoOp);
            RepositoryDefValidator v = new RepositoryDefValidator(repoDef, DatabaseType.SqlServer);
            var errors = v.Validate().ToList();

            Assert.AreEqual(expectedErrors.Length, errors.Count);
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                StringAssert.EndsWith(expectedErrors[i].Message, errors[i].Message);
            }
        }

        private ValidationError Error(string code, string message)
        {
            return new ValidationError(code, message);
        }

#endregion
    }
}
