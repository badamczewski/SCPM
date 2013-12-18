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
