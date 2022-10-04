using System;
using System.Diagnostics;

namespace TRNGTool
{
	/// <summary>
	///     Class providing random numbers in a required range. External binary files are used as a data source of random numbers.
	///		The data is loaded into the LinkedList of arrays of bytes, UInt16s or UInt32s depending on the generic type T.
	///		The return values are always uint.
	/// 
	///     It's supposed that you're using files containing true random numbers obtained from
	///     TRNG services like www.random.org or https://quantumnumbers.anu.edu.au	
	/// </summary>
	/// <exception cref="TRNGToolException" />
	/// <exception cref="TRNGToolIOException" />
	/// <exception cref="TRNGToolOutOfDataException" />
	public abstract class RandomNumbers<T> : IRandomNumbers<uint>, IArrayPool<T>, ILoadable, ICloneable
	{
		public Type RandomType => typeof(T);
		abstract protected uint RandomTypeMaxValue { get; }
		abstract protected uint NextInt();
		abstract protected uint BoundedRand(uint range);
		abstract internal ArrayPool<T> DataPool { get; set; }
		abstract internal ArrayPoolLoader<T, DataConstructor<T>> Loader { get; set; }


		// IRandomNumbers
		public virtual uint GetInt()
		{
			uint r = NextInt();
			if (ReachedEnd)
				OutOfData?.Invoke(this, null);

			return r;
		}

		public virtual uint GetInt(uint min, uint max)
		{
			Debug.Assert(min < max);
			Debug.Assert(max - min <= RandomTypeMaxValue);
			return min + BoundedRand(max - min);
		}

		/// <summary>Raised before returning from GetInt() method when there is no more data left in the buffer</summary>
		public event EventHandler<EventArgs> OutOfData;

		protected RandomNumbers()
		{
			DataPool = new ArrayPool<T>();
			Loader = new ArrayPoolLoader<T, DataConstructor<T>>(DataPool);
		}

		static RandomNumbers<T> CreateObj()
		{
			if (typeof(T) == typeof(byte))
			{
				return (RandomNumbers<T>)(object)new RandomNumbersUInt8();
			}
			else if (typeof(T) == typeof(UInt16))
			{
				return (RandomNumbers<T>)(object)new RandomNumbersUInt16();
			}
			else if (typeof(T) == typeof(UInt32))
			{
				return (RandomNumbers<T>)(object)new RandomNumbersUInt32();
			}
			else
			{
				throw new TRNGToolException($"{typeof(T).Name} for random type is not supported yet");
			}
		}

		public static RandomNumbers<T> Create(string directoryPath, string searchPattern, long numBytesToRead)
		{
			var obj = CreateObj();
			obj.AddFromPath(directoryPath, searchPattern, numBytesToRead);
			return obj;
		}

		public static RandomNumbers<T> Create(string directoryPath, string searchPattern)
		{
			var obj = CreateObj();
			obj.AddFromPath(directoryPath, searchPattern);
			return obj;
		}

		public static RandomNumbers<T> Create(string filePath, long numBytesToRead)
		{
			var obj = CreateObj();
			obj.AddFromFile(filePath, numBytesToRead);
			return obj;
		}

		public static RandomNumbers<T> Create(string filePath)
		{
			var obj = CreateObj();
			obj.AddFromFile(filePath);
			return obj;
		}

		public static RandomNumbers<T> Create(byte[] bytes, int indexFrom, int numBytesToRead)
		{
			var obj = CreateObj();
			obj.AddFromBytes(bytes, indexFrom, numBytesToRead);
			return obj;
		}

		public static RandomNumbers<T> Create(byte[] bytes)
		{
			var obj = CreateObj();
			obj.AddFromBytes(bytes);
			return obj;
		}

		public static RandomNumbers<T> Create()
		{
			return CreateObj();
		}


		// ICloneable
		public object Clone()
		{
			var newObj = (RandomNumbers<T>)this.MemberwiseClone();
			newObj.DataPool = (ArrayPool<T>)this.DataPool.Clone();
			return newObj;
		}


		// IArrayPool
		public T Get() => DataPool.Get();
		public void Clear() => DataPool.Clear();
		public bool ReachedEnd => DataPool.ReachedEnd;
		public double Usage => DataPool.Usage;
		public long OverallSize => DataPool.OverallSize;
		public long OverallSizeBytes => DataPool.OverallSizeBytes;
		public long AvailableSize => DataPool.AvailableSize;
		public long AvailableSizeBytes => DataPool.AvailableSizeBytes;
		public int MaxArraySize { get => DataPool.MaxArraySize; set => DataPool.MaxArraySize = value; }


