using Repomat.Schema;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class RepositoryDefBuilderTests
    {
        [Test]
        public void GetAssignableColumnsForType_TwoCtors_UseLongerCtor()
        {
            var cols = RepositoryDefBuilder.GetAssignableColumnsForType(NamingConvention.NoOp, typeof(ClassWithTwoCtors)).ToArray();
            Assert.AreEqual(2, cols.Length);
            Assert.AreEqual("Id", cols[0].PropertyName);
            Assert.AreEqual("Name", cols[1].PropertyName);
        }

        [Test]
        public void Foo_InterfaceWithTwoGets_LongerGetDeterminesPrimaryKey()
        {
            var def = RepositoryDefBuilder.BuildRepositoryDef<MyFoo, IMyFoos>(NamingConvention.NoOp, NamingConvention.NoOp);
            Assert.AreEqual(2, def.PrimaryKey.Count);
            Assert.AreEqual("Id", def.PrimaryKey[0].PropertyName);
            Assert.AreEqual("Name", def.PrimaryKey[1].PropertyName);
        }

        [Test]
        public void Foo_InterfaceWithTwoGetsWhereLongerIsNotSingleton_UseTheSingletonMethod()
        {
            var def = RepositoryDefBuilder.BuildRepositoryDef<MyFoo, ITwoGetsLongerIsNotSingleton>(NamingConvention.NoOp, NamingConvention.NoOp);
            Assert.AreEqual(1, def.PrimaryKey.Count);
            Assert.AreEqual("Id", def.PrimaryKey[0].PropertyName);
        }

        private class ClassWithTwoCtors
        {
            public ClassWithTwoCtors(int id)
            {
            }

            public ClassWithTwoCtors(int id, string name)
            {
            }

            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class MyFoo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Foo { get; set; }
        }

        private interface IMyFoos
        {
            MyFoo Get(int id);
            MyFoo Get(int id, string name);
        }

        private interface ITwoGetsLongerIsNotSingleton
        {
            MyFoo Get(int id);
            IEnumerable<MyFoo> Get(int id, string name);
        }
    }
}
