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
using System.Runtime.InteropServices;
using System.Threading;
using SCPM.Collections;
using SCPM.Interfaces;
using SCPM.Scheduling;

namespace SCPM.Threading
{
    /// <summary>
    /// A smart thread that incorporates work stealing tehniques.
    /// </summary>
    public class SmartThread : ISchedulableThread
    {
        /// <summary>
        /// Native API import function, that is used to Yeild a thread on
        /// the same processor.
        /// </summary>
        /// <returns>the value indicating that the operation was successful.</returns>
        [DllImport("kernel32.dll")]
        static extern bool SwitchToThread();

        private readonly int waitSpinLmit = 20;
        private readonly int waitSpinLmitOne = 21;
        private readonly int waitSpinLmitTwo = 22;
        private readonly int waitSpinTime = 30;
        
        private readonly Thread thread;
        internal readonly NonBlockingHybridQueue<IComputation> scheduler;
        private readonly ManualResetEvent wait;
        private readonly int stealCondition = 2;

        private bool isSignalled;
        private bool isPendingJoin;
        private int id;
        private int executionCount;
        private int queueCount;
        private long totalTime;
        private bool isInitializedInPool;
        private int waitSpinCount = 0;
        
        /// <summary>
        /// Gets the index in the scheduller. 
        /// This data is exposed for custom schedullers.
        /// <summary>
        public int SchedulableIndex { get; set; }

        /// <summary>
        /// Gets the ManagedThread Id.
        /// </summary>
        public int ThreadId
        {
            get { return id; }
        }

        /// <summary>
        /// Gets the Average task running time.
        /// </summary>
        public decimal AvgTaskTime
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the TotalTaskTime.
        /// </summary>
        /// <remarks>
        /// This value will get reseted over time, after it reaches the maximum int32 value.
        /// </remarks>
        public long TotalTasksTime
        {
            get { return totalTime; }
        }

        /// <summary>
        /// Gets the value that indicated that the thread was created in the ThreadPool.
        /// </summary>
        public bool IsInitializedInPool
        {
            get { return isInitializedInPool; }
        }

        /// <summary>
        /// Gets the workload (total num of computations / total num of finished computations).
        /// </summary>
        public double Workload
        {
            get { return executionCount != 0 ? queueCount / executionCount : 1; }
        }

        /// <summary>
        /// An internal constructor that initializes a smart thread with a parameter
        /// indicating that this insance is started in a threadpool.
        /// </summary>
        /// <param name="isInitializedInPool">Indicates that this instance will live in a threadpool.</param>
        internal SmartThread(bool isInitializedInPool)
        {
            this.isInitializedInPool = isInitializedInPool;
            thread = new Thread(new ThreadStart(Process));
            thread.IsBackground = true;
            thread.Priority = Configuration.SmartThreadDefaultPrority;

            waitSpinLmit = Configuration.SmartThreadWaitSpinLimit;
            waitSpinTime = Configuration.SmartThreadWaitSpinTime;
            stealCondition = Configuration.SmartThreadStealCondition;

            waitSpinLmitOne = waitSpinLmit + 1;
            waitSpinLmitTwo = waitSpinLmit + 2;

            wait = new ManualResetEvent(isSignalled);
            scheduler = new NonBlockingHybridQueue<IComputation>();
            id = thread.ManagedThreadId;        
        }

        /// <summary>
        /// Initializes the SmartThread, in the non thread pool scope, therefor
        /// this thread will not steal work.
        /// </summary>
        public SmartThread() : this(false) { }

        /// <summary>
        /// The thread Processing loop, that consumes up the queue. 
        /// </summary>
        private void Process()
        {
            IComputation localComputation = null;
        
            while (true)
            {
                //check if our thread is in the signalled state,
                //if that's true then reset it and continue work. 
                if (isSignalled)
                {
                    isSignalled = wait.Reset();
                }

                if (scheduler.IsEmpty == false)
                {
                    localComputation = scheduler.Dequeue();
                }
                else
                {
                    //lets try to steal some of this work.
                    bool workStolen = TryStealWork();

                    //if we stolen something then don't sleap and first check you work queue
                    //and then try to steal again.
                    while (workStolen)
                        workStolen = TryStealWork();

                    if (isPendingJoin)
                        break;

                    SpinWait();
                }

                if (localComputation != null)
                {
                    InvokeAction(localComputation);
                    localComputation = null;

                    //update the stats.
                    SmartThreadPool.Reschedule(isInitializedInPool, this, SchedulerAction.Dequeue);
                }
            }
    
            //end processing.
            IsStarted = false;
            wait.Close();
        }

