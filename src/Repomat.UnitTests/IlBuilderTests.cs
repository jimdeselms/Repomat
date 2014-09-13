using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    [TestFixture]
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
    }
}
