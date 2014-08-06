using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Runtime;
using Moq;
using NUnit.Framework;

namespace Repomat.UnitTests
{
    [TestFixture]
    class ReaderHelperTests
    {
        [Test]
        public void VerifyFieldsAreUnique_HappyCase_NoException()
        {
            var reader = SetupReader("Happy", "Case");
            ReaderHelper.VerifyFieldsAreUnique(reader);
        }

        [Test]
        public void VerifyFieldsAreUnique_BothSame_Exception()
        {
            var reader = SetupReader("sad", "sad");
            Assert.Throws<RepomatException>(() => ReaderHelper.VerifyFieldsAreUnique(reader));
        }

        [Test]
        public void VerifyFieldsAreUnique_SameButDifferentCase_Exception()
        {
            var reader = SetupReader("sad", "hi", "SAD", "there");
            Assert.Throws<RepomatException>(() => ReaderHelper.VerifyFieldsAreUnique(reader));
        }

        [Test]
        public void VerifyFieldsAreUnique_SameButDifferentTables_Exception()
        {
            var reader = SetupReader("y.sad", "this", "that", "theother", "sad");
            Assert.Throws<RepomatException>(() => ReaderHelper.VerifyFieldsAreUnique(reader));
        }

        [Test]
        public void GetIndexForColumn_GetsIndexes()
        {
            var reader = SetupReader("a", "foo.b", "blah.C");
            Assert.AreEqual(0, ReaderHelper.GetIndexForColumn(reader, "A"));
            Assert.AreEqual(1, ReaderHelper.GetIndexForColumn(reader, "b"));
            Assert.AreEqual(2, ReaderHelper.GetIndexForColumn(reader, "c"));
        }

        [Test]
        public void GetIndexForColumn_ColumnNotFound_Throws()
        {
            var reader = SetupReader("a", "foo.b", "blah.C");
            Assert.Throws<RepomatException>(() => ReaderHelper.GetIndexForColumn(reader, "bogus"));
        }

        private IDataReader SetupReader(params string[] columns)
        {
            Mock<IDataReader> reader = new Mock<IDataReader>();
            reader.Setup(r => r.FieldCount).Returns(columns.Length);
            for (int i = 0; i < columns.Length; i++)
            {
                int idx = i;
                reader.Setup(r => r.GetName(idx)).Returns(columns[idx]);
            }
            return reader.Object;
        }
    }
}
