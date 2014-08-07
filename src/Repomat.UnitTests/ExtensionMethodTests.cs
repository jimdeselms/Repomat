using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class ExtensionMethodTests
    {
        [Test]
        public void Capitalize_CapitalizesOnlyFirstLetter()
        {
            Assert.AreEqual("Hello", "hello".Capitalize());
        }

        [Test]
        public void Capitalize_EmptyString_ReturnsEmptyString()
        {
            Assert.AreEqual("", "".Capitalize());
        }

        [Test]
        public void Uncapitalize_LowersOnlyFirstLetter()
        {
            Assert.AreEqual("hELLO", "HELLO".Uncapitalize());
        }

        [Test]
        public void Uncapitalize_EmptyString_ReturnsEmptyString()
        {
            Assert.AreEqual("", "".Uncapitalize());
        }

        [Test]
        public void ToCSharp_SimpleType()
        {
            Assert.AreEqual("Repomat.UnitTests.ExtensionMethodTests", typeof(ExtensionMethodTests).ToCSharp());
        }

        [Test]
        public void ToCSharp_PrivateInnerClass()
        {
            Assert.AreEqual("Repomat.UnitTests.ExtensionMethodTests.PrivateInnerClass", typeof(PrivateInnerClass).ToCSharp());
        }

        [Test]
        public void ToCSharp_PublicInnerClass()
        {
            Assert.AreEqual("Repomat.UnitTests.ExtensionMethodTests.PublicInnerClass", typeof(PublicInnerClass).ToCSharp());
        }

        [Test]
        public void ToCSharp_GenericTypeInnerClass()
        {
            Assert.AreEqual("Repomat.UnitTests.ExtensionMethodTests.Foo<Repomat.UnitTests.ExtensionMethodTests.PublicInnerClass>", typeof(Foo<PublicInnerClass>).ToCSharp());
        }

        [Test]
        public void ToCSharp_Array()
        {
            Assert.AreEqual("string[]", typeof(string[]).ToCSharp());
        }

        [Test]
        public void ToCSharp_CrazyArray()
        {
            Assert.AreEqual("string[][,,][]", typeof(string[][,,][]).ToCSharp());
        }

        [Test]
        public void ToCSharp_GenericClass()
        {
            Assert.AreEqual("System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>", typeof(Dictionary<string, List<int>>).ToCSharp());
        }

        [Test]
        public void GetCoreType_NotEnumerable_JustGetType()
        {
            Assert.AreEqual(typeof(int), typeof(int).GetCoreType());
        }

        [Test]
        public void GetCoreType_Array_GetElementType()
        {
            Assert.AreEqual(typeof(int), typeof(int[]).GetCoreType());
        }

        [Test]
        public void GetCoreType_List_GetGenericParameter()
        {
            Assert.AreEqual(typeof(int), typeof(List<int>).GetCoreType());
        }

        [Test]
        public void IsEnumerableOfType_ListOfSameType_True()
        {
            Assert.IsTrue(typeof(List<int>).ImplementsIEnumerableOfType(typeof(int)));
        }

        [Test]
        public void IsEnumerableOfType_ListOfDifferentType_False()
        {
            Assert.IsFalse(typeof(List<int>).ImplementsIEnumerableOfType(typeof(char)));
        }

        [Test]
        public void IsEnumerableOfType_ArrayOfSameType_True()
        {
            Assert.IsTrue(typeof(List<char>).ImplementsIEnumerableOfType(typeof(char)));
        }

        [Test]
        public void IsEnumerableOfType_ArrayOfDifferentType_False()
        {
            Assert.IsFalse(typeof(List<string>).ImplementsIEnumerableOfType(typeof(bool)));
        }

        private class PrivateInnerClass
        {
        }

        public class PublicInnerClass
        {
        }

        public class Foo<T>
        {
        }
    }
}
