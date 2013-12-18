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
using System.Text;
using SCPM.Collections;
using SCPM.Interfaces;
using SCPM.Threading;
using System.Threading;

namespace SCPM.Scheduling
{
    public sealed class DefaultThreadScheduler : IThreadScheduler
    {
        private const string name = "DefaultScheduler"; 
        private List<ISchedulableThread> threadScheduler;
        private readonly object locker = new object();
        private bool isBuilding = false;

        static DefaultThreadScheduler() {}
        private DefaultThreadScheduler() {}

        public static DefaultThreadScheduler Create()
        {
            return new DefaultThreadScheduler();
        }

        public bool IsCreated { get; set; }
        public string Name { get { return name; } }
        public int ThreadCount { get; set; }

        public void Add(ISchedulableThread thread)
        {
            lock (locker)
            {
                threadScheduler.Add(thread);
            }
        }

        public void Remove(ISchedulableThread thread)
        { 
            threadScheduler.Remove(thread);
        }

        public void Create(ISchedulableThread[] threads)
        {
            threadScheduler = new List<ISchedulableThread>();

            for (int i = 0; i < threads.Length; i++)
            {
                threadScheduler.Add(threads[i]);
                threadScheduler[i].SchedulableIndex = i;
            }
            ThreadCount = threadScheduler.Count;
            IsCreated = true;
        }

        public ISchedulableThread GetScheduledThread()
        {
            ISchedulableThread thread = threadScheduler[0];

            return thread;
        }

        public void Reschedule(ISchedulableThread owner, SchedulerAction action)
        {
            if (isBuilding == false)
            {
                //We need this lock as our underlying implementation
                //is not thread safe atm, in the future TODO: this will change
                //as we will move to other DS or use fine grained locks.
                if (Monitor.TryEnter(locker))
                {
                    isBuilding = true;
                    var copy = threadScheduler[owner.SchedulableIndex];

                    if (action == SchedulerAction.Enqueue || action == SchedulerAction.Steal)
                    {
                        var indexPlus = owner.SchedulableIndex + 1;
                        while (threadScheduler.Count - 1 != owner.SchedulableIndex)
                        {
                            if (copy.CompareTo(threadScheduler[indexPlus]) > 0)
                            {
                                threadScheduler[owner.SchedulableIndex] = threadScheduler[indexPlus];
                                threadScheduler[owner.SchedulableIndex].SchedulableIndex--;
                                threadScheduler[indexPlus] = copy;
                                owner.SchedulableIndex++;
                                indexPlus = owner.SchedulableIndex + 1;
                            }
                            else
                                break;
                        }
                    }
                    else if (action == SchedulerAction.Dequeue)
                    {
                        var indexMinus = owner.SchedulableIndex - 1;
                        while (owner.SchedulableIndex != 0)
                        {
                            if (copy.CompareTo(threadScheduler[indexMinus]) < 0)
                            {
                                threadScheduler[owner.SchedulableIndex] = threadScheduler[indexMinus];
                                threadScheduler[owner.SchedulableIndex].SchedulableIndex++;
                                threadScheduler[indexMinus] = copy;
                                owner.SchedulableIndex--;
                                indexMinus = owner.SchedulableIndex - 1;
                            }
                            else
                                break;
                        }
                    }

                    Monitor.Exit(locker);

                }

                isBuilding = false;
            }
        }

        public ISchedulableThread GetBestThread()
        {
            return threadScheduler[0];
        }

        public ISchedulableThread GetWorstThread()
        {
            return threadScheduler[threadScheduler.Count - 1];
        }
    }
}
