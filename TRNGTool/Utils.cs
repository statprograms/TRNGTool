using System;

namespace TRNGTool
{
	static public class RandomShuffler
	{
		public static void ShuffleRandomly<E, T>(E[] data, RandomNumbers<T> rg)
		{
			for (int i = data.Length; i > 1; --i)
			{
				uint r = rg.GetInt(0, (uint)i);
				E t = data[r];
				data[r] = data[i - 1];
				data[i - 1] = t;
			}
		}

		public static void ShufflePseudoRandomly<T>(T[] data, Random rg)
		{
			for (int i = data.Length; i > 1; --i)
			{
				int r = rg.Next(0, i);
				T t = data[r];
				data[r] = data[i - 1];
				data[i - 1] = t;
			}
		}
	}
}
