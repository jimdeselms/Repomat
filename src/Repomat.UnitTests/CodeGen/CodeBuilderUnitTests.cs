using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.CodeGen;
using NUnit.Framework;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class CodeBuilderUnitTests
    {
        [Test]
        public void DoNothingReturnsEmptyString()
        {
            Expect("");
        }

        [Test]
        public void WriteLine_WritesWholeLine()
        {
            _builder.WriteLine("Hello, how are you!");
            Expect("Hello, how are you!\r\n");
        }

        [Test]
        public void Write_NothingElseWrittenYet_JustWritesString()
        {
            _builder.Write("Hello");
            Expect("Hello");
        }

        [Test]
        public void Write_WithParameters_IncludesParameters()
        {
            _builder.Write("{0}, {1}!", "Hello", "World");
            Expect("Hello, World!");
        }

        [Test]
        public void OpenBrace_Unclosed_Throws()
        {
            _builder.OpenBrace();
            Assert.Throws<Exception>(() => _builder.ToString());
        }

        [Test]
        public void OpenBrace_PairedWithCloseBrace_TwoLines()
        {
            _builder.OpenBrace();
            _builder.CloseBrace();
            Expect("{\r\n}\r\n");
        }

        [Test]
        public void OpenBrace_AfterWrite_NewLine()
        {
            _builder.Write("namespace Thingy");
            _builder.OpenBrace();
            _builder.CloseBrace();
            Expect("namespace Thingy\r\n{\r\n}\r\n");
        }

        [Test]
        public void OpenBrace_AfterWriteLine_NewLine()
        {
            _builder.WriteLine("namespace Thingy");
            _builder.OpenBrace();
            _builder.CloseBrace();
            Expect("namespace Thingy\r\n{\r\n}\r\n");
        }

        [Test]
        public void Indentation_SeveralLevels()
        {
            _builder.WriteLine("namespace Thingy");
            _builder.OpenBrace();

            _builder.WriteLine("class MyClass");
            _builder.OpenBrace();
            _builder.WriteLine("Hi");
            _builder.CloseBrace();

            _builder.WriteLine("class NoOp() {{}}");

            _builder.CloseBrace();

            Expect("namespace Thingy\r\n{\r\n    class MyClass\r\n    {\r\n        Hi\r\n    }\r\n    class NoOp() {}\r\n}\r\n");
        }

        private CodeBuilder _builder;

        private void Expect(string s)
        {
            Assert.AreEqual(s, _builder.ToString());
        }

        [SetUp]
        public void Setup()
        {
            _builder = new CodeBuilder();
        }
    }
}
