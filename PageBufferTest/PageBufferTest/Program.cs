using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PageBufferTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var items = new PageBufferEnumerable<int>((index, pageSize) => Enumerable.Range(index, pageSize), 4, 10);

            Console.WriteLine("foreach (var item in items)");
            foreach (var item in items)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("");
            Console.WriteLine("foreach (var item in items.Skip(2).Take(6))");
            foreach (var item in items.Skip(2).Take(6))
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("");
            Console.WriteLine("foreach (var item in items.Skip(12).Take(2))");
            foreach (var item in items.Skip(12).Take(2))
            {
                Console.WriteLine(item);
            }

            TestPaging(items);
        }

        private static void TestPaging(PageBufferEnumerable<int> items)
        {
            Console.WriteLine("");
            Console.WriteLine("Paging ...");
            Console.WriteLine("");
            PrintPage(0, items.PageCount, items.Skip(0).Take(4));
            PrintPage(1, items.PageCount, items.Skip(4).Take(4));
            PrintPage(2, items.PageCount, items.Skip(8).Take(4));
        }

        private static void PrintPage(int page, int pageCount, IEnumerable<int> pageItems)
        {
            Console.WriteLine("Page: {0}", page);
            Console.Write("Items: ");
            foreach (var item in pageItems)
            {
                Console.Write(string.Format("{0}, ", item));
            }
            Console.WriteLine("");
            Console.WriteLine("Page count: {0}", pageCount);
            Console.WriteLine("");
        }
    }

    public class PageBufferEnumerable<T> : IEnumerable<T>
    {
        private Func<int, int, IEnumerable<T>> selector;
        public int PageSize { get; private set; }
        public int PageCount { get; private set; }
        private int count;

        public PageBufferEnumerable(Func<int, int, IEnumerable<T>> selector, int pageSize, int count)
        {
            this.selector = selector;
            this.PageSize = pageSize;
            this.count = count;
            this.PageCount = count / pageSize + (count % pageSize > 0 ? 1 : 0);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new PageBufferEnumerable<T>.Enumerator(selector, PageSize, count);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        class Enumerator : IEnumerator<T>
        {
            private Func<int, int, IEnumerable<T>> selector;
            private int pageSize;
            private int count;

            int currentIndex = -1;

            public Enumerator(Func<int, int, IEnumerable<T>> selector, int pageSize, int count)
            {
                this.selector = selector;
                this.pageSize = pageSize;
                this.count = count;
            }

            IEnumerator<T> bufferEnumerator;
            T IEnumerator<T>.Current
            {
                get {
                    if (this.currentIndex < count)
                    {
                        if (bufferEnumerator == null)
                        {
                            var pickCount = Math.Min(pageSize, count - currentIndex);
                            bufferEnumerator = selector(currentIndex, pickCount).GetEnumerator();
                            var moveNext = bufferEnumerator.MoveNext();
                            Debug.WriteLine("selector({0}, {1}):{2}", currentIndex, pickCount, moveNext);
                        }

                        var current = bufferEnumerator.Current;
                        Debug.WriteLine("Current:{0};[{1}]", current, currentIndex);
                        return current;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            void IDisposable.Dispose()
            {
                Debug.WriteLine("Dispose");

                if (bufferEnumerator != null)
                {
                    bufferEnumerator.Dispose();
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return ((IEnumerator<T>)this).Current; }
            }

            bool System.Collections.IEnumerator.MoveNext()
            {
                if (currentIndex < count - 1)
                {
                    if (bufferEnumerator != null && 
                        !bufferEnumerator.MoveNext())
                    {
                        bufferEnumerator.Dispose();
                        bufferEnumerator = null;
                    }

                    currentIndex++;
                    Debug.WriteLine("MoveNext():{0};[{1}]", true, currentIndex);
                    return true;
                }
                else
                {
                    Debug.WriteLine("MoveNext():{0};[{1}]", false, currentIndex);
                    return false;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                Debug.WriteLine("Reset");

                currentIndex = 0;

                if (bufferEnumerator != null)
                {
                    bufferEnumerator.Dispose();
                    bufferEnumerator = null;
                }
            }
        }
    }
}