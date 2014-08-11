using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat
{
    public static class ExtensionMethods
    {
        public static string Capitalize(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            else
            {
                return Char.ToUpper(s[0]) + s.Substring(1);
            }
        }

        public static string Uncapitalize(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            else
            {
                return Char.ToLower(s[0]) + s.Substring(1);
            }
        }

        public static string ToCSharp(this Type t)
        {
            string simpleTypeName;
            if (_simpleTypeNames.TryGetValue(t, out simpleTypeName))
            {
                return simpleTypeName;
            }
            else if (t.IsGenericType)
            {
                string name = t.FullName.Substring(0, t.FullName.IndexOf('`')).Replace("+", ".");

                var genericParameters = new List<string>();
                foreach (var parm in t.GetGenericArguments())
                {
                    genericParameters.Add(parm.ToCSharp());
                }

                return string.Format("{0}<{1}>", name, string.Join(", ", genericParameters));
            }
            else if (t.IsArray)
            {
                return t.GetCoreType().ToCSharp() + string.Format("[{0}]", "".PadRight(t.GetArrayRank() - 1, ','));
            }
            else
            {
                return t.FullName.Replace("&", "").Replace("+", ".");
            }
        }

        public static Type GetCoreType(this Type t)
        {
            // TODO - does this still work for byte[]?
            if (t.GetElementType() != null)
            {
                return t.GetElementType();
            }
            if (t.IsGenericType)
            {
                // This is cheating
                // TODO: Improve detection of the core type for an enumerable type.
                if (t.GetGenericArguments().Length == 1)
                {
                    return t.GetGenericArguments()[0];
                }

                throw new RepomatException(string.Format("Don't know how to get the core type for {0}", t.ToCSharp()));
            }
            else
            {
                return t;
            }
        }

        public static bool IsNullable(this Type t)
        {
            return t.Name == "Nullable`1" && t.Namespace == "System";
        }

        public static bool IsDatabaseType(this Type t)
        {
            return t == typeof(int)
                || t == typeof(int?)
                || t == typeof(uint)
                || t == typeof(uint?)
                || t == typeof(long)
                || t == typeof(long?)
                || t == typeof(ulong)
                || t == typeof(ulong?)
                || t == typeof(short)
                || t == typeof(short?)
                || t == typeof(ushort)
                || t == typeof(ushort?)
                || t == typeof(decimal)
                || t == typeof(decimal?)
                || t == typeof(byte)
                || t == typeof(byte?)
                || t == typeof(char)
                || t == typeof(char?)
                || t == typeof(bool)
                || t == typeof(bool?)
                || t == typeof(string)
                || t == typeof(byte[]);
        }

        public static bool ImplementsIEnumerableOfType(this Type t, Type coreType)
        {
            var enumerable = typeof(IEnumerable<>).MakeGenericType(coreType);
            return enumerable.IsAssignableFrom(t);
        }

        public static bool IsIEnumerableOfType(this Type t, Type coreType)
        {
            var enumerable = typeof(IEnumerable<>).MakeGenericType(coreType);
            return enumerable.Equals(t);
        }

        private static Dictionary<Type, string> _simpleTypeNames = new Dictionary<Type, string>();

        static ExtensionMethods()
        {
            _simpleTypeNames.Add(typeof(void), "void");
            _simpleTypeNames.Add(typeof(int), "int");
            _simpleTypeNames.Add(typeof(short), "short");
            _simpleTypeNames.Add(typeof(long), "long");
            _simpleTypeNames.Add(typeof(uint), "uint");
            _simpleTypeNames.Add(typeof(ushort), "ushort");
            _simpleTypeNames.Add(typeof(ulong), "ulong");
            _simpleTypeNames.Add(typeof(char), "char");
            _simpleTypeNames.Add(typeof(byte), "byte");
            _simpleTypeNames.Add(typeof(bool), "bool");
            _simpleTypeNames.Add(typeof(decimal), "decimal");
            _simpleTypeNames.Add(typeof(int?), "int?");
            _simpleTypeNames.Add(typeof(short?), "short?");
            _simpleTypeNames.Add(typeof(long?), "long?");
            _simpleTypeNames.Add(typeof(uint?), "uint?");
            _simpleTypeNames.Add(typeof(ushort?), "ushort?");
            _simpleTypeNames.Add(typeof(ulong?), "ulong?");
            _simpleTypeNames.Add(typeof(char?), "char?");
            _simpleTypeNames.Add(typeof(byte?), "byte?");
            _simpleTypeNames.Add(typeof(bool?), "bool?");
            _simpleTypeNames.Add(typeof(decimal?), "decimal?");
            _simpleTypeNames.Add(typeof(string), "string");
        }
    }
}
