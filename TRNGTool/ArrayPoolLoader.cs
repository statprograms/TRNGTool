using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TRNGTool
{
	internal class ArrayPoolLoader<T> : ILoadable
	{
		private ArrayPool<T> _pool;
		private static readonly int TSize = Marshal.SizeOf(typeof(T));
		private readonly object _lock; // the lock passed from RandomNumbers
		private readonly RandomNumbers<T> _owner;

		public ArrayPoolLoader(ArrayPool<T> pool, object syncRoot, RandomNumbers<T> owner)
		{
			_pool = pool;
			_lock = syncRoot;
			_owner = owner;
		}

		public long AddFromPath(string directoryPath, string searchPattern, long numBytesToRead)
		{
			if (!Directory.Exists(directoryPath))
			{
				return -1;
			}

			var files = Directory.GetFiles(directoryPath, searchPattern);
			long totalRead = 0;

			foreach (var file in files)
			{
				if (totalRead >= numBytesToRead)
				{
					break;
				}

				long remaining = numBytesToRead - totalRead;
				totalRead += AddFromFile(file, remaining);
			}

			return totalRead;
		}

		public long AddFromPath(string directoryPath, string searchPattern)
		{
			if (!Directory.Exists(directoryPath)) return -1;

			var files = Directory.GetFiles(directoryPath, searchPattern);
			long totalRead = 0;

			foreach (var file in files)
			{
				totalRead += AddFromFile(file);
			}

			return totalRead;
		}

		long ILoadable.AddFromBytes(byte[] bytes)
		{
			return AddFromBytes(bytes, 0, bytes.Length);
		}

		public long AddFromFile(string filePath, long numBytesToRead)
		{
			byte[] fileBytes = ReadAllBytes(filePath);

			int actualBytesToRead = (int)Math.Min(fileBytes.Length, numBytesToRead);

			actualBytesToRead -= actualBytesToRead % TSize;

			if (actualBytesToRead < numBytesToRead)
			{
				throw new ArgumentOutOfRangeException(nameof(numBytesToRead),
					$"The size of File {filePath} (aligned to {TSize} bytes) is insufficient to read {numBytesToRead} bytes.");
			}

			if (actualBytesToRead > 0)
			{
				AddFromBytes(fileBytes, 0, actualBytesToRead);
				return actualBytesToRead;
			}

			return 0;
		}

		public long AddFromFile(string filePath)
		{
			var fileInfo = GetFileInfo(filePath);
			return AddFromFile(filePath, fileInfo.Length);
		}

		public long AddFromBytes(byte[] bytes)
			=>
			AddFromBytes(bytes, 0, bytes.Length);

		public long AddFromBytes(byte[] bytes, int indexFrom, int numBytesToRead)
		{
			if (bytes == null) throw new ArgumentNullException(nameof(bytes));

			if (indexFrom < 0 || numBytesToRead < 0 || indexFrom + numBytesToRead > bytes.Length)
			{
				throw new ArgumentOutOfRangeException("Specified range is outside the bounds of the byte array.");
			}

			if (numBytesToRead % TSize != 0)
			{
				throw new ArgumentException($"Bytes must be a multiple of type size ({TSize}).");
			}

			int elementCount = numBytesToRead / TSize;
			T[] convertedData = new T[elementCount];

			Buffer.BlockCopy(bytes, indexFrom, convertedData, 0, numBytesToRead);

			lock (_lock)
			{
				_pool.Add(convertedData);
				_owner.NotifyDataLoaded();
			}

			return numBytesToRead;
		}

		private static FileInfo GetFileInfo(string filePath)
		{
			try
			{
				return new FileInfo(filePath);
			}
			catch (SystemException e)
			{
				throw new TRNGToolIOException($"Unable to get file information: {filePath}", String.Empty, e);
			}
		}

		private static byte[] ReadAllBytes(string filePath)
		{
			try
			{
				return File.ReadAllBytes(filePath);
			}
			catch (SystemException e)
			{
				throw new TRNGToolIOException($"Unable to read the file: {filePath}", String.Empty, e);
			}
		}
	}
}
