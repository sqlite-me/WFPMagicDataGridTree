using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MagicDataGridTree
{
    class NullableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new();
        private TValue? _nullValue;
        private bool _hasNullValue;
        private int _count;
        private ICollection<TKey?> _keys;
        private ICollection<TValue?> _values;

        public TValue this[TKey key]
        {
            get => key == null ? (_hasNullValue ? _nullValue : throw new KeyNotFoundException()) : _dictionary[key];
            set
            {
                try
                {
                    if (key != null)
                    { _dictionary[key] = value; }
                    else
                    {
                        _nullValue = value;
                        _hasNullValue = true;
                    }
                }
                finally
                {
                    freshCount();
                }
            }
        }

        public ICollection<TKey?> Keys => _keys ?? _dictionary.Keys;

        public ICollection<TValue?> Values => _values ?? _dictionary.Values;

        public int Count => _count;

        public bool IsReadOnly { get; } = false;

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                _nullValue = value;
                _hasNullValue = true;
            }
            else
            {
                _dictionary.Add(key, value);
            }
            freshCount();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
            freshCount();
        }

        public void Clear()
        {
            _hasNullValue = false;
            _nullValue = default;
            _dictionary.Clear();
            freshCount();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null)
            {
                return _hasNullValue && (_nullValue?.Equals(item.Value) ?? item.Value == null);
            }
            else
            {
                return _dictionary.Contains(item);
            }
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null)
            {
                return _hasNullValue;
            }
            else { return _dictionary.ContainsKey(key); }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if ((uint)index > (uint)array.Length)
            {
                throw new IndexOutOfRangeException("index");
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
            }

            int count = _count;
            for (int i = 0; i < count; i++)
            {
                if (i == 0 && _hasNullValue)
                {
                    array[index++] = new KeyValuePair<TKey, TValue>(default, _nullValue);
                    continue;
                }

                ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, index);
            }
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);


        public bool Remove(TKey key)
        {
            try
            {
                if (key == null)
                {
                    if (_hasNullValue)
                    {
                        _nullValue = default;
                        _hasNullValue = false;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return _dictionary.Remove(key);
                }
            }
            finally
            {
                freshCount();
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null)
            {
                if (_hasNullValue)
                {
                    value = _nullValue;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }
            else
            {
                return _dictionary.TryGetValue(key, out value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void freshCount()
        {
            if (_hasNullValue)
            {
                _count = _dictionary.Count + 1;
                _keys = _dictionary.Keys.Concat(new[] { default(TKey) }).ToArray();
                _values = _dictionary.Values.Concat(new[] { _nullValue }).ToArray();
            }
            else
            {
                _count = _dictionary.Count;
                _keys = _dictionary.Keys;
                _values = _dictionary.Values;
            }
        }


        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly NullableDictionary<TKey, TValue> _dictionary;
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> _notNullEnumerator;
            private int _index;
            private KeyValuePair<TKey?, TValue> _current;
            private readonly int _getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal Enumerator(NullableDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                _dictionary = dictionary;
                _notNullEnumerator = dictionary._dictionary.GetEnumerator();
                _index = 0;
                _getEnumeratorRetType = getEnumeratorRetType;
                _current = default;
            }

            public bool MoveNext()
            {
                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                while ((uint)_index < (uint)_dictionary._count)
                {
                    if (_index++ == 0 && _dictionary._hasNullValue)
                    {
                        _current = new KeyValuePair<TKey?, TValue>(default, _dictionary._nullValue);
                        return true;
                    }

                    if (_notNullEnumerator.MoveNext())
                    {
                        _current = _notNullEnumerator.Current!;
                        return true;
                    }
                }

                _index = _dictionary._count + 1;
                _current = default;
                return false;
            }

            public KeyValuePair<TKey, TValue> Current => _current;

            public void Dispose() { }

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    if (_getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(_current.Key, _current.Value);
                    }

                    return new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
                }
            }

            void IEnumerator.Reset()
            {
                _index = 0;
                _current = default;
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    return new DictionaryEntry(_current.Key, _current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    return _current.Key;
                }
            }

            object? IDictionaryEnumerator.Value
            {
                get
                {
                    if (_index == 0 || (_index == _dictionary._count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    return _current.Value;
                }
            }
        }
    }

    static class NullableDictionary
    {
        internal static NullableDictionary<TKey, TElement> ToNullableDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> enumerable, Func<TSource, TKey> getKey, Func<TSource, TElement> getElement)
        {
            NullableDictionary<TKey, TElement> rlt = new();
            foreach (var element in enumerable)
            {
                rlt[getKey(element)] = getElement(element);
            }
            return rlt;
        }

        internal static NullableDictionary<TKey, TElement> ToNullableDictionary<TKey, TElement>(IDictionary<TKey, TElement> dictionary)
        {
            NullableDictionary<TKey, TElement> rlt = new();
            foreach (var element in dictionary)
            {
                rlt[element.Key] = element.Value;
            }
            return rlt;
        }
    }
}
