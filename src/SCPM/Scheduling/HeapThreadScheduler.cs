#region Licence
// Copyright (c) 2013 BAX Services Bartosz Adamczewski
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
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
    public class HeapThreadScheduler : IThreadScheduler
    {
        private const string name = "HeapThreadScheduler";
        private Heap threadScheduler;
        private readonly object locker = new object();

        static HeapThreadScheduler() { }
        private HeapThreadScheduler() { }

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
