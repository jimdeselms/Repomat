using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Schema
{
    /// <summary>
    /// Defines the behavior of a singleton Get method if not exactly one row is returned
    /// 
    /// The default behavior for a singleton Get is to throw an exception if not exactly one row is returned.
    /// The default behavior for a TryGet method is to throw an exception if more than one row is returned.
    /// </summary>
    [Flags]
    public enum SingletonGetMethodBehavior
    {
        /// <summary>
        /// If zero rows are found, a singleton Get will return null. If multiple matches are found, the
        /// first row will be returned.
        /// </summary>
        Loose = 0,

        /// <summary>
        /// If set, a singleton Get method will fail if no rows are found. If not set, null will be returned.
        /// </summary>
        FailIfNoRowFound = 1,

        /// <summary>
        /// If set, a singleton Get or TryGet method will fail if multiple rows are found. If not set, the first match will be found.
        /// </summary>
        FailIfMultipleRowsFound = 2,

        /// <summary>
        /// A singleton Get method will throw an exception if the query does not return exactly one row.
        /// A TryGet method will throw an exception if the query returns more than one row
        /// </summary>
        Strict = FailIfNoRowFound | FailIfMultipleRowsFound
    }
}
