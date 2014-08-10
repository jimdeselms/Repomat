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
        public void Validate_SingletonMethodThatReturnsTypeOtherThanDto()
        {
            Validate<Person, IGetReturnsSomethingOtherThanDto>(
                Error("SingleGetReturnWrongType", "expected return type Repomat.UnitTests.Person, returns Repomat.UnitTests.ColorThing instead"));
        }

        [Test]
        public void Validate_MultirowMethodThatReturnsTypeOtherThanDto()
        {
            Validate<Person, IGetReturnsCollectionOtherThanDto>(
                Error("MultiGetReturnWrongType", "expected enumerable return type Repomat.UnitTests.Person, returns System.Collections.Generic.IEnumerable<Repomat.UnitTests.ColorThing> instead"),
                Error("MultiGetReturnWrongType", "expected enumerable return type Repomat.UnitTests.Person, returns System.Collections.Generic.List<Repomat.UnitTests.ColorThing> instead"),
                Error("MultiGetReturnWrongType", "expected enumerable return type Repomat.UnitTests.Person, returns Repomat.UnitTests.ColorThing[] instead"));
        }

        [Test]
        public void Validate_TryGetWithoutBoolReturn()
        {
            Validate<Person, ITryGetWithoutBoolReturn>(
                Error("TryGetReturnWrongType", "expected return type bool, returns string instead"));
        }

        [Test]
        public void Validate_TryGetWithWrongOutType()
        {
            Validate<Person, ITryGetWithWrongOutParam>(
                Error("TryGetOutParamWrongType", "expected out parameter of type Repomat.UnitTests.Person, out parameter of type string instead"));
        }

        [Test]
        public void Validate_TryGetWithoutOutParam()
        {
            Validate<Person, ITryGetWithoutOutParam>(
                Error("TryGetNoOut", "missing out parameter of type Repomat.UnitTests.Person"));
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
            var dlBuilder = DataLayerBuilder.DefineSqlDatabase(Connections.NewInMemoryConnection());
            var repoBuilder = dlBuilder.SetupRepo<Person, IProcRepo>();
            repoBuilder.SetupMethod("Foo").ExecutesStoredProcedure();

            try 
            {
                dlBuilder.CreateRepo<IProcRepo>();
                Assert.Fail();
            }
            catch (RepomatException e) 
            {
                StringAssert.Contains("Database type SQLite does not support stored procedures", e.Message);
            }

        }

        private interface IGetReturnsSomethingOtherThanDto
        {
            ColorThing Get(int personId);
        }

        private interface IGetReturnsCollectionOtherThanDto
        {
            IEnumerable<ColorThing> GetByPersonIdReturnsEnumerable(int personId);
            List<ColorThing> GetByPersonIdReturnsList(int personId);
            ColorThing[] GetByPersonIdReturnsArray(int personId);
        }

        private interface ITryGetWithoutBoolReturn
        {
            string TryGet(int personId, out Person p);
        }

        private interface ITryGetWithWrongOutParam
        {
            bool TryGet(int personId, out string s);
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
            var repoDef = RepositoryDefBuilder.BuildRepositoryDef<TType, TRepo>(NamingConvention.NoOp, NamingConvention.NoOp);
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
