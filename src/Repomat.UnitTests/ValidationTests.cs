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
            Validate<Person, IPersonRepository>();
        }

        [Test]
        public void Validate_TryGetWithoutBoolReturn()
        {
            Validate<Person, ITryGetWithoutBoolReturn>(
                Error("MultiGetReturningWrongType", "expected enumerable return type Repomat.UnitTests.Person, returns string instead"),
                Error("TryGetReturnWrongType", "expected return type bool, returns string instead"));
        }

        [Test]
        public void Validate_GetArgumentsDontMapToProperties()
        {
            Validate<Person, IGetWithParameterThatDoesntMap>(
                Error("ParameterDoesntHaveProperty", "found parameter badParam that does not map to a settable property BadParam"));
        }

        [Test]
        public void Validate_GetArgumentsParameterTypeDoesntMatchProperty()
        {
            Validate<Person, IGetWithParameterOfDifferentType>(
                Error("ParameterDoesntHaveProperty", "parameter birthday is not the same type as property Birthday. It must be System.DateTime"));
        }

        [Test]
        public void Validate_CreateAndInsertMutuallyExclusive()
        {
            Validate<Person, IRepoWithCreateAndInsert>(
                Error("BothCreateAndInsert", "Create and Insert methods are mutually exclusive. Please choose one"));
        }

        [Test]
        public void Validate_CustomMethodDoesntHaveSqlDefined()
        {
            Validate<Person, ICustomMethodWithoutCustomSql>(
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

        #region Helpers

        private void Validate<TType, TRepo>(params ValidationError[] expectedErrors)
        {
            var repoDef = RepositoryDefBuilder.BuildRepositoryDef<TRepo>(NamingConvention.NoOp, NamingConvention.NoOp);
            RepositoryDefValidator v = new RepositoryDefValidator(DatabaseType.SqlServer);
            var errors = v.Validate(repoDef);

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
