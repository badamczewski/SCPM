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

using SCPM.Interfaces;
using SCPM.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SCPM
{
    public static class Configuration
    {
        static Configuration()
        {}

        public static ThreadPriority SmartThreadDefaultPrority = ThreadPriority.Normal;
        public static int SmartThreadWaitSpinLimit = 20;
        public static int SmartThreadWaitSpinTime = 30;
        public static int SmartThreadStealCondition = 3;

        public static int FiberThreadWaitSpinLimit = 20;
        public static int FiberThreadWaitSpinTime = 30;

        public static IThreadScheduler SmartPoolScheduler = DefaultThreadScheduler.Create();
        public static IThreadScheduler FiberPoolScheduler = DefaultThreadScheduler.Create();
        public static IThreadScheduler ArtificialThreadScheduler = HeapThreadScheduler.Create();
    }
}



