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

using SCPM.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace SCPM.Tests
{
    /// <summary>
    ///This is a test class for SmartThreadPoolTest and is intended
    ///to contain all SmartThreadPoolTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SmartThreadPoolTest
    {
        [TestMethod()]
        public void SmartThreadPoolShouldBeCorrect()
        {
            var time = DoTestsSmart();
            Console.WriteLine(time);

            Assert.AreEqual(cntH + cntM + cntL, MAX);

            int count = final.Count(x => x <= 0);

            Assert.IsTrue(count == 0);
        }

        private static ManualResetEvent ev = new ManualResetEvent(false);

        private static int MAX = 1245;
        private static int CountFast1 = 51100;
        private static int CountFast2 = 912400;
        private static int CountFast3 = 214010;
        private static List<int> taskList = new List<int>();
        private static int[] final = new int[MAX];
        private static readonly object locker = new object();


        private static int cntH = 0;
        private static int cntL = 0;
        private static int cntM = 0;


        private static long DoTestsSmart()
        {
            ev = new ManualResetEvent(false);

            cntH = 0;
            cntL = 0;
            cntM = 0;

            Console.WriteLine("SmartThreadPool GO:");

            Random r = new Random();
            int rand = r.Next(0, 10);

            Stopwatch w = new Stopwatch();
            w.Start();

            for (int i = 0; i < MAX; i++)
            {
                if (i % 9 == 0)
                    rand = r.Next(0, 29);

                if (i % 5 + rand == 0)
                    SmartThreadPool.QueueWorkItem(new Action<object>(ComputeSlow), i);
                else if (i % 3 + rand == 0)
                    SmartThreadPool.QueueWorkItem(new Action<object>(ComputeMid), i);
                else
                    SmartThreadPool.QueueWorkItem(new Action<object>(ComputeFast), i);
            }

            ev.WaitOne();

            w.Stop();
            Console.WriteLine("SmartThreadPool: " + w.ElapsedMilliseconds);
            ev.Reset();

            Thread.Sleep(5000);

            foreach (var item in taskList)
            {
                final[item]++;
            }

            foreach (var item in final)
            {
                Console.Write(item + ", ");
            }

            return w.ElapsedMilliseconds;
        }

        private static void ComputeFast(object o)
        {

            string s = string.Empty;
            List<int> l = new List<int>();

            for (int i = 0; i < CountFast1 - (int)o; i++)
            {
                l.Add(i);
                s = l[i != 0 ? i - 1 : i] + i.ToString();
            }

            Interlocked.Increment(ref cntL);

            lock (locker)
            {
                taskList.Add((int)o);
            }

            if (cntH + cntL + cntM >= MAX)
            {
                Thread.Sleep(100);
                Console.WriteLine(cntL);
                Console.WriteLine(cntM);
                Console.WriteLine(cntH);
                ev.Set();
            }
        }

        private static void ComputeSlow(object o)
        {
            string s = string.Empty;
            List<int> l = new List<int>();

            for (int i = 0; i < CountFast2 + (int)o; i++)
            {
                l.Add(i);
                s = l[i != 0 ? i - 1 : i] + i.ToString();
            }

            Interlocked.Increment(ref cntH);

            lock (locker)
            {
                taskList.Add((int)o);
            }


            if (cntH + cntL + cntM >= MAX)
            {
                Thread.Sleep(100);
                Console.WriteLine(cntL);
                Console.WriteLine(cntM);
                Console.WriteLine(cntH);
                ev.Set();
            }
        }

        private static void ComputeMid(object o)
        {
            string s = string.Empty;
            List<int> l = new List<int>();

            for (int i = 0; i < CountFast3 + (int)o; i++)
            {
                l.Add(i);
                s = l[i != 0 ? i - 1 : i] + i.ToString();
            }

            Interlocked.Increment(ref cntM);

            lock (locker)
            {
                taskList.Add((int)o);
            }

            if (cntH + cntL + cntM >= MAX)
            {
                Thread.Sleep(100);
                Console.WriteLine(cntL);
                Console.WriteLine(cntM);
                Console.WriteLine(cntH);
                ev.Set();
            }
        }
    }
}
