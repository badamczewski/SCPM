using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCPM.Threading
{
    /// <summary>
    /// Represents a cookie thats reponsible for descreate
    /// comunication between the compuation and it's runner.
    /// 
    /// The client can inject of read mid execution some interesting
    /// data and behave acordingly.
    /// </summary>
    public class ComputationCookie
    {
        /// <summary>
        /// Gets the boolean flag that indicates that executing computation had an exception.
        /// </summary>
        public bool IsException { get; internal set; }
        /// <summary>
        /// Gets the exception.
        /// </summary>
        public Exception Exception { get; internal set; }
    }
}
