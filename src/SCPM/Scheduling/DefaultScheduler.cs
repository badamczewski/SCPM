/*
Copyright (c) 2013-2014 Contributors as noted in the AUTHORS file

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

using System;
using System.Collections.Generic;
using System.Text;
using SCPM.Collections;
using SCPM.Interfaces;
using SCPM.Threading;

namespace SCPM.Scheduling
{
    public sealed class DefaultScheduler
    {
        private static readonly DefaultScheduler instance = new DefaultScheduler(); 
        private static Heap<SmartThread> threadScheduler;
        private int bestThCnt = 0;

        static DefaultScheduler()
        { }

        private DefaultScheduler()
        {
        }

        public void Create(SmartThread[] threads)
        {
            threadScheduler = new Heap<SmartThread>(threads);
            IsCreated = true;
        }

        public SmartThread GetScheduledThread()
        {
            if (bestThCnt > Int16.MaxValue)
                bestThCnt = 0;

            return threadScheduler.items[bestThCnt++ % 3];
        }

        public void Reschedule()
        {
            if (threadScheduler.IsBuilding == false)
                threadScheduler.BuildHeap();
        }

        public SmartThread GetBestThread()
        {
            return threadScheduler.ReadMin();
        }

        public SmartThread GetWorstThread()
        {
            return threadScheduler.ReadMax();
        }

        public bool IsCreated { get; set; }

        public static DefaultScheduler Scheduler { get { return instance; } }
    }
}
