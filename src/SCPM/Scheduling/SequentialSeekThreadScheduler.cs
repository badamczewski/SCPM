using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCPM.Interfaces;
using SCPM.Threading;
using System.Threading;

namespace SCPM.Scheduling
{
    /// <summary>
    /// Represents a sequential seek scheduller that tries to find best and worst
    /// threads by sequentially reading the thread array.
    /// 
    /// This scheduler is very efective for small thread arrays (arround 128 HW threads),
    /// and when uniform workloads are needed, with some deviation from the ideal solution and scheduling speed needs to be fast 
    /// (holds no locks).
    /// </summary>
    public sealed class SequentialSeekThreadScheduler : IThreadScheduler
    {
        private const string name = "SequentialSeekThreadScheduler";
        private ISchedulableThread[] threadScheduler;

        private ISchedulableThread bestThread = null;
        private ISchedulableThread worstThread = null;

        public bool IsCreated { get; set; }
        public string Name { get { return name; } }
        public int ThreadCount { get; set; }

        public static SequentialSeekThreadScheduler Create()
        {
            return new SequentialSeekThreadScheduler();
        }

        public void Create(Threading.ISchedulableThread[] threads)
        {
            threadScheduler = threads;

            ThreadCount = threadScheduler.Length;

            bestThread = threadScheduler[0];
            worstThread = threadScheduler[0];

            IsCreated = true;
        }

        public void Reschedule(Threading.ISchedulableThread caller, SchedulerAction action)
        {
            var scheduler = threadScheduler;

            int min = scheduler[0].Count;
            int minId = 0;

            int max = scheduler[0].Count;
            int maxId = 0;

            for (int i = 0; i < scheduler.Length; i++)
            {
                var cnt = scheduler[i].Count;

                if (min > cnt)
                {
                    min = cnt;
                    minId = i;
                }
                else if (max <= cnt)
                {
                    max = cnt;
                    maxId = i;
                }
            }
            bestThread = scheduler[minId];
            worstThread = scheduler[maxId];
        }

        public Threading.ISchedulableThread GetBestThread()
        {
            return bestThread;
        }

        public Threading.ISchedulableThread GetScheduledThread()
        {
            return bestThread;
        }

        public Threading.ISchedulableThread GetWorstThread()
        {
            return worstThread;
        }
    }
}
