using System;
using System.Collections.Generic;
using System.Text;
using SCPM.Threading;

namespace SCPM.Interfaces
{
    public interface IScheduler
    {
        void Create(SmartThread[] threads);
        SmartThread GetBestThread();
        SmartThread GetScheduledThread();
        SmartThread GetWorstThread();
        bool IsCreated { get; set; }
        void Reschedule();
    }
}
