using System;
using System.Runtime.InteropServices;

namespace TRNGTool
{
	internal abstract class DataConstructor<T>
	{
		// Fills array of T-s from array of bytes
		public abstract void ConstructElements(byte[] arrayFrom, int indexFrom, T[] arrayTo, int indexTo, int numElements);

		static public DataConstructor<T> Create()
		{
			if (typeof(T) == typeof(byte))
			{
				return (DataConstructor<T>)(object)new UInt8Data();
			}
			else if (typeof(T) == typeof(UInt16))
			{
				return (DataConstructor<T>)(object)new UInt16Data();
			}
			else if (typeof(T) == typeof(UInt32))
			{
				return (DataConstructor<T>)(object)new UInt32Data();
			}
			else
			{
				throw new NotSupportedException();
			}
		}
	}

	internal class UInt8Data : DataConstructor<byte>
	{
		public override void ConstructElements(byte[] arrayFrom, int indexFrom, byte[] arrayTo, int indexTo, int numElements)
		{
			Array.Copy(arrayFrom, indexFrom, arrayTo, indexTo, numElements);
		}
	}

	internal class UInt16Data : DataConstructor<UInt16>
	{
		public override void ConstructElements(byte[] arrayFrom, int indexFrom, UInt16[] arrayTo, int indexTo, int numElements)
		{
			int from = indexFrom;
			int to = indexTo;

			for (int i = 0; i < numElements; i++)
			{
				arrayTo[to++] = BitConverter.ToUInt16(arrayFrom, from);
				from += sizeof(UInt16);
			}
		}
	}

	internal class UInt32Data : DataConstructor<UInt32>
	{
		public override void ConstructElements(byte[] arrayFrom, int indexFrom, UInt32[] arrayTo, int indexTo, int numElements)
		{
			int from = indexFrom;
			int to = indexTo;

			for (int i = 0; i < numElements; i++)
			{
				arrayTo[to++] = BitConverter.ToUInt32(arrayFrom, from);
				from += sizeof(UInt32);
			}
		}
	}
}
