using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TRNGTool
{
	internal class ArrayPoolLoader<T, F> : ILoadable where F : DataConstructor<T>
	{
		private ArrayPool<T> _pool;
		private static F s_dataConstructor = (F)DataConstructor<T>.Create();
		private static readonly int TBytes = Marshal.SizeOf(typeof(T));

		public ArrayPoolLoader(ArrayPool<T> pool)
		{
			_pool = pool;
		}

		internal class IncompleteReadException : TRNGToolException
		{
			public IncompleteReadException() { }
		}

		internal static FileInfo[] GetFilesInfo(string directoryPath, string searchPattern)
		{
			var files = GetDirectoryFiles(directoryPath, searchPattern);

			if (files.Length == 0)
			{
				throw new TRNGToolIOException($"There are no files matching '{searchPattern}' found in the specified directory", directoryPath);
			}

			return files;
		}

		internal static FileInfo[] GetDirectoryFiles(string directoryPath, string searchPattern)
		{
			try
			{
				var di = new DirectoryInfo(directoryPath);
				return di.GetFiles(searchPattern);
			}
			catch (SystemException e)
			{
				throw new TRNGToolIOException($"Unable to list files in {directoryPath}", searchPattern, e);
			}
		}

		internal static FileInfo GetFileInfo(string filePath)
		{
			try
			{
				return new FileInfo(filePath);
			}
			catch (SystemException e)
			{
				throw new TRNGToolIOException($"No such file: {filePath}", String.Empty, e);
			}
		}

		internal static FileStream GetFileStream(FileInfo file)
		{
			string path = file.FullName;

			try
			{
				return File.OpenRead(path);
			}
			catch (SystemException e)
			{
				throw new TRNGToolIOException("File open error", path, e);
			}
		}

		internal static MemoryStream GetMemoryStream(byte[] array, int indexFrom, int numBytesToRead)
		{
			try
			{
				return new MemoryStream(array, indexFrom, numBytesToRead);
			}
			catch (SystemException e)
			{
				throw new TRNGToolIOException("Unable to open memory stream", String.Empty, e);
			}
		}

		internal static BinaryReader GetBinaryReader(Stream stream)
		{
			try
			{
				return new BinaryReader(stream);
			}
			catch (SystemException e)
			{
				throw new TRNGToolIOException("Unable to create reader", String.Empty, e);
			}
		}

		public void AddFromPath(string directoryPath, string searchPattern, long numBytesToRead)
		{
			CheckNumBytesToRead(numBytesToRead);

			var files = GetFilesInfo(directoryPath, searchPattern);
			var added = AddFromFiles(files, numBytesToRead);

			Debug.Assert(added == numBytesToRead);
		}

		private void CheckNumBytesToRead(long numBytesToRead)
		{
			if (numBytesToRead <= 0)
			{
				throw new TRNGToolException($"Invalid read size: {numBytesToRead}");
			}

			if (numBytesToRead % TBytes != 0)
			{
				throw new TRNGToolException($"Amount of bytes required to read is not proportional to the size of the base random type: {numBytesToRead}");
			}
		}

		private long AddFromFiles(FileInfo[] files, long numBytesToRead)
		{
			if (files.Length == 1)
			{
				return AddFromFile(files[0], numBytesToRead);
			}

			Array.Sort(files, (f1, f2) => f1.Name.CompareTo(f2.Name));

			long bytesLeft = numBytesToRead;
			foreach (var file in files)
			{
				long toRead = file.Length >= bytesLeft ? bytesLeft : file.Length;

				bytesLeft -= AddFromFile(file, toRead);
				if (bytesLeft <= 0)
				{
					break;
				}
			}

			return numBytesToRead - bytesLeft;
		}

		private long AddFromFile(FileInfo file, long numBytesPortion)
		{
			if (file.Length % TBytes != 0)
			{
				throw new TRNGToolException($"Size of file {file.Length} is not proportional to the size of the base random type", file.FullName);
			}

			try
			{
				using var stream = GetFileStream(file);
				using var reader = GetBinaryReader(stream);
				return AddBytes(reader, numBytesPortion);
			}
			catch (IncompleteReadException)
			{
				throw new TRNGToolIOException("Unable to read all file contents", file.FullName);
			}
		}

		public long AddFromPath(string directoryPath, string searchPattern)
		{
			var files = GetFilesInfo(directoryPath, searchPattern);

			long toRead = files.Sum(x => x.Length);
			CheckNumBytesToRead(toRead);

			var added = AddFromFiles(files, toRead);
			Debug.Assert(added == toRead);

			return added;
		}

		public void AddFromFile(string filePath, long numBytesToRead)
		{
			CheckNumBytesToRead(numBytesToRead);

			var fileInfo = GetFileInfo(filePath);
			AddFromFile(fileInfo, numBytesToRead);
		}

		public long AddFromFile(string filePath)
		{
			var fileInfo = GetFileInfo(filePath);
			return AddFromFile(fileInfo, fileInfo.Length);
		}

		public void AddFromBytes(byte[] bytes, int indexFrom, int numBytesToRead)
		{
			CheckArgs(bytes, indexFrom, numBytesToRead);

			try
			{
				using var stream = GetMemoryStream(bytes, indexFrom, numBytesToRead);
				using var reader = GetBinaryReader(stream);
				AddBytes(reader, numBytesToRead);
			}
			catch (IncompleteReadException)
			{
				throw new TRNGToolIOException("Unable to read all byte array contents");
			}
		}

		private void CheckArgs(byte[] bytes, int indexFrom, int numBytesToRead)
		{
			CheckNumBytesToRead(numBytesToRead);

			if (bytes.Length % TBytes != 0)
			{
				throw new TRNGToolException($"array lenth {bytes.Length} is not proportional to the size of the base random type");
			}

			if (indexFrom < 0 || indexFrom + numBytesToRead > bytes.Length)
			{
				throw new TRNGToolException("Cannot read from byte array: invalid arguments");
			}
		}

		public void AddFromBytes(byte[] bytes)
			=>
			AddFromBytes(bytes, 0, bytes.Length);

		private long AddBytes(BinaryReader reader, long numBytes)
		{
			long readLeft = numBytes;

			while (readLeft > 0)
			{
				int bytesRead = GetReadSize(readLeft);
				byte[] data = ReadBytes(reader, bytesRead);
				var N = data.Length / TBytes;
				var TData = new T[N];
				s_dataConstructor.ConstructElements(data, 0, TData, 0, N);
				_pool.Add(TData);
				readLeft -= data.Length;
			}

			return numBytes - readLeft;
		}

		int GetReadSize(long readLeft)
		{
			return readLeft <= int.MaxValue ? (int)readLeft : int.MaxValue;
		}

		byte[] ReadBytes(BinaryReader reader, int numBytes)
		{
			byte[] data;

			try
			{
				data = reader.ReadBytes(numBytes);
			}
			catch (SystemException e)
			{
				throw new TRNGToolIOException("Reading failed", String.Empty, e);
			}

			if (data.Length != numBytes)
			{
				throw new IncompleteReadException();
			}

			return data;
		}
	}
}
