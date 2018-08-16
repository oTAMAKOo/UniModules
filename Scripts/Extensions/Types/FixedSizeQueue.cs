
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Extensions
{
    /// <summary>
    /// 固定長のQueue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedQueue<T> : System.Collections.Generic.Queue<T>
    {
        //----- params -----

        public const int DefaultLength = 4096;

        //----- field -----

        protected int length = 0;

        //----- property -----

        public int Length { get { return this.length; } }

        //----- method -----

        public FixedQueue()
        {
            this.length = DefaultLength;
        }

        public FixedQueue(int length)
        {
            this.length = length;
        }

        public new void Enqueue(T item)
        {
            if (length <= Count)
            {
                Dequeue();
            }

            base.Enqueue(item);
        }
    }
}
