using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Repomat.UnitTests
{
    [TestFixture]
    [Ignore]
    public class IlBuilderTests
    {
        [Test]
        public void Ret_CorrectStackSize_Success()
        {
            IlTester tester = IlTester.Create<int>();
            var il = tester.IL;

            il.Ldc(123);
            il.Ret();

            Assert.AreEqual(123, tester.Invoke<int>());
        }

        [Test]
        public void Ret_StackTooBig_Throws()
        {
            IlTester tester = IlTester.Create<int>();
            var il = tester.IL;

            il.Ldc(123);
            il.Ldc(234);

            Assert.Throws<RepomatException>(() => il.Ret());
        }

        [Test]
        public void Ret_StackTooSmall_Throws()
        {
            IlTester tester = IlTester.Create<int>();
            var il = tester.IL;

            Assert.Throws<RepomatException>(() => il.Ret());
        }

        [Test]
        public void Ret_NoArgs_Success()
        {
            IlTester tester = IlTester.CreateVoid();
            var il = tester.IL;

            il.Ret();

            tester.InvokeVoid();
        }

        [Test]
        public void Ret_ReturnTypeRefTypeReturningValueType_BoxResult()
        {
            IlTester tester = IlTester.Create<object>();
            var il = tester.IL;

            il.Ldc(123);
            il.Ret();

            Assert.AreEqual(123, tester.Invoke<object>());
        }

        [Test]
        public void Ret_ReturnTypeValueTypeReturningRefType_UnboxResult()
        {
            IlTester tester = IlTester.Create<int>(typeof(object));
            var il = tester.IL;

            il.Ldarg(0);
            il.Ret();

            Assert.AreEqual(999, tester.Invoke<int>((object)999));
        }

        [Test]
        public void Ret_ReturnedValueNotSubclassOfReturnType_Throw()
        {
            IlTester tester = IlTester.Create<XmlDocument>(typeof(XmlNode));
            var il = tester.IL;

            il.Ldarg(0);

            Assert.Throws<RepomatException>(() => il.Ret());
        }

        [Test]
        public void Ret_ReturnedTypeIsSubclassOfReturnType_Success()
        {
            IlTester tester = IlTester.Create<XmlNode>(typeof(XmlDocument));
            var il = tester.IL;

            il.Ldarg(0);
            il.Ret();

            var doc = new XmlDocument();

            Assert.AreSame(doc, tester.Invoke<XmlNode>(doc));
        }

        [Test]
        public void IfTest()
        {
            IlTester tester = IlTester.Create<int>(typeof(bool));
            var il = tester.IL; 

            il.Ldc(1);

            il.Ldarg(0);
            il.If(() =>
            {
                il.Pop();
                il.Ldc(2);
            });

            il.Ret();

            Assert.AreEqual(1, tester.Invoke<int>(false));
            Assert.AreEqual(2, tester.Invoke<int>(true));
        }

        [Test]
        public void IfWithElseTest()
        {
            IlTester tester = IlTester.Create<int>(typeof(bool));
            var il = tester.IL;

            il.Ldarg(0);
            il.If(() =>
            {
                il.Ldc(1);
            },
            () =>
            {
                il.Ldc(2);
            });

            il.Ret();

            Assert.AreEqual(1, tester.Invoke<int>(true));
            Assert.AreEqual(2, tester.Invoke<int>(false));
        }

        [Test]
        public void If_StackNotSameLength_Throws()
        {
            IlTester tester = IlTester.Create<int>();
            var il = tester.IL;

            il.Ldc(0);
            Assert.Throws<RepomatException>(() => il.If(() =>
            {
                il.Ldc(1);
            }));
        }

        [Test]
        public void If_StackNotSameType_Throws()
        {
            IlTester tester = IlTester.Create<int>();
            var il = tester.IL;

            il.Ldstr("Hello");

            il.Ldc(1);

            Assert.Throws<RepomatException>(() => il.If(() =>
            {
                il.Pop();
                il.Ldc(1);
            }));
        }

        [Test]
        public void IfElse_StackNotSameLength_Throws()
        {
            IlTester tester = IlTester.Create<int>();
            var il = tester.IL;

            il.Ldc(0);

            Assert.Throws<RepomatException>(() => il.If(() =>
            {
                il.Ldc(1);
            },
            () =>
            {
                il.Ldc(2);
                il.Ldc(3);
            }));
        }

        [Test]
        public void IfElse_StackNotSameType_Throws()
        {
            IlTester tester = IlTester.Create<int>();
            var il = tester.IL;

            il.Ldc(1);

            Assert.Throws<RepomatException>(() => il.If(() =>
            {
                il.Ldc(1);
            },
            () =>
            {
                il.Ldstr("Hi");
            }));
        }

    }
}
