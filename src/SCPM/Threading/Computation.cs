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
using System.Text;
using SCPM.Interfaces;
using SCPM.Scheduling;
using System.Threading;
using SCPM.Exceptions;

namespace SCPM.Threading
{
    /// <summary>
    /// Represents an unit of execution.
    /// </summary>
    /// <typeparam name="T">typeparam that represents the state to be used while executing.</typeparam>
    public sealed class Computation<T> : IComputation
    {
        private Action<T> action;
        private IWorkScheduler scheduler;
        private ManualResetEvent wait;
        private bool isInternalComputation;
        private T state;

        private ComputationCookie cookie;

        public Computation(Action<T> action) : this(action, new ComputationCookie())
        { }

        public Computation(Action<T> action, ComputationCookie cookie)
        {
            this.action = action;
            this.scheduler = DefaultWorkScheduler.Scheduler;

            this.cookie = cookie;
        }

        internal Computation(Action<T> action, T state, bool isInternal) : this(action)
        {
            this.state = state;
            this.isInternalComputation = isInternal;
        }

        /// <summary>
        /// Runs the current computation.
        /// </summary>
        /// <param name="state">The state to be passed.</param>
        public void Run(T state)
        {
            this.state = state;
            this.scheduler.Queue(this);
        }

        /// <summary>
        /// Executes the given computation.
        /// </summary>
        /// <returns></returns>
        object IComputation.Execute()
        {
            try
            {
                action(state);

                //Internal computation doesn't need to set the event.
                if (!isInternalComputation && wait != null)
                    wait.Set();

                return null;
            }
            catch (Exception ex)
            {
                cookie.IsException = true;
                cookie.Exception = ex;

                if (wait != null)
                    wait.Set();

                return null;
            }
        }

        /// <summary>
        /// Bloks the current thread and waits for the computation to finish.
        /// </summary>
        public void WaitForCompletion()
        {
            if (wait == null)
                wait = new ManualResetEvent(false);

            wait.WaitOne();
            wait.Close();

            if (cookie.IsException)
                throw new ComputationException(Resources.Computation_Exception, cookie.Exception);
        }

        public string ComputationType
        {
            get { return Resources.Computation_Generic; }
        }
    }
}
