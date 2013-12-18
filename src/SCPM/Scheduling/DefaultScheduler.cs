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
