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
using System.Threading;
using SCPM.Collections;

namespace SCPM.Threading
{
    public class FiberThread : ISchedulableThread
    {
        private readonly int waitSpinLmit = 20;
        private readonly int waitSpinTime = 30;

        private readonly Thread fiberThread;
        internal readonly List<IComputation> scheduler;
        private readonly ManualResetEvent wait;
        private readonly object locker = new object();
        private int id;
        private int waitSpinCount = 0;
        private bool isSignalled;
        private bool isPendingJoin;

        public FiberThread()
        {
            fiberThread = new Thread(Process);
            id = fiberThread.ManagedThreadId;
            scheduler = new List<IComputation>();
            wait = new ManualResetEvent(false);

            waitSpinLmit = Configuration.FiberThreadWaitSpinLimit;
            waitSpinTime = Configuration.FiberThreadWaitSpinTime;
        }

        public bool IsStarted { get; private set; }
        public int ThreadId { get { return id; } }
        public int ComputationCount { get { return scheduler.Count; } }
        public int SchedulableIndex { get; set; }

        /// <summary>
        /// Joins (blocks and releases resources) the current thread.
        /// </summary>
        public void Join()
        {
            isPendingJoin = true;
        }

        public void Execute(IComputation computation)
        {
            lock (locker)
            {
                scheduler.Add(computation);
            }

            isSignalled = wait.Set();
        }

        public void Start()
        {
            fiberThread.Start();
            IsStarted = true;
        }

        private void Process()
        {
            int index = 0;

            while (true)
            {
                if (isSignalled)
                {
                    isSignalled = wait.Reset();
                }

                if (index >= Int16.MaxValue)
                    index = 0;

                Monitor.Enter(locker);

                if (scheduler.Count > 0)
                {
                    // Get next fiber thread from scheduller and execute.
                    IComputation fiber = scheduler[index++ % scheduler.Count];
                    InvokeAction(fiber);

                    Monitor.Exit(locker);
                }
                else
                {
                    Monitor.Exit(locker);

                    if (isPendingJoin)
                        break;

                    SpinWait();
                }
            }

            //end processing.
            IsStarted = false;
            wait.Close();
        }

        /// <summary>
        /// Invokes the Fiber Action.
        /// </summary>
        /// <param name="fiber">the fiber computation.</param>
        private void InvokeAction(IComputation fiber)
        {
            object result = fiber.Execute();
            FiberStatus fiberStatus = (FiberStatus)result;

            if (fiberStatus == FiberStatus.Done)
            {
                scheduler.Remove(fiber);
            }
        }

        /// <summary>
        /// Increments the counter of a wait spin on the current thread and eiter it
        /// gives up it's timeslice to another thread or it waits and sleeps
        /// or it waits to become signalled.
        /// </summary>
        private void SpinWait()
        {
            waitSpinCount++;

            if (waitSpinCount > waitSpinLmit + 2)
            {
                waitSpinCount = 0;
                wait.WaitOne();
            }
            else if (waitSpinCount == waitSpinLmit)
                Thread.Sleep(0);
            else if (waitSpinCount == waitSpinLmit + 1)
                Thread.Sleep(1);
            else if (waitSpinCount > waitSpinLmit + 1)
            {
                wait.WaitOne(waitSpinTime);
            }
        }

        public int CompareTo(ISchedulableThread other)
        {
            return this.scheduler.Count - ((FiberThread)other).scheduler.Count;
        }

        public int Count
        {
            get { return this.Count; }
        }
    }
}
