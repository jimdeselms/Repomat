using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema
{
    internal class ValidationError
    {
        private readonly string _code;
        private readonly string _message;

        public ValidationError(string errorCode, string format, params object[] args)
        {
            _code = errorCode;
            _message = string.Format(format, args);
        }

        public string Code { get { return _code; } }
        public string Message { get { return _message; } }

        public override bool Equals(object obj)
        {
            ValidationError other = obj as ValidationError;

            return other == this || (other._code == _code && other._message == _message);
        }

        public override int GetHashCode()
        {
            return _code.GetHashCode() ^ _message.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", _code, _message);
        }
    }
}
