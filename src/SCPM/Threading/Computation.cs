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
using System.Threading;

namespace SCPM.Threading
{
    public sealed class Computation<T> : IComputation
    {
        private Action<T> action;
        private IWorkScheduler scheduler;
        private ManualResetEvent wait;
        private bool isInternalComputation;
        private T state;

        public Computation(Action<T> action)
        {
            this.action = action;
            this.scheduler = DefaultWorkScheduler.Scheduler;
        }

        internal Computation(Action<T> action, T state, bool isInternal) : this(action)
        {
            this.state = state;
            this.isInternalComputation = isInternal;
        }

        public void Run(T state)
        {
            this.state = state;
            this.scheduler.Queue(this);
        }

        object IComputation.Execute()
        {
            action(state);

            //Internal computation doesn't need to set the event.
            if (!isInternalComputation && wait != null)
                wait.Set();

            return null;
        }

        public void WaitForCompletion()
        {
            if (wait == null)
                wait = new ManualResetEvent(false);

            wait.WaitOne();
            wait.Close();
        }


        public string ComputationType
        {
            get { return Resources.Computation_Generic; }
        }
    }
}
