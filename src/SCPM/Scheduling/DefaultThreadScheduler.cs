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
        private int isBuilding = False;
        private const int False = 0;
        private const int True = 0;

        static DefaultThreadScheduler() { }
        private DefaultThreadScheduler() { }

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
            if (isBuilding == False)
            {
                //We need this lock as our underlying implementation
                //is not thread safe atm, in the future TODO: this will change
                //as we will move to other DS or use fine grained locks.
                int local = isBuilding;
                if (Interlocked.CompareExchange(ref isBuilding, 1, local) == 0)
                {
                    isBuilding = True;
                    var copy = threadScheduler[owner.SchedulableIndex];

                    if (action == SchedulerAction.Enqueue || action == SchedulerAction.Steal)
                    {
                        var indexPlus = owner.SchedulableIndex + 1;

                        while (threadScheduler.Count - 1 != owner.SchedulableIndex)
                        {
                            var plusThread = threadScheduler[indexPlus];

                            if (copy.CompareTo(plusThread) > 0)
                            {
                                plusThread.SchedulableIndex--;
                                threadScheduler[owner.SchedulableIndex] = plusThread;
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
                            var minusThread = threadScheduler[indexMinus];

                            if (copy.CompareTo(minusThread) < 0)
                            {
                                minusThread.SchedulableIndex++;
                                threadScheduler[owner.SchedulableIndex] = minusThread;
                                threadScheduler[indexMinus] = copy;
                                owner.SchedulableIndex--;
                                indexMinus = owner.SchedulableIndex - 1;
                            }
                            else
                                break;
                        }
                    }

                    isBuilding = False;
                }
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
