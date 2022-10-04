using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TRNGTool
{
	internal class ArrayPool<T> : IArrayPool<T>, ICloneable
	{
		private const int oneKb = 2 << (10 - 1);
		private const int oneMb = 2 << (20 - 1);
		private const int oneGb = 2 << (30 - 1);

		private const int _minArraySize = oneKb;
		private const int _maxArraySize = oneGb;
		private int _defaultMaxArraySize;

		private static readonly int TBytes = Marshal.SizeOf(typeof(T));

		private LinkedList<T[]> _dataPool;
		private LinkedListNode<T[]> _currentArray;

		private int _currentArrayReadIndex;
		private bool _reachedEnd;

		// How much data ready to be read (not counting the current array)
		private long _freeSize;

		// How much data was deleted
		private long _deletedSize;

		public ArrayPool()
		{
			MaxArraySize = 35 * oneMb;
			_dataPool = new();
			ClearInit();
		}

		void ClearInit()
		{
			_dataPool.Clear();
			_currentArray = null;
			_currentArrayReadIndex = -1;
			_freeSize = 0;
			_deletedSize = 0;
			_reachedEnd = true;
		}


		// ICloneable
		public object Clone()
		{
			var newObj = (ArrayPool<T>)this.MemberwiseClone();

			newObj._dataPool = new LinkedList<T[]>();
			foreach (var arr in this._dataPool)
			{
				newObj._dataPool.AddLast((T[])arr.Clone());
			}

			return newObj;
		}


		// IArrayPool
		public bool ReachedEnd => _reachedEnd;
		public long OverallSize => _freeSize + _deletedSize + CurrentArraySize;
		public long OverallSizeBytes => OverallSize * TBytes;
		public long AvailableSize => ReachedEnd ? 0 : _freeSize + CurrentArraySize - _currentArrayReadIndex;
		public long AvailableSizeBytes => AvailableSize * TBytes;
		public void Clear() => ClearInit();
		private long CurrentArraySize => _currentArray?.Value.Length ?? 0;

		public int MaxArraySize
		{
			get => _defaultMaxArraySize;

			set
			{
				Debug.Assert(value % TBytes == 0);
				Debug.Assert(value >= _minArraySize && value <= _maxArraySize);

				if (value % TBytes == 0
					&& value >= _minArraySize
					&& value <= _maxArraySize)
				{
					_defaultMaxArraySize = value;
				}
			}
		}

		private int TMaxArraySize => MaxArraySize / TBytes;

		// Amount of data used, from 0.0 to 1.0
		public double Usage
		{
			get
			{
				if (ReachedEnd)
				{
					return 1;
				}

				long used = _deletedSize + _currentArrayReadIndex;
				return (double)used / OverallSize;
			}
		}

		public T Get()
		{
			if (_currentArray == null)
			{
				throw new TRNGToolOutOfDataException("Insufficient random data");
			}

			T[] buffer = _currentArray.Value;
			T result = buffer[++_currentArrayReadIndex];

			if (_currentArrayReadIndex >= buffer.Length - 1)
			{
				_currentArray = _currentArray.Next;
				_dataPool.RemoveFirst();
				_deletedSize += buffer.Length;
				_currentArrayReadIndex = -1;

				if (_currentArray == null)
				{
					_reachedEnd = true;
				}
				else
				{
					_freeSize -= _currentArray.Value.Length;
				}
			}

			return result;
		}

		public void Add(T[] data)
		{
			int addLeft = data.Length;
			int fromIdx = 0;

			while (addLeft > 0)
			{
				T[] buffer = AddArray(addLeft);
				Array.Copy(data, fromIdx, buffer, 0, buffer.Length);
				fromIdx += buffer.Length;
				addLeft -= buffer.Length;
			}

			_freeSize += data.Length;
		}

		private T[] AddArray(int addLeft)
		{
			int size = GetAllocateSize(addLeft);
			var buffer = new T[size];

			_dataPool.AddLast(buffer);
			_reachedEnd = false;

			if (_dataPool.Count == 1 || _currentArray == null)
			{
				// this is the first array, or adding after the out of data state (_currentArray == null)
				_currentArray = _dataPool.Last;
			}

			return buffer;
		}

		private int GetAllocateSize(int addLeft)
		{
			return addLeft <= TMaxArraySize ? addLeft : TMaxArraySize;
		}
	}
}
