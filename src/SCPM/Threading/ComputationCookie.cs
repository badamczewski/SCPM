#region Licence
/*
Copyright (c) 2011-2014 Contributors as noted in the AUTHORS file

This file is part of SCPM.

SCPM is free software; you can redistribute it and/or modify it under
the terms of the GNU Lesser General Public License as published by
the Free Software Foundation; either version 3 of the License, or
(at your option) any later version.

SCPM is distributed WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

You should have received a copy of the GNU Lesser General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/
#endregion

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

        /// <summary>
        /// Gets the information that idicates if the computation is completed.
        /// </summary>
        public bool Completed { get; internal set; }

        /// <summary>
        /// Gets the information that idicates if the the issuer was waiting on
        /// computation completion.
        /// </summary>
        public bool WasWaitingForCompletion { get; internal set; }
    }
}
