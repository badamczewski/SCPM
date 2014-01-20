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

using SCPM.Interfaces;
using SCPM.Scheduling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SCPM.Exceptions;

namespace SCPM.Threading
{
    /// <summary>
    /// Represents a specific unit of execution that it self controls the process
    /// of halting and switching to other computations of this type and returning againg
    /// to continue work.
    /// </summary>
    /// <typeparam name="T">typeparam that represents the state to be used while executing.</typeparam>
    public sealed class FiberComputation<T> : IComputation
    {
        private Func<T, IEnumerable<FiberStatus>> action;
        private IEnumerator<FiberStatus> fiberContext;
        private IWorkScheduler scheduler;
        private ManualResetEvent wait;
        private FiberStatus status;
        private bool isInternalComputation;
        private T state;

        private ComputationCookie cookie;

        public FiberComputation(Func<T, IEnumerable<FiberStatus>> fiberAction)
            : this(fiberAction, new ComputationCookie())
        {
        }

        public FiberComputation(Func<T, IEnumerable<FiberStatus>> fiberAction, ComputationCookie cookie)
        {
            this.action = fiberAction;
            this.scheduler = DefaultWorkScheduler.Scheduler;
            this.fiberContext = action(state).GetEnumerator();

            this.cookie = cookie;
        }

        internal FiberComputation(Func<T, IEnumerable<FiberStatus>> action, T state, bool isInternal)
            : this(action)
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
                if (fiberContext.MoveNext() == true && status != FiberStatus.Wait)
                {
                    status = fiberContext.Current;
                    return status;
                }
                else if (status == FiberStatus.Wait)
                {
                    status = FiberStatus.Switch;
                    return status;
                }
                else
                    status = FiberStatus.Done;

                //Internal computation doesn't need to set the event.
                if (!isInternalComputation && wait != null)
                    wait.Set();

                return status;
            }
            catch (Exception ex)
            {
                cookie.IsException = true;
                cookie.Exception = ex;

                if (wait != null)
                    wait.Set();

                return FiberStatus.Done;
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
            get { return Resources.Computation_Fiber; }
        }
    }
}
