using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat
{
    public class NamingConvention
    {
        private readonly Func<string, string> _convention;
        private readonly Dictionary<string, string> _overrides = new Dictionary<string, string>();

        public static NamingConvention PascalCase { get { return new NamingConvention(NamingConventionHelpers.ToPascalCase); } }
        public static NamingConvention CamelCase { get { return new NamingConvention(NamingConventionHelpers.ToCamelCase); } }
        public static NamingConvention UppercaseWords { get { return new NamingConvention(NamingConventionHelpers.ToUppercaseWords); } }
        public static NamingConvention LowercaseWords { get { return new NamingConvention(NamingConventionHelpers.ToLowercaseWords); } }
        public static NamingConvention NoOp { get { return new NamingConvention(s => s);}}

        public NamingConvention(Func<string, string> convention)
        {
            _convention = convention;
        }

        public NamingConvention AddOverride(string from, string to)
        {
            _overrides.Add(from, to);
            return this;
        }

        public string Convert(string input)
        {
            string overrideValue;
            if (_overrides.TryGetValue(input, out overrideValue))
            {
                return overrideValue;
            }

            return _convention(input);
        }
    }
}
