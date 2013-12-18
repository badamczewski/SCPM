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
using SCPM.Interfaces;
using SCPM.Scheduling;

namespace SCPM.Threading
{
    public static class FiberPool
    {
        private static readonly IThreadScheduler fiberScheduler = null;
        private static int fiberThreadCount = Environment.ProcessorCount / 2;

        static FiberPool()
        {
            ISchedulableThread[] threads = new ISchedulableThread[fiberThreadCount];

            for (int i = 0; i < fiberThreadCount; i++)
                threads[i] = new FiberThread();

            fiberScheduler = Configuration.FiberPoolScheduler;
            fiberScheduler.Create(threads);
        }

        public static void QueueWorkItem(IComputation computation)
        {
            ISchedulableThread fiber = fiberScheduler.GetScheduledThread();

            if (!fiber.IsStarted)
                fiber.Start();

            //This code was commented out as upon fiber framework start we are creating
            //up front many fiber threads therefor it's pointless to exmpand even more
            //as it's hard to define a criteria how to expand.
            //
            //if (((FiberThread)fiber).ComputationCount >= nextFiberThreashold)
            //    fiberScheduler.Add(new FiberThread());

            fiber.Execute(computation);    
        }
    }
}
