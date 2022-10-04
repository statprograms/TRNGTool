namespace TRNGTool
{
	public interface IRandomNumbers<T>
	{
		// Returns next random number
		public T GetInt();

		// Returns a random number within a [min, max) range
		public T GetInt(T min, T max);
	}

	public interface ILoadable
	{
		public void AddFromPath(string directoryPath, string searchPattern, long numBytesToRead);
		/// <returns>Number of added bytes</returns>
		public long AddFromPath(string directoryPath, string searchPattern);
		public void AddFromFile(string filePath, long numBytesToRead);
		/// <returns>Number of added bytes</returns>
		public long AddFromFile(string filePath);
		public void AddFromBytes(byte[] bytes, int indexFrom, int numBytesToRead);
		public void AddFromBytes(byte[] bytes);
	}

	public interface IArrayPool<T>
	{
		/// <summary>Gets next int from the data pool</summary>
		public T Get();

		/// <summary>Clears all data pool contents</summary>
		public void Clear();

		/// <summary>True if there are no data available to read</summary>
		public bool ReachedEnd { get; }

		/// <summary>Amount of data used, from 0.0 to 1.0</summary>
		public double Usage { get; }

		/// <summary>Overall size in T-s</summary>
		public long OverallSize { get; }

		/// <summary>Overall size in bytes</summary>
		public long OverallSizeBytes { get; }

		/// <summary>Size available for reading</summary>
		public long AvailableSize { get; }

		/// <summary>Size available for reading in bytes</summary>
		public long AvailableSizeBytes { get; }

		/// <summary>Maximum size of one array</summary>
		public int MaxArraySize { get; set; }
	}
}
