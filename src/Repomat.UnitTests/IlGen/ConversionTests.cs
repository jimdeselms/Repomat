﻿using System.Reflection;
using NUnit.Framework;
using Repomat.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Repomat.IlGen;

namespace Repomat.UnitTests.IlGen
{
    [TestFixture]
    public class ConversionTests
    {
        [Test]
        public void StringToIntConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int));

            var t = IlTester.Create<int>();
            var il = t.IL;

            il.Ldstr("1234");
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<int>();

            Assert.AreEqual(1234, result);
        }

        [Test]
        public void IntConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int));

            var t = IlTester.Create<int>();
            var il = t.IL;

            il.Ldc(1234);
            il.Box(typeof(int));
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<int>();

            Assert.AreEqual(1234, result);
        }

        [Test]
        public void DateTimeConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTime));

            var t = IlTester.Create<DateTime>();
            var il = t.IL;

            il.Ldstr("12/13/1971");
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<DateTime>();

            Assert.AreEqual(new DateTime(1971, 12, 13), result);
        }

        [Test]
        public void DateTimeOffsetConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTimeOffset));

            var t = IlTester.Create<DateTimeOffset>(typeof(object));
            var il = t.IL;

            il.Ldarg(0);
            info.EmitConversion(t.IL);
            il.Ret();

            var offset = new DateTimeOffset(new DateTime(2000, 2, 4));
            var result = t.Invoke<DateTimeOffset>(offset);
            Assert.AreEqual(offset, result);
        }

        [Test]
        public void StringConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(string));

            var t = IlTester.Create<string>();
            var il = t.IL;

            il.Ldc(9876);
            il.Box(typeof(int));
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<string>();
            Assert.AreEqual("9876", result);
        }

        [Test]
        public void NullStringConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(string));

            var t = IlTester.Create<string>();
            var il = t.IL;

            il.Ldnull();
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<string>();
            Assert.IsNull(result);
        }

        [Test]
        public void CharConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(char));

            var t = IlTester.Create<char>();
            var il = t.IL;

            il.Ldstr("Hello");
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<char>();
            Assert.AreEqual('H', result);
        }

        [Test]
        public void NullableIntConversion_Null()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int?));

            var t = IlTester.Create<int?>();
            var il = t.IL;

            il.Ldnull();
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<int?>();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableIntConversion_NotNull_ReturnsValue()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int?));

            var t = IlTester.Create<int?>();
            var il = t.IL;

            il.Ldstr("123");
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<int?>();

            Assert.AreEqual(123, result);
        }

        [Test]
        public void NullableIntConversion_DbNull_ReturnsNull()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int?));

            var t = IlTester.Create<int?>();
            var il = t.IL;

            var dbNullValueProp = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            il.Ldfld(dbNullValueProp);
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<int?>();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableDateTimeConversion_Null()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTime?));

            var t = IlTester.Create<DateTime?>();
            var il = t.IL;

            il.Ldnull();
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<DateTime?>();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableDateTimeConversion_NotNull_ReturnsValue()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTime?));

            var t = IlTester.Create<DateTime?>();
            var il = t.IL;

            il.Ldstr("4/5/2012");
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<DateTime?>();

            Assert.AreEqual(new DateTime(2012, 4, 5), result);
        }

        [Test]
        public void NullableDateTimeConversion_DbNull_ReturnsNull()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTime?));

            var t = IlTester.Create<DateTime?>();
            var il = t.IL;

            var dbNullValueProp = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            il.Ldfld(dbNullValueProp);
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<DateTime?>();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableCharConversion_Null()
        {
            var info = PrimitiveTypeInfo.Get(typeof(char?));

            var t = IlTester.Create<char?>();
            var il = t.IL;

            il.Ldnull();
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<char?>();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableCharConversion_NotNull_ReturnsValue()
        {
            var info = PrimitiveTypeInfo.Get(typeof(char?));

            var t = IlTester.Create<char?>();
            var il = t.IL;

            il.Ldstr("hi there");
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<char?>();

            Assert.AreEqual('h', result);
        }

        [Test]
        public void NullableCharConversion_DbNull_ReturnsNull()
        {
            var info = PrimitiveTypeInfo.Get(typeof(char?));

            var t = IlTester.Create<char?>();
            var il = t.IL;

            var dbNullValueProp = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            il.Ldfld(dbNullValueProp);
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<char?>();

            Assert.AreEqual(null, result);
        }
        [Test]
        public void ByteArrayConversion_Null()
        {
            var info = PrimitiveTypeInfo.Get(typeof(byte[]));

            var t = IlTester.Create<byte[]>();
            var il = t.IL;

            il.Ldnull();
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<byte[]>();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void ByteArrayConversion_NotNull_ReturnsValue()
        {
            var info = PrimitiveTypeInfo.Get(typeof(byte[]));

            var t = IlTester.Create<byte[]>();
            var il = t.IL;

            il.Ldc(3);
            il.Newarr(typeof(byte));
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<byte[]>();

            CollectionAssert.AreEqual(new byte[3], result);
        }

        [Test]
        public void ByteArrayConversion_DbNull_ReturnsNull()
        {
            var info = PrimitiveTypeInfo.Get(typeof(byte[]));

            var t = IlTester.Create<byte[]>();
            var il = t.IL;

            var dbNullValueProp = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            il.Ldfld(dbNullValueProp);
            info.EmitConversion(t.IL);
            il.Ret();

            var result = t.Invoke<byte[]>();

            Assert.AreEqual(null, result);
        }
    }
}
