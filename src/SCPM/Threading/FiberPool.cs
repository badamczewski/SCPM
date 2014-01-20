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