		// ILoadable
		public void AddFromPath(string directoryPath, string searchPattern, long numBytesToRead)
			=> Loader.AddFromPath(directoryPath, searchPattern, numBytesToRead);

		public long AddFromPath(string directoryPath, string searchPattern)
			=> Loader.AddFromPath(directoryPath, searchPattern);

		public void AddFromFile(string filePath, long numBytesToRead)
			=> Loader.AddFromFile(filePath, numBytesToRead);

		public long AddFromFile(string filePath)
			=> Loader.AddFromFile(filePath);

		public void AddFromBytes(byte[] bytes, int indexFrom, int numBytesToRead)
			=> Loader.AddFromBytes(bytes, indexFrom, numBytesToRead);

		public void AddFromBytes(byte[] bytes)
			=> Loader.AddFromBytes(bytes);
	}

	internal class RandomNumbersUInt32 : RandomNumbers<UInt32>
	{
		// Algorithm by Melissa O'Neill:
		//     https://www.pcg-random.org/posts/bounded-rands.html
		//     https://github.com/imneme/bounded-rands
		//					
		/// <returns>Random number within a [0..UInt32.MaxValue) range</returns>
		protected override uint BoundedRand(uint r)
		{
			UInt32 range = (UInt32)r;
			UInt32 x = GetInt();
			UInt64 m = (UInt64)x * (UInt64)range;
			UInt32 l = (UInt32)m;

			if (l < range)
			{
				UInt32 t = (UInt32)(-range);

				if (t >= range)
				{
					t -= range;

					if (t >= range)
						t %= range;
				}

				while (l < t)
				{
					x = GetInt();
					m = (UInt64)x * (UInt64)range;
					l = (UInt32)m;
				}
			}

			return (UInt32)(m >> 32);
		}

		internal override ArrayPool<UInt32> DataPool { get; set; }
		internal override ArrayPoolLoader<UInt32, DataConstructor<UInt32>> Loader { get; set; }
		protected override uint RandomTypeMaxValue => UInt32.MaxValue;
		protected override uint NextInt() => DataPool.Get();
	}

	internal class RandomNumbersUInt16 : RandomNumbers<UInt16>
	{
		/// <returns>Random number within a [0..UInt16.MaxValue) range</returns>
		protected override uint BoundedRand(uint r)
		{
			UInt16 range = (UInt16)r;
			UInt16 x = (UInt16)GetInt();
			UInt32 m = (UInt32)x * (UInt32)range;
			UInt16 l = (UInt16)m;

			if (l < range)
			{
				UInt16 t = (UInt16)(-range);

				if (t >= range)
				{
					t -= range;

					if (t >= range)
						t %= range;
				}

				while (l < t)
				{
					x = (UInt16)GetInt();
					m = (UInt32)x * (UInt32)range;
					l = (UInt16)m;
				}
			}

			return (UInt16)(m >> 16);
		}

		internal override ArrayPool<UInt16> DataPool { get; set; }
		internal override ArrayPoolLoader<UInt16, DataConstructor<UInt16>> Loader { get; set; }
		protected override uint RandomTypeMaxValue => UInt16.MaxValue;
		protected override uint NextInt() => DataPool.Get();
	}

	public class RandomNumbersUInt8 : RandomNumbers<byte>
	{
		/// <returns>Random number within a [0..byte.MaxValue) range</returns>
		protected override uint BoundedRand(uint r)
		{
			byte range = (byte)r;
			byte x = (byte)GetInt();
			UInt16 m = (UInt16)((UInt16)x * (UInt16)range);
			byte l = (byte)m;

			if (l < range)
			{
				byte t = (byte)-range;

				if (t >= range)
				{
					t -= range;

					if (t >= range)
						t %= range;
				}

				while (l < t)
				{
					x = (byte)GetInt();
					m = (UInt16)((UInt16)x * (UInt16)range);
					l = (byte)m;
				}
			}

			return (byte)(m >> 8);
		}

		internal override ArrayPool<byte> DataPool { get; set; }
		internal override ArrayPoolLoader<byte, DataConstructor<byte>> Loader { get; set; }
		protected override uint RandomTypeMaxValue => byte.MaxValue;
		protected override uint NextInt() => DataPool.Get();
	}
}
