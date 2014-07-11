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

using SCPM.Interfaces;
using SCPM.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SCPM
{
    /// <summary>
    /// This is the public global configuration class that
    /// all of the internals can be configured.
    /// </summary>
    public static class Configuration
    {
        static Configuration()
        {}

        public static ThreadPriority SmartThreadDefaultPrority = ThreadPriority.Normal;
        public static int SmartThreadWaitSpinLimit = 20;
        public static int SmartThreadWaitSpinTime = 30;
        public static int SmartThreadStealCondition = 2;

        public static int FiberThreadWaitSpinLimit = 20;
        public static int FiberThreadWaitSpinTime = 30;

        public static IThreadScheduler SmartPoolScheduler = SequentialSeekThreadScheduler.Create();
        public static IThreadScheduler FiberPoolScheduler = SequentialSeekThreadScheduler.Create();
        public static IExpandableThreadScheduler ArtificialThreadScheduler = HeapThreadScheduler.Create();

        public static bool SmartThreadPoolPreLoadThreads = true;
    }
}



