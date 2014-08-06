using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat
{
    [Serializable]
    public class RepomatException : Exception
    {
        public RepomatException(string format, params object[] args) : base(string.Format(format, args))
        {
        }
    }
}
