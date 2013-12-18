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
using SCPM.Threading;
using SCPM.Scheduling;

namespace SCPM.Interfaces
{
    /// <summary>
    /// Describes a thread scheduller.
    /// </summary>
    public interface IThreadScheduler
    {
        /// <summary>
        /// Creates a thread scheduller using the specified thread array.
        /// </summary>
        /// <param name="threads"></param>
        void Create(ISchedulableThread[] threads);
        void Reschedule(ISchedulableThread caller, SchedulerAction action);

        ISchedulableThread GetBestThread();
        ISchedulableThread GetScheduledThread();
        ISchedulableThread GetWorstThread();

        void Add(ISchedulableThread thread);
        void Remove(ISchedulableThread thread);

        int ThreadCount { get; set; }
        bool IsCreated { get; set; }
        string Name { get; }
        
    }
}
