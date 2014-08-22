﻿using NUnit.Framework;
using Repomat.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests.IlGen
{
    [TestFixture]
    public class ConversionTests
    {
        [Test]
        public void IntConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int));

            var t = new IlTester<int>();
            var il = t.IL;

            il.Emit(OpCodes.Ldstr, "1234");
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(1234, result);
        }

        [Test]
        public void DateTimeConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTime));

            var t = new IlTester<DateTime>();
            var il = t.IL;

            il.Emit(OpCodes.Ldstr, "12/13/1971");
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(new DateTime(1971, 12, 13), result);
        }

        [Test]
        public void DateTimeOffsetConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTimeOffset));

            var t = new IlTester<DateTimeOffset>(typeof(object));
            var il = t.IL;

            il.Emit(OpCodes.Ldarg_0);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var offset = new DateTimeOffset(new DateTime(2000, 2, 4));
            var result = t.Invoke(offset);
            Assert.AreEqual(offset, result);
        }
    }
}
