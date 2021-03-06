﻿#region Licence
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
using SCPM.Threading;

namespace SCPM.Scheduling
{
    public class DefaultWorkScheduler : IWorkScheduler
    {
        private static readonly DefaultWorkScheduler scheduler = new DefaultWorkScheduler();

        private DefaultWorkScheduler() { }
        static DefaultWorkScheduler() { }

        public void Queue(IComputation computation)
        {
            if (computation.ComputationType == Resources.Computation_Fiber)
                FiberPool.QueueWorkItem(computation);
            else
            {
                if (computation.ExecutionType == ComputationExecutionType.LongRunning)
                {
                    SmartThread thread = new SmartThread(false, -1);
                    thread.Execute(computation);
                    thread.Start();
                }

                SmartThreadPool.QueueWorkItem(computation);
            }
        }

        public static DefaultWorkScheduler Scheduler
        {
            get { return scheduler; }
        }
    }
}
