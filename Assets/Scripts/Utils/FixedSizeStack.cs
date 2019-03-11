using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Utils
{
    [Serializable]
    public class FixedSizeStack<T> : IEnumerable<T>, IEnumerable
    {
        private T[] _data;
        private int _pointer;
        private int _count;
        private long _version;

        public FixedSizeStack(int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            _data = new T[size];
            _pointer = _data.GetLowerBound(0);
            _version = 0;
        }

        private void _IncrementPointer()
        {
            if (_pointer++ == _data.GetUpperBound(0))
            {
                _pointer = _data.GetLowerBound(0);
            }
        }

        private void _DecrementPointer()
        {
            if (_pointer-- == _data.GetLowerBound(0))
            {
                _pointer = _data.GetUpperBound(0);
            }
        }

        public bool Contains(T item)
        {
            return _data.Contains(item);
        }

        public int Count()
        {
            return _count;
        }

        public int Size
        {
            get { return _data.Length; }
        }

        public void Clear()
        {
            _data = new T[Size];
        }

        public T this[int index]
        {
            get
            {
                var i = _pointer - index;
                if (i < 0)
                {
                    i += Size;
                }
                return _data[i];
            }
        }

        public T Pop()
        {
            var item = _data[_pointer];
            _data[_pointer] = default(T);
            _DecrementPointer();
            _version++;
            if (_count > _data.GetLowerBound(0))
            {
                _count--;
            }
            return item;
        }

        public void Push(T item)
        {
            _IncrementPointer();
            _data[_pointer] = item;
            _version++;
            if (_count <= _data.GetUpperBound(0))
            {
                _count++;
            }
        }

        public T Peek()
        {
            return _data[_pointer];
        }

        [Serializable]
        private struct Enumerator : IEnumerator<T>
        {
            private readonly FixedSizeStack<T> _stack;
            private int _index;
            private readonly long _version;
            private T _currentElement;

            internal Enumerator(FixedSizeStack<T> stack)
            {
                _stack = stack;
                _version = _stack._version;
                _index = -2;
                _currentElement = default(T);
            }

            public void Dispose()
            {
                _index = -1;
            }

            public bool MoveNext()
            {
                if (_version != _stack._version)
                    throw new InvalidOperationException("Version Conflict");
                if (_index == -2)
                {
                    _index = 0;
                    var flag = _index >= 0;
                    if (flag)
                        _currentElement = _stack[_index];
                    return flag;
                }
                if (_index == -1)
                    return false;
                var num = _index + 1;
                _index = num;
                var flag1 = num < _stack.Count();
                _currentElement = !flag1 ? default(T) : _stack[_index];
                return flag1;
            }

            public T Current
            {
                get
                {
                    if (_index == -2)
                        throw new InvalidOperationException("Enumeration Not Started");
                    if (_index == -1)
                        throw new InvalidOperationException("Enumeration Ended");
                    return _currentElement;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                if (_version != _stack._version)
                    throw new InvalidOperationException("Version Conflict");
                _index = -2;
                _currentElement = default(T);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}