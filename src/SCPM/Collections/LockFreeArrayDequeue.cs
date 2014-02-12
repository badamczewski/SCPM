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
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace SCPM.Collections
{
    /// <summary>
    /// Represents a LockFree work stealing queue, that uses non blocking (spining) techniques to achive thread safety.
    /// </summary>
    /// <remarks>
    /// This queue is recomended for use cases where a very stable fixed timed outputs are generated. 
    /// it yields very stable perfomance without any spikes.
    /// </remarks>
    /// <typeparam name="T">generic Typeparam.</typeparam>
    public class LockFreeDequeue<T> : IStealingQueue<T>
    {
        private const int len = 64;
        private int mask = len - 1;
  
        private T[] array = new T[len];

        private volatile int head = 0;
        private volatile int tail = 0;

        private int tailLocker = 0;
        private int headLocker = 0;

        public void Enqueue(T value)
        {
            while (true)
            {
                int local = tailLocker;
                if (Interlocked.CompareExchange(ref tailLocker, 1, local) == 0)
                {
                    int count = tail - head;

                    if (count < mask)
                    {
                        array[tail++ & mask] = value;
                        Interlocked.Exchange(ref tailLocker, 0);
                        return;
                    }
                    else
                    {
                        //Double the size.
                        T[] newArr = new T[array.Length << 1];

                        for (int i = 0; i < count; i++)
                            newArr[i] = array[(i + head) & mask];

                        array = newArr;
                        head = 0;
                        tail = count;
                        mask = (mask << 1) | 1;

                        array[tail++ & mask] = value;
                        Interlocked.Exchange(ref tailLocker, 0);
                        return;
                    }
                }
            }
        }

        public T Dequeue()
        {
            while (true)
            {
                int local = headLocker;
                if (Interlocked.CompareExchange(ref headLocker, 1, local) == 0)
                {
                    T value = array[head++ & mask];
                    Interlocked.Exchange(ref headLocker, 0);
                    return value;
                }
            }
        }

        public T DequeueLast()
        {
            while (true)
            {
                int local = tailLocker;
                if (Interlocked.CompareExchange(ref tailLocker, 1, local) == 0)
                {
                    T value = array[--tail & mask];
                    Interlocked.Exchange(ref tailLocker, 0);
                    return value;
                }
                else
                {
                    local = headLocker;

                    if (Interlocked.CompareExchange(ref headLocker, 1, local) == 0)
                    {
                        T value = array[head++ & mask];
                        Interlocked.Exchange(ref headLocker, 0);
                        return value;
                    }
                }
            }
        }

        public bool IsEmpty
        {
            get { return tail <= head; }
        }

        public int Count
        {
            get { return tail - head; }
        }

        public int UnsafeCount
        {
            get { return tail - head; }
        }
    }
}
