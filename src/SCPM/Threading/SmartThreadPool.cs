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
using System.Threading;
using SCPM.Threading;
using SCPM.Collections;
using SCPM.Interfaces;
using SCPM.Scheduling;
using SCPM.Common;

namespace SCPM.Threading
{
   /// <summary>
    /// Thread pool that incorprates fair thread scheduling as well as work stealing.
    /// </summary>
    public static class SmartThreadPool
    {
        /// <summary>
        /// Heap that represents a priority queue of worker threads.
        /// </summary>
        private static readonly IThreadScheduler threadScheduler = null;

        /// <summary>
        /// Heap that represents a priority queue of worker threads that werent created in the pool.
        /// </summary>
        private static readonly IThreadScheduler artificialThreadScheduler = null;

        /// <summary>
        /// A default static constructor that initializes the pool threads.
        /// </summary>
        static SmartThreadPool()
        {
            //create a scheduler for threads created outside the computation system.
            //By default this is a heap scheduller since there can be much more threads,
            //thus heap sort is more efficient.
            artificialThreadScheduler = Configuration.ArtificialThreadScheduler;

            uint physicalProcessors = Unsafe.GetPhysicalCores();

            //the idea here is {core_count} * 2 the rest should be spawned as fibers.
            ISchedulableThread[] threads = new ISchedulableThread[physicalProcessors * 2];

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new SmartThread(true, -1);
                threads[i].SchedulableIndex = i;
            }

            //Load the default thread scheduler.
            threadScheduler = Configuration.SmartPoolScheduler;

            threadScheduler.Create(threads);
        }

        public static int GetThreadLoad()
        {
            SmartThread thread = (SmartThread)threadScheduler.GetBestThread();
            return thread.scheduler.UnsafeCount;
        }

        /// <summary>
        /// Queues a new Action on the thread pool.
        /// </summary>
        /// <param name="action">Action delegate.</param>
        public static void QueueWorkItem<T>(Action<T> action, T state)
        {
            QueueWorkItem(new Computation<T>(action, state, true));
        }

        public static void QueueWorkItem(IComputation computation)
        {
            ISchedulableThread lowestWorkloadThread = null;

            lowestWorkloadThread = threadScheduler.GetScheduledThread();

            //If a thread is not started then do Start it.
            if (lowestWorkloadThread.IsStarted == false)
            {
                lowestWorkloadThread.Start();
            }

            //schedule a task.
            lowestWorkloadThread.Execute(computation);
            threadScheduler.Reschedule(lowestWorkloadThread, SchedulerAction.Enqueue);
        }

        /// <summary>
        /// Tries to steal work from the most loaded thread in the pool.
        /// </summary>
        /// <returns>SmartThread</returns>
        /// <param name="threadPoolThread">boolean value idnicating that we want threads that
        /// were created in a threadpool.</param>
        /// <returns>SmartThread that has the most work to do.</returns>
        internal static ISchedulableThread GetThreadToSteal(bool threadPoolThread)
        {
            //get the element that has most of the load.
            if (threadPoolThread)
                return threadScheduler.GetWorstThread();

            return artificialThreadScheduler.GetWorstThread(); ;
        }

        /// <summary>
        /// Atempts to rebuild the pririty queue, to contain correct information
        /// about priorities.
        /// </summary>
        /// <param name="threadPoolThread">boolean value idnicating that we want threads that
        /// were created in a threadpool.</param>
        internal static void Reschedule(bool threadPoolThread, ISchedulableThread @this, SchedulerAction action)
        {
            if (threadPoolThread)
            {
                //we don't need to lock this section as generally we are ok
                //with the race as it will not cause any errors but putting a simple
                //flag arround it should be enough to prevent most races.
                threadScheduler.Reschedule(@this, action);

                return;
            }

            artificialThreadScheduler.Reschedule(@this, action);

        }

        /// <summary>
        /// Inserts a thread that not orginated in a thread pool to the artificial scheduler,
        /// in order to enable fair work scheduling and work stealing.
        /// </summary>
        /// <param name="thread">SmartThread.</param>
        internal static void InsertToArtificialScheduler(SmartThread thread)
        {
            artificialThreadScheduler.Add(thread);
        }

        /// <summary>
        /// Removes the given thread that not orginated in a thread pool from the artificial scheduler.
        /// </summary>
        /// <param name="thread">SmartThread</param>
        internal static void RemoveFromArtificialScheduler(SmartThread thread)
        {
            artificialThreadScheduler.Remove(thread);
        }
    }
}
