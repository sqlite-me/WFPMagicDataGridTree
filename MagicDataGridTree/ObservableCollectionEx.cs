using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagicDataGridTree
{
    internal static class ObservableCollectionEx
    {
        public static int FindIndex<T>(this ObservableCollection<T> source, Predicate<T> match)
        {
            return source.FindIndex(0, source.Count, match);
        }
        public static int FindIndex<T>(this IList<T> source, int startIndex, Predicate<T> match)
        {
            return source.FindIndex(startIndex, source.Count - startIndex, match);
        }
        public static int FindIndex<T>(this IList<T> source, int startIndex, int count, Predicate<T> match)
        {
            var _size = source.Count;
            if ((uint)startIndex > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Argument({nameof(startIndex)},{startIndex}) out of range in {nameof(source)}!");
            }

            if (count < 0 || startIndex > _size - count)
            {
                throw new IndexOutOfRangeException($"Argument({nameof(count)},{count}) out of range in {nameof(source)}!");
            }

            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(source[i])) return i;
            }
            return -1;
        }

        public static T[] RemoveRange<T>(this IList<T> source, int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Argument({nameof(index)},{index}) out of range in {nameof(source)}!");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            var _size = source.Count;
            if (_size - index < count)
                throw new ArgumentException(nameof(count));

            if (count > 0)
            {
                T[] removed= new T[count];
                var j = count - 1;
                for (var i = index + count - 1; i >= index; i--,j--)
                {
                    removed[j]=source[i];
                    source.RemoveAt(i);
                }
                return removed;
            }

            return new T[0];
        }

        public static T[] GetRange<T>(this IList<T> source, int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Argument({nameof(index)},{index}) out of range in {nameof(source)}!");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            var _size = source.Count;
            if (_size - index < count)
                throw new ArgumentException(nameof(count));

            if (count > 0)
            {
                T[] result= new T[count];
                for (var i=0; i<count; i++,index++)
                {
                    result[i]=source[index];
                }

                return result;
            }
            return new T[0];
        }
    }
}