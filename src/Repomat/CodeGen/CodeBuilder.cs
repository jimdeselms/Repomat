using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class CodeBuilder
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private bool _atStartOfLine = true;
        private int _indentationLevel = 0;

        public void Write(string format, params object[] args)
        {
            BeginLineIfNeeded();

            _builder.AppendFormat(format, args);
        }

        public void WriteLine()
        {
            WriteLine("");
        }

        public void WriteLine(string format, params object[] args)
        {
            BeginLineIfNeeded();

            _builder.AppendFormat(format + "\r\n", args);

            _atStartOfLine = true;
        }

        private void BeginLineIfNeeded()
        {
            if (_atStartOfLine)
            {
                _builder.Append("".PadLeft(_indentationLevel * 4));
                _atStartOfLine = false;
            }
        }

        public void OpenBrace()
        {
            if (!_atStartOfLine)
            {
                WriteLine();
            }
            WriteLine("{{");
            Indent();
        }

        public void CloseBrace()
        {
            Outdent();
            WriteLine("}}");
        }

        public void Indent()
        {
            if (!_atStartOfLine)
            {
                _builder.AppendLine();
            }
            _atStartOfLine = true;
            _indentationLevel++;
        }

        public void Outdent()
        {
            if (!_atStartOfLine)
            {
                _builder.AppendLine();
            }
            _atStartOfLine = true;
            _indentationLevel--;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override string ToString()
        {
            if (_indentationLevel != 0)
            {
                throw new Exception("Unmatched braces");
            }
            return _builder.ToString();
        }
    }
}