        /// <summary>
        /// Increments the counter of a wait spin on the current thread and eiter it
        /// gives up it's timeslice to another thread or it waits and sleeps
        /// or it waits to become signalled.
        /// </summary>
        private void SpinWait()
        {
            waitSpinCount++;

            if (waitSpinCount > waitSpinLmitTwo)
            {
                waitSpinCount = 0;
                wait.WaitOne();
            }
            else if (waitSpinCount == waitSpinLmit)
                Thread.Sleep(0);
            else if (waitSpinCount == waitSpinLmitOne)
                Thread.Sleep(1);
            else if (waitSpinCount > waitSpinLmitTwo)
            {
                wait.WaitOne(waitSpinTime);
            }
        }

        /// <summary>
        /// Invokes the current action.
        /// </summary>
        /// <param name="localAction">localAction taken from queue.</param>
        private void InvokeAction(IComputation localAction)
        {
            // start to measure time.
            int ticksStart = System.Environment.TickCount;

            // do execute the action.
            localAction.Execute();

            //we do need to reset the stats so that they will not overflow.
            if (totalTime > int.MaxValue)
            {
                executionCount = 0;
                queueCount = 0;
                totalTime = 0;
            }

            //increment the counter.
            executionCount++;

            totalTime += System.Environment.TickCount - ticksStart;

            AvgTaskTime = totalTime / executionCount;
        }

        /// <summary>
        /// Tries to steal workload from other heavy loaded threads.
        /// </summary>
        /// <returns>a boolan flag indicating the steal success or failure.</returns>
        internal bool TryStealWork()
        {
            //1. Ask the pool for a thread with the worst stats.
            //2. Access it's internal queue by calling count and then doing Dequeue
            // Here we eith hold a lock or we dont lock at all and handle all queue empty exception.

            SmartThread threadToSteal = (SmartThread)SmartThreadPool.GetThreadToSteal(isInitializedInPool);

            //This code is needed as ThreadPool might tell us that in some sittuations that we can steal
            //work from ourselvs for e.g if other thread will join or we will fork ourselfs and the operations
            //is running.
            if (threadToSteal.id != this.id)
            {
                //perform a steal but only if the window is grater then 1 this means that we will
                //try to snach the item when no one is asking for it, if we fail we still do it in lock free
                //fashion but we may spin.

                bool result = TryStealFromQueue(threadToSteal.scheduler, stealCondition);

                //update the stats.
                if (result)
                    SmartThreadPool.Reschedule(isInitializedInPool, threadToSteal, SchedulerAction.Dequeue);

                return result;
            }

            return false;
        }

        /// <summary>
        /// Tries to perform a steal from the provided queue.
        /// </summary>
        /// <param name="queue">the queue type.</param>
        /// <param name="condition">the condition for the queue steal.</param>
        /// <returns>a value indicationg the sucess or failure of the operation.</returns>
        private bool TryStealFromQueue(IStealingQueue<IComputation> queue, int conditionThreshold)
        {
            // Try steal as much as possible from the selected thread as the selection
            // procedure to steal is expensive.
            while (!queue.IsEmpty && queue.UnsafeCount >= conditionThreshold)
            {
                IComputation localComputation = queue.DequeueLast();

                if (localComputation != null)
                {
                    localComputation.Execute();
                    return true;
                }
                else
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Schedules the current action for execution.
        /// </summary>
        /// <param name="action">the action to be executed.</param>
        public void Execute(IComputation action)
        {
            scheduler.Enqueue(action);
            queueCount++;

            isSignalled = wait.Set();
        }

        /// <summary>
        /// Starts the current thread.
        /// </summary>
        public void Start()
        {
            if (isInitializedInPool == false)
                SmartThreadPool.InsertToArtificialScheduler(this);

            thread.Start();
            IsStarted = true;
        }

        /// <summary>
        /// Joins (blocks and releases resources) the current thread.
        /// </summary>
        public void Join()
        {
            isPendingJoin = true;
        }

        /// <summary>
        /// Gets the value that indicates if the thread is started.
        /// </summary>
        public bool IsStarted
        {
            get;
            private set;
        }

        /// <summary>
        /// Passes the execution (gives up the time slice) to another working thread
        /// that's located on the same processor.
        /// </summary>
        /// <returns></returns>
        public bool Yield()
        {
            //for 4.0 we get this for free.
#if NET40

            return Thread.Yield();

#else
            return SwitchToThread();
#endif
        }

        // Actually threads should not be comparable :(
        // TODO: Create a comparable inner class.  
        public int CompareTo(ISchedulableThread other)
        {
            return this.scheduler.UnsafeCount - ((SmartThread)other).scheduler.UnsafeCount;
        }

    }
}
