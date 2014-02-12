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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SCPM.Threading;
using System.Threading;
using SCPM.Exceptions;

namespace SCPM.Tests.Threading
{
    [TestClass]
    public class CompuationTest
    {
        [TestMethod]
        public void ComputationShouldWaitOnCompletion()
        {
            Computation<int> computation = new Computation<int>((x) => { Console.WriteLine(++x); });
            int state = 0;
            computation.Run(state);
            computation.WaitForCompletion();

            Assert.IsTrue(computation.Cookie.WasWaitingForCompletion);
            Assert.IsTrue(computation.Cookie.Completed);
        }

        [TestMethod]
        public void ComputationShouldRun()
        {
            Computation<int> computation = new Computation<int>((x) => { Thread.Sleep(100); Console.WriteLine(x); });
            int state = 0;
            computation.Run(state);

            Assert.IsFalse(computation.Cookie.WasWaitingForCompletion);
            Assert.IsFalse(computation.Cookie.Completed);

            Thread.Sleep(200);

            Assert.IsTrue(computation.Cookie.Completed);
        }

        [TestMethod]
        public void ComputationShouldRunWithExceptionAndHandleIt()
        {
            Computation<int> computation = new Computation<int>((x) => { int zero = 0; int res = x / zero; Console.WriteLine(res); });

            try
            {    
                int state = 0;
                computation.Run(state);
            }
            catch (ComputationException ex)
            {
                Assert.IsTrue(computation.Cookie.IsException);
                Assert.AreEqual(computation.Cookie.Exception, ex);

                Assert.IsInstanceOfType(ex.InnerException, typeof(DivideByZeroException));
            }
        }

        [TestMethod]
        public void ComputationShouldWaitOnCompletionWhileCreatatedFromStatic()
        {
            var computation = Computation.Create<int>(((x) => { Console.WriteLine(++x); }));

            int state = 0;
            computation.Run(state);
            computation.WaitForCompletion();

            Assert.IsTrue(computation.Cookie.WasWaitingForCompletion);
            Assert.IsTrue(computation.Cookie.Completed);
        }

        [TestMethod]
        public void ComputationThatIsLongRunningShouldRun()
        {
            Computation<int> computation = new Computation<int>((x) => { Thread.Sleep(100); Console.WriteLine(x); }, ComputationExecutionType.LongRunning);
            int state = 0;
            computation.Run(state);

            Assert.IsFalse(computation.Cookie.WasWaitingForCompletion);
            Assert.IsFalse(computation.Cookie.Completed);

            Thread.Sleep(200);

            Assert.IsTrue(computation.Cookie.Completed);
        }


    }
}
