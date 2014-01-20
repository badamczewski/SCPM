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
    public class LockFreeQueue<T> : IEnumerable<T>
    {
        class Node
        {
            internal T m_val;
            internal Node m_next;
        }

        private Node m_head;
        private Node m_tail;

        public LockFreeQueue()
        {
            m_head = m_tail = new Node();
        }

        public int Count
        {
            get
            {
                int count = 0;
                for (Node curr = m_head.m_next;
                    curr != null; curr = curr.m_next) count++;
                return count;
            }
        }

        public bool IsEmpty
        {
            get { return m_head.m_next == null; }
        }

        private Node GetTailAndCatchUp()
        {
            Node tail = m_tail;
            Node next = tail.m_next;

            // Update the tail until it really points to the end.
            while (next != null)
            {
                Interlocked.CompareExchange(ref m_tail, next, tail);
                tail = m_tail;
                next = tail.m_next;
            }

            return tail;
        }

        public void Enqueue(T obj)
        {
            // Create a new node.
            Node newNode = new Node();
            newNode.m_val = obj;

            // Add to the tail end.
            Node tail;
            do
            {
                tail = GetTailAndCatchUp();
                newNode.m_next = tail.m_next;
            }
            while (Interlocked.CompareExchange(
                ref tail.m_next, newNode, null) != null);

            Interlocked.CompareExchange(ref m_tail, newNode, tail);
        }

        public T Dequeue()
        {
            T val = default(T);

            while (true)
            {
                Node head = m_head;
                Node next = head.m_next;

                if (next == null)
                {
                    val = default(T);
                }
                else
                {
                    if (Interlocked.CompareExchange(
                        ref m_head, next, head) == head)
                    {
                        val = next.m_val;
                    }
                }
            }
        }

        public bool TryPeek(out T val)
        {
            Node curr = m_head.m_next;

            if (curr == null)
            {
                val = default(T);
                return false;
            }
            else
            {
                val = curr.m_val;
                return true;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            Node curr = m_head.m_next;
            Node tail = GetTailAndCatchUp();

            while (curr != null)
            {
                yield return curr.m_val;

                if (curr == tail)
                    break;

                curr = curr.m_next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
