using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCPM.Interfaces;
using SCPM.Threading;

namespace SCPM.Scheduling
{
    public interface IExpandableThreadScheduler : IThreadScheduler
    {
        void Add(ISchedulableThread thread);
        void Remove(ISchedulableThread thread);
    }
}
