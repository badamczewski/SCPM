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


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        [TestMethod()]
        public void QueueWorkItemCorrectnessTest()
        {
            var time = DoTestsSmart();
            Console.WriteLine(time);

            Assert.AreEqual(cntH + cntL, MAX);

            int count = final.Count(x => x <= 0);

            Assert.IsTrue(count == 0);
        }

        private static ManualResetEvent ev = new ManualResetEvent(false);
        private static int MAX = 800;
        private static int CountFast1 = 50000;
        private static int CountFast2 = 900000;
        private static List<int> taskList = new List<int>();
        private static int[] final = new int[MAX];


        private static int cntH = 0;
        private static int cntL = 0;

        private static long DoTestsSmart()
        {
            ev = new ManualResetEvent(false);

            cntH = 0;
            cntL = 0;

            Console.WriteLine("SmartThreadPool GO:");

            Stopwatch w = new Stopwatch();
            w.Start();

            for (int i = 0; i < MAX; i++)
            {
                if (i % 5 == 0)
                    SmartThreadPool.QueueWorkItem(new Action<object>(ComputeSlow), i);
                else
                    SmartThreadPool.QueueWorkItem(new Action<object>(ComputeFast), i);
            }

            ev.WaitOne();

            w.Stop();
            Console.WriteLine("SmartThreadPool: " + w.ElapsedMilliseconds);
            ev.Reset();

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
            taskList.Add((int)o);

            if (cntL + cntH >= MAX)
            {
                Thread.Sleep(100);
                Console.WriteLine(cntL);
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
            taskList.Add((int)o);

            if (cntH + cntL >= MAX)
            {
                Thread.Sleep(100);
                Console.WriteLine(cntL);
                Console.WriteLine(cntH);
                ev.Set();
            }
        }
    }
}
