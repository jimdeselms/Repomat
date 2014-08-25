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

            var t = new IlTester<int>();
            var il = t.IL;

            il.Emit(OpCodes.Ldstr, "1234");
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(1234, result);
        }

        [Test]
        public void IntConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int));

            var t = new IlTester<int>();
            var il = t.IL;

            il.Emit(OpCodes.Ldc_I4, 1234);
            il.Emit(OpCodes.Box, typeof(int));
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

        [Test]
        public void StringConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(string));

            var t = new IlTester<string>();
            var il = t.IL;

            il.Emit(OpCodes.Ldc_I4, 9876);
            il.Emit(OpCodes.Box, typeof(int));
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();
            Assert.AreEqual("9876", result);
        }

        [Test]
        public void NullStringConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(string));

            var t = new IlTester<string>();
            var il = t.IL;

            il.Emit(OpCodes.Ldnull);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();
            Assert.IsNull(result);
        }

        [Test]
        public void CharConversion()
        {
            var info = PrimitiveTypeInfo.Get(typeof(char));

            var t = new IlTester<char>();
            var il = t.IL;

            il.Emit(OpCodes.Ldstr, "Hello");
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();
            Assert.AreEqual('H', result);
        }

        [Test]
        public void NullableIntConversion_Null()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int?));

            var t = new IlTester<int?>();
            var il = t.IL;

            il.Emit(OpCodes.Ldnull);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableIntConversion_NotNull_ReturnsValue()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int?));

            var t = new IlTester<int?>();
            var il = t.IL;

            il.Emit(OpCodes.Ldstr, "123");
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(123, result);
        }

        [Test]
        public void NullableIntConversion_DbNull_ReturnsNull()
        {
            var info = PrimitiveTypeInfo.Get(typeof(int?));

            var t = new IlTester<int?>();
            var il = t.IL;

            var dbNullValueProp = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            il.Emit(OpCodes.Ldsfld, dbNullValueProp);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableDateTimeConversion_Null()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTime?));

            var t = new IlTester<DateTime?>();
            var il = t.IL;

            il.Emit(OpCodes.Ldnull);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableDateTimeConversion_NotNull_ReturnsValue()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTime?));

            var t = new IlTester<DateTime?>();
            var il = t.IL;

            il.Emit(OpCodes.Ldstr, "4/5/2012");
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(new DateTime(2012, 4, 5), result);
        }

        [Test]
        public void NullableDateTimeConversion_DbNull_ReturnsNull()
        {
            var info = PrimitiveTypeInfo.Get(typeof(DateTime?));

            var t = new IlTester<DateTime?>();
            var il = t.IL;

            var dbNullValueProp = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            il.Emit(OpCodes.Ldsfld, dbNullValueProp);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableCharConversion_Null()
        {
            var info = PrimitiveTypeInfo.Get(typeof(char?));

            var t = new IlTester<char?>();
            var il = t.IL;

            il.Emit(OpCodes.Ldnull);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void NullableCharConversion_NotNull_ReturnsValue()
        {
            var info = PrimitiveTypeInfo.Get(typeof(char?));

            var t = new IlTester<char?>();
            var il = t.IL;

            il.Emit(OpCodes.Ldstr, "hi there");
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual('h', result);
        }

        [Test]
        public void NullableCharConversion_DbNull_ReturnsNull()
        {
            var info = PrimitiveTypeInfo.Get(typeof(char?));

            var t = new IlTester<char?>();
            var il = t.IL;

            var dbNullValueProp = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            il.Emit(OpCodes.Ldsfld, dbNullValueProp);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(null, result);
        }
        [Test]
        public void ByteArrayConversion_Null()
        {
            var info = PrimitiveTypeInfo.Get(typeof(byte[]));

            var t = new IlTester<byte[]>();
            var il = t.IL;

            il.Emit(OpCodes.Ldnull);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(null, result);
        }

        [Test]
        public void ByteArrayConversion_NotNull_ReturnsValue()
        {
            var info = PrimitiveTypeInfo.Get(typeof(byte[]));

            var t = new IlTester<byte[]>();
            var il = t.IL;

            il.Emit(OpCodes.Ldc_I4, 3);
            il.Emit(OpCodes.Newarr, typeof(byte));
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            CollectionAssert.AreEqual(new byte[3], result);
        }

        [Test]
        public void ByteArrayConversion_DbNull_ReturnsNull()
        {
            var info = PrimitiveTypeInfo.Get(typeof(byte[]));

            var t = new IlTester<byte[]>();
            var il = t.IL;

            var dbNullValueProp = typeof(DBNull).GetField("Value", BindingFlags.Public | BindingFlags.Static);
            il.Emit(OpCodes.Ldsfld, dbNullValueProp);
            info.EmitConversion(il);
            il.Emit(OpCodes.Ret);

            var result = t.Invoke();

            Assert.AreEqual(null, result);
        }
    }
}
