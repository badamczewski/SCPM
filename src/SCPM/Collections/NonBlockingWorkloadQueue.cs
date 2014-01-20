/*
Copyright (c) 2013-2014 Contributors as noted in the AUTHORS file

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

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;

namespace SCPM.Collections
{
    /// <summary>
    /// Represents a hybrid queue, that uses non blocking (spining) techniques to achive thread safety.
    /// </summary>
    /// <typeparam name="T">generic Typeparam.</typeparam>
    public class NonBlockingWorkloadQueue<T> : NonBlockingHybridQueue<T>, IStealingQueue<T>
    {
        /*
         * You may ask yourself why volatile is here, well the
         * main reason for this when we don't do explicit locking
         * we don't get the memory barier safety so instructions
         * might get reordered.
         * 
         * NOTE: having volatile code in here is just a workaround
         * as we get a performance hit, instead we need to put mem bariers
         * when they are actually needed!
         */
        private volatile int ttlSum = 0;

        /// <summary>
        /// Initializes a new instance of a workload queue.
        /// </summary>
        public NonBlockingWorkloadQueue() : base() { }

        public new void Enqueue(T obj)
        {
            Enqueue(obj, Int16.MaxValue);
        }

        /// <summary>
        /// Puts a new item on the Queue.
        /// </summary>
        /// <param name="obj">The value to be queued.</param>
        public void Enqueue(T obj, int ttl)
        {
            Node localTail;
            Node newNode = new Node();
            newNode.val = obj;

            TryResetCounter();

            /*
             * This is not the best way to deal with int overflows
             * but it's the only fast way to do it.
             */
            if (ttlSum > Int16.MaxValue && head.next == null)
                ttlSum = 0;

            newNode.ttl = ttl;

            if (newNode.ttl != Int16.MaxValue)
                newNode.ttl += ttlSum;

            do
            {
                //get the tail.
                localTail = GetTail();

                //TODO: This should be atomic.
                newNode.next = localTail.next;
                newNode.prev = localTail;
                newNode.id = localTail.id + 1;
            }
            // if we arent null, then this means that some other
            // thread interffered with our plans (sic!) and we need to 
            // start over.
            while (Interlocked.CompareExchange(
                ref localTail.next, newNode, null) != null);

            // if we finally are at the tail and we are the same,
            // then we switch the values to the new node, phew! :)
            Interlocked.CompareExchange(ref tail, newNode, localTail);

            if (newNode.ttl != Int16.MaxValue)
                Interlocked.Increment(ref ttlSum);
        }

        /// <summary>
        /// Gets the first element in the queue.
        /// </summary>
        /// <returns>Head element.</returns>
        public new T Dequeue()
        {
            // keep spining until we catch the propper head.
            while (true)
            {
                Node localHead = head;
                Node localNext = localHead.next;

                // if the queue is empty then return the default for that
                // typeparam.
                if (localNext == null)
                {
                    return default(T);
                }
                else
                {
                    localNext.prev = localHead.prev;

                    // if no other thread changed the head then we are good to
                    // go and we can return the local value;
                    if (Interlocked.CompareExchange(
                        ref head, localNext, localHead) == localHead)
                    {
                        if (localNext.ttl != Int16.MaxValue)
                        {
                            //skip ttl nodes that expired.
                            if (localNext.ttl - ttlSum <= 0)
                                continue;

                            Interlocked.Increment(ref ttlSum);
                        }
                        return localNext.val;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the last element in the queue.
        /// </summary>
        /// <returns>old tail element.</returns>
        public T DequeueLast()
        {
            Node localTail;
            Node localPrev;
            Node swapNode = new Node();

            do
            {
                localTail = GetTail();
                localPrev = localTail.prev;

                if (localPrev == null)
                    return default(T);
                else if (localPrev.prev == null)
                    return default(T);
                else if (localPrev.prev == head)
                    return default(T);
                else if (localTail == null)
                    return default(T);

                swapNode.next = localTail.next;
                swapNode.prev = localPrev.prev;
                swapNode.val = localPrev.val;
                swapNode.id = localPrev.id;

                if (localTail.ttl != Int16.MaxValue)
                {
                    //skip ttl nodes that expired.
                    if (localTail.ttl - ttlSum <= 0)
                        continue;

                    Interlocked.Increment(ref ttlSum);
                }
            }
            while (Interlocked.CompareExchange(ref localTail.next, localTail, null) != null);

            Interlocked.CompareExchange(ref tail, swapNode, tail);
            Interlocked.CompareExchange(ref tail.prev.next, swapNode, tail.prev.next);

            return localTail.val;
        }
    }
}
