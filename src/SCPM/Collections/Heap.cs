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
using System.Text;
using SCPM.Threading;

namespace SCPM.Collections
{
    /// <summary>
    /// Represents a heap data structure for threads.
    /// </summary>
    /// <remarks>
    /// This heap is not a general purpose data structure as
    /// it's just easier to maitain a specialized heap for threads then try
    /// to fit a generic adaptable DS.
    /// </remarks>
    public class Heap
    {
        internal ISchedulableThread[] items;

        private ISchedulableThread max;
        private int size;

        public bool IsBuilding
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the heap that takes the input array, to construct the heap.
        /// </summary>
        /// <param name="array">Generic array.</param>
        public Heap(ISchedulableThread[] array)
        {
            this.items = array;
            if (array.Length != 0)
                max = this.items[0];
            size = array.Length;
            BuildHeap();
        }

        /// <summary>
        /// Deletes the minimum element from the heap.
        /// </summary>
        /// <returns></returns>
        public ISchedulableThread DeleteMin()
        {
            //get min and ovveride it, with the next array element. 
            ISchedulableThread min = items[0];
            items[0] = items[--size];

            //build the heap down.
            BuildDown(0);

            return min;
        }

        public void Remove(ISchedulableThread thread)
        {
            int cnt = 0;
            ISchedulableThread[] shortItems = new ISchedulableThread[--size];

            //the linear search in this context should not be to big of a problem,
            //as if we join then this means that probably our queue is empty so we should
            //be somwhere in the top of the tree.
            foreach (ISchedulableThread currentThread in items)
            {
                if (currentThread.ThreadId != thread.ThreadId)
                {
                    shortItems[cnt] = currentThread;
                    cnt++;
                }
            }

            items = shortItems;
        }

        /// <summary>
        /// Reads the minimum element from the heap.
        /// </summary>
        /// <returns>the minimum element.</returns>
        public ISchedulableThread ReadMin()
        {
            return items[0];
        }

        /// <summary>
        /// Reads the maximum element from the heap.
        /// </summary>
        /// <returns>the maximum element.</returns>
        public ISchedulableThread ReadMax()
        {
            return max;
        }

        /// <summary>
        /// Set's a new heap value.
        /// </summary>
        /// <param name="newSize">new size.</param>
        public void SetSize(int newSize)
        {
            size = newSize;
        }

        /// <summary>
        /// Get's the heap size.
        /// </summary>
        /// <returns>size.</returns>
        public int GetSize()
        {
            return size;
        }

        /// <summary>
        /// Inserts a new item into the heap.
        /// </summary>
        /// <param name="item">item to be added.</param>
        public void Insert(ISchedulableThread item)
        {
            if (size == items.Length)
                Expand();

            int k = ++size - 1;

            items[k] = item;

            if (max == null)
                max = item;

            BuildUp(k);
        }

        /// <summary>
        /// Updates the specified value at k-th element.
        /// </summary>
        /// <param name="k">the element index to update.</param>
        /// <param name="item">the specified item value.</param>
        public void Update(int k, ISchedulableThread item)
        {
            items[k] = item;

            // start as a parent as check for build up is faster.
            int childOrParrent = (k - 1) / 2;

            // first check if we should build up or down.
            if (CheckAndBuildUp(k, childOrParrent, item))
                return;

            childOrParrent = k * 2 + 1;

            // check the left child.
            if (CheckAndBuildDown(k, childOrParrent, item))
                return;

            // check the right child.
            childOrParrent++;
            if (CheckAndBuildDown(k, childOrParrent, item))
                return;
        }

        /// <summary>
        /// Builds the heap.
        /// </summary>
        public void BuildHeap()
        {
            IsBuilding = true;

            for (int k = size / 2; k >= 0; k--)
                BuildDown(k);

            IsBuilding = false;
        }

        /// <summary>
        /// Builds the heap down.
        /// </summary>
        /// <param name="k">the k-th element to start from.</param>
        public void BuildDown(int k)
        {
            int child = k;

            for (; 2 * child + 1 < size; k = child)
            {
                // local subtree root 
                ISchedulableThread root = items[k];

                // left child.
                child = 2 * k + 1;

                // if left child is bigger then the right then pick the right.
                if (child != size - 1 && items[child].CompareTo(items[child + 1]) > 0)
                    child++;


                // now compare with root
                if (root.CompareTo(items[child]) > 0)
                {
                    if (max.CompareTo(items[child]) < 0)
                        max = items[child];

                    // swamp
                    items[k] = items[child];
                    items[child] = root;
                    items[k].SchedulableIndex = k;
                    items[child].SchedulableIndex = child;
                }
            }
        }

        /// <summary>
        /// Checks if k-th element is smaller then it's parent and builds up
        /// if that yeilds true.
        /// </summary>
        /// <param name="k">k-th element.</param>
        /// <param name="parent">the parent.</param>
        /// <param name="item">the value of k-th element.</param>
        /// <returns>boolean value indicating that the heap was build up.</returns>
        private bool CheckAndBuildUp(int k, int parent, ISchedulableThread item)
        {
            if (parent > 0 && item.CompareTo(items[parent]) < 0)
            {
                items[k] = items[parent];
                items[parent] = item;

                items[k].SchedulableIndex = k;
                items[parent].SchedulableIndex = parent;

                BuildUp(parent);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if k-th element is bigger then it's parent and builds down
        /// if that yeilds true.
        /// </summary>
        /// <param name="k">k-th element.</param>
        /// <param name="parent">the parent.</param>
        /// <param name="item">the value of k-th element.</param>
        /// <returns>boolean value indicating that the heap was build down.</returns>
        private bool CheckAndBuildDown(int k, int child, ISchedulableThread item)
        {
            if (child != items.Length && child < items.Length && item.CompareTo(items[child]) > 0)
            {
                // swamp
                items[k] = items[child];
                items[child] = item;

                items[k].SchedulableIndex = k;
                items[child].SchedulableIndex = child;

                BuildDown(child);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Builds the heap up.
        /// </summary>
        /// <param name="k">the k-th element to start from.</param>
        private void BuildUp(int k)
        {
            int parent = k;

            for (; k > 1; k = parent)
            {
                // local subtree root
                ISchedulableThread root = items[k];

                parent = (k - 1) / 2;

                // check my parent, if im smaller then him then swamp
                if (root.CompareTo(items[parent]) > 0)
                {
                    if (max.CompareTo(items[parent]) <= 0)
                        max = items[parent];

                    items[k] = items[parent];
                    items[parent] = root;


                    items[k].SchedulableIndex = k;
                    items[parent].SchedulableIndex = parent;
                }
            }
        }

        /// <summary>
        /// Expands the heap array.
        /// </summary>
        private void Expand()
        {
            ISchedulableThread[] oldItems = items;

            int len = items.Length;

            if (len == 0)
                len = 1;
            else
                len *= 2;

            items = new ISchedulableThread[len];

            if (oldItems.Length != 0)
                Array.Copy(oldItems, items, size);
        }
    }
}
