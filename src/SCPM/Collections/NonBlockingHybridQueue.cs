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
using System.Threading;
using System.Collections;

namespace SCPM.Collections
{
    /// <summary>
    /// Represents a hybrid queue, that uses non blocking (spining) techniques to achive thread safety.
    /// </summary>
    /// <remarks>
    /// The queue is hybrid due it's functionality that it can dequeue second last node, which makes it
    /// perfect for certain set of alghoritms, like work stealing. 
    /// </remarks>
    /// <typeparam name="T">generic Typeparam.</typeparam>
    public class NonBlockingHybridQueue<T> : IEnumerable<T>, IStealingQueue<T>
    {
        /// <summary>
        /// Internal node class for the use of internal double linked list structure.
        /// </summary>
        private class Node
        {
            public T val;
            public Node next;
            public Node prev;
            public int id;
        }

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
        private volatile Node head;
        private volatile Node tail;

        /// <summary>
        /// Initializes a new instance of a hybrid queue.
        /// </summary>
        public NonBlockingHybridQueue()
        {
            head = new Node();
            tail = new Node();
            head = tail;
        }

        /// <summary>
        /// Gets the Unsafe Count (A count that will not nesserly provide the correct actual value). 
        /// </summary>
        public int UnsafeCount
        {
            get
            {
                return tail.id - head.id;
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                EvaluateCount((x) => false, out count);
                return count;
            }
        }

        /// <summary>
        /// Stars counting nodes utils a certain condition has been met.
        /// </summary>
        /// <param name="value">the confiiton.</param>
        /// <returns>the value indication that the condition was met or not.</returns>
        public bool EvaluateCount(Predicate<int> value)
        {
            int count = 0;
            return EvaluateCount(value, out count); 
        }

        /// <summary>
        /// Stars counting nodes utils a certain condition has been met.
        /// </summary>
        /// <param name="value">the confiiton.</param>
        /// <param name="actualCount">the actual counted number of elements.</param>
        /// <returns>the value indication that the condition was met or not.</returns>
        private bool EvaluateCount(Predicate<int> value, out int actualCount)
        {
            int count = 0;
            for (Node current = head.next;
                current != null; current = current.next)
            {
                count++;

                if (value(count))
                {
                    actualCount = count;
                    return true;
                }
            }
            actualCount = count;
            return false;
        }

        /// <summary>
        /// Get's the value indicating if the Queue is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return head.next == null; }
        }

        /// <summary>
        /// Get's the tail.
        /// </summary>
        /// <remarks>
        /// In order to achieve correctness we need to keep track of the tail,
        /// accessing tail.next will not do as some other thread might just moved it
        /// so in order to catch the tail we need to do a subtle form of a spin lock
        /// that will use CompareAndSet atomic instruction ( Interlocked.CompareExchange )
        /// and set ourselvs to the tail if it had been moved.
        /// </remarks>
        /// <returns>Tail.</returns>
        private Node GetTail()
        {
            Node localTail = tail;
            Node localNext = localTail.next;

            //if some other thread moved the tail we need to set to the right possition.
            while (localNext != null)
            {
                //set the tail.
                Interlocked.CompareExchange(ref tail, localNext, localTail);
                localTail = tail;
                localNext = localTail.next;
            }

            return tail;
        }

        /// <summary>
        /// Attempts to reset the Couner id.
        /// </summary>
        private void TryResetCounter()
        {
            if (tail.id >= Int16.MaxValue)
            {
                int res = (tail.id - head.id);
                head.id = 0;
                tail.id = res;
            }
        }

        /// <summary>
        /// Puts a new item on the Queue.
        /// </summary>
        /// <param name="obj">The value to be queued.</param>
        public void Enqueue(T obj)
        {
            Node localTail = null;
            Node newNode = new Node();
            newNode.val = obj;

            TryResetCounter();

            do
            {
                //get the tail.
                localTail = GetTail();

                //TODO: This should be atomic.
                newNode.next = localTail.next;
                newNode.id = localTail.id + 1;
                newNode.prev = localTail;
            }
            // if we arent null, then this means that some other
            // thread interffered with our plans (sic!) and we need to 
            // start over.
            while (Interlocked.CompareExchange(
                ref localTail.next, newNode, null) != null);
            // if we finally are at the tail and we are the same,
            // then we switch the values to the new node, phew! :)
            Interlocked.CompareExchange(ref tail, newNode, localTail);
        }

        /// <summary>
        /// Gets the first element in the queue.
        /// </summary>
        /// <returns>Head element.</returns>
        public T Dequeue()
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
                //get the tail.
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

                // Set the swap node values that will exchange the element
                // in a sense that it will skip right through it.
                swapNode.next = localTail.next;
                swapNode.prev = localPrev.prev;
                swapNode.val = localPrev.val;
                swapNode.id = localPrev.id;
            }
            // In order for this to be actualy *thread safe* we need to subscribe ourselfs
            // to the same logic as the enque and create a blockade by setting the next value
            // of the tail!
            while (Interlocked.CompareExchange(ref localTail.next, localTail, null) != null);      

            // do a double exchange, if we get interrupted between we should be still fine as,
            // all we need to do after the first echange is to swing the prev element to point at the
            // correct tail.
            Interlocked.CompareExchange(ref tail, swapNode, tail);
            Interlocked.CompareExchange(ref tail.prev.next, swapNode, tail.prev.next);

            return localTail.val;
        }

        /// <summary>
        /// Tries to peek the next value in the queue without 
        /// getting it out.
        /// </summary>
        /// <param name="value">the output value.</param>
        /// <returns>the value indicating that there are still values to be peeked.</returns>
        public bool TryPeek(out T value)
        {
            Node currentNode = head.next;

            if (currentNode == null)
            {
                value = default(T);
                return false;
            }
            else
            {
                value = currentNode.val;
                return true;
            }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            Node currenNode = head.next;
            Node localTail = GetTail();

            while (currenNode != null)
            {
                yield return currenNode.val;

                if (currenNode == localTail)
                    break;

                currenNode = currenNode.next;
            }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
