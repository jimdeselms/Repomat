using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class NamingConventionTests
    {
        [Test]
        public void ToLowercaseWordsTest()
        {
            string input = "this_Is_a_TEST";
            string output = "this_is_a_t_e_s_t";

            var convention = NamingConvention.LowercaseWords;
            Assert.AreEqual(output, convention.Convert(input));
        }

        [Test]
        public void ToUppercaseWordsTest()
        {
            string input = "this_Is_a_TEST";
            string output = "THIS_IS_A_T_E_S_T";

            var convention = NamingConvention.UppercaseWords;
            Assert.AreEqual(output, convention.Convert(input));
        }

        [Test]
        public void ToCamelCaseTest()
        {
            string input = "this_Is_a_TEST";
            string output = "thisIsATEST";

            var convention = NamingConvention.CamelCase;
            Assert.AreEqual(output, convention.Convert(input));
        }

        [Test]
        public void ToPascalCaseTest()
        {
            string input = "this_Is_a_TEST";
            string output = "ThisIsATEST";

            var convention = NamingConvention.PascalCase;
            Assert.AreEqual(output, convention.Convert(input));
        }

        [Test]
        public void NoopTest()
        {
            string input = "this_Is_a_TEST";

            var convention = NamingConvention.NoOp;
            Assert.AreEqual(input, convention.Convert(input));
        }

        [Test]
        public void OverrideExists_UserOverride()
        {
            var convention = NamingConvention.PascalCase;
            convention.AddOverride("foobar", "My table named 'Foobar'");

            Assert.AreEqual("My table named 'Foobar'", convention.Convert("foobar"));
        }
    }
}
