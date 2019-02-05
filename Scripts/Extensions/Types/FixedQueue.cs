
using System;
using System.Linq;
using UniRx;

namespace Extensions
{
    /// <summary> 固定長のQueue </summary>
    public class FixedQueue<T> : System.Collections.Generic.Queue<T>
    {
        //----- params -----

        public const int DefaultLength = 4096;

        //----- field -----

        private int length = 0;

        private Subject<T> onExtruded = null;

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
                var dequeuedItem = Dequeue();

                if(onExtruded != null)
                {
                    onExtruded.OnNext(dequeuedItem);
                }
            }

            base.Enqueue(item);
        }

        public void Remove(T item)
        {
            var items = this.Where(x => !x.Equals(item)).ToArray();

            Clear();

            foreach (var obj in items)
            {
                Enqueue(obj);
            }
        }

        public IObservable<T> OnExtrudedAsObservable()
        {
            return onExtruded ?? (onExtruded = new Subject<T>());
        }
    }
}
