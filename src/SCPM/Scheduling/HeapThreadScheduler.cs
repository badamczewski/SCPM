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
using SCPM.Threading;
using System.Threading;
using SCPM.Collections;
using SCPM.Interfaces;

namespace SCPM.Scheduling
{
    /// <summary>
    /// Represents a heap based scheduller that guarantes that the best possible
    /// thread to schedule on will be found. 
    /// 
    /// This scheduler is very efective when very uniform workloads are needed.
    /// (the owner lock the rescheduling process and other threads skip the routine)
    /// </summary>
    public class HeapThreadScheduler : IExpandableThreadScheduler
    {
        private const string name = "HeapThreadScheduler";
        private Heap threadScheduler;
        private readonly object locker = new object();

        static HeapThreadScheduler() { }
        private HeapThreadScheduler() { threadScheduler = new Heap(new ISchedulableThread[] { }); }

        public static HeapThreadScheduler Create()
        {
            return new HeapThreadScheduler();
        }

        public bool IsCreated { get; set; }
        public string Name { get { return name; } }
        public int ThreadCount { get; set; }

        public void Add(ISchedulableThread thread)
        {
            lock (locker)
            {
                threadScheduler.Insert(thread);
            }
        }

        public void Remove(ISchedulableThread thread)
        {
            lock (locker)
            {
                threadScheduler.Remove(thread);
            }
        }

        public void Create(ISchedulableThread[] threads)
        {
            threadScheduler = new Heap(threads);

            for (int i = 0; i < threadScheduler.items.Length; i++)
            {
                threadScheduler.items[i].SchedulableIndex = i;
            }
            ThreadCount = threadScheduler.GetSize();
            IsCreated = true;
        }

        public ISchedulableThread GetScheduledThread()
        {
            ISchedulableThread thread = threadScheduler.ReadMin();
            return thread;
        }

        public void Reschedule(ISchedulableThread owner, SchedulerAction action)
        {
            if (threadScheduler.IsBuilding == false)
            {
                //We need this lock as our underlying heap implementation
                //is not thread safe atm, in the future TODO: this will change
                //as we will move to other DS or use fine grained or lock free heaps.
                if (Monitor.TryEnter(locker))
                {

                    threadScheduler.IsBuilding = true;
                    threadScheduler.Update(owner.SchedulableIndex, owner);
                    Monitor.Exit(locker);
                    threadScheduler.IsBuilding = false;
                }
            }
        }

        public ISchedulableThread GetBestThread()
        {
            return threadScheduler.ReadMin();
        }

        public ISchedulableThread GetWorstThread()
        {
            return threadScheduler.ReadMax();
        }

    }
}
