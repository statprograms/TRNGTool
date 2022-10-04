using System;

namespace TRNGTool
{
	public class TRNGToolException : ApplicationException
	{
		public string Details { get; } = String.Empty;
		public TRNGToolException() { }
		public TRNGToolException(string message) : base(message) { }
		public TRNGToolException(string message, string details) : base(message) { Details = details; }
		public TRNGToolException(string message, string details, System.Exception inner) : base(message, inner) { Details = details; }
		public override string ToString() => this.GetType().FullName + ": " + Message + "\n" + Details;
	}

	public class TRNGToolOutOfDataException : TRNGToolException
	{
		public TRNGToolOutOfDataException() { }
		public TRNGToolOutOfDataException(string message) : base(message) { }
		public TRNGToolOutOfDataException(string message, string details, System.Exception inner) : base(message, details, inner) { }
	}

	public class TRNGToolIOException : TRNGToolException
	{
		public TRNGToolIOException() { }
		public TRNGToolIOException(string message) : base(message) { }
		public TRNGToolIOException(string message, string details) : base(message, details) { }
		public TRNGToolIOException(string message, string details, System.Exception inner) : base(message, details, inner) { }
	}
}
