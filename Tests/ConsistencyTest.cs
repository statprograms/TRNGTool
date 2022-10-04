using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using TRNGTool;

namespace TRNGTool.ConsistencyTest
{
	[TestClass]
	public class ConsistencyTest
	{
		private const int oneKb = 2 << (10 - 1);
		private const int _arraysSize = oneKb;
		private const string _tempDir = "test_tmp";

		[TestMethod]
		public void TestMethod()
		{
			PrepareTempDirectory();
			Assert.IsTrue(BytesConsistent());
			Assert.IsTrue(FilesConsistent());
			Assert.IsTrue(MultiFilesConsistent());
		}

		private bool BytesConsistent()
		{
			ConsistencyTestBytes<byte>(0, byte.MaxValue);
			ConsistencyTestBytes<UInt16>(0, UInt16.MaxValue);
			ConsistencyTestBytes<UInt32>(UInt32.MaxValue - 100_000, UInt32.MaxValue);
			return true;
		}

		private bool FilesConsistent()
		{
			ConsistencyTestFile<byte>(Path.Combine(_tempDir, "numbers1.tmp"), 0, byte.MaxValue);
			ConsistencyTestFile<UInt16>(Path.Combine(_tempDir, "numbers2.tmp"), 0, UInt16.MaxValue);
			ConsistencyTestFile<UInt32>(Path.Combine(_tempDir, "numbers3.tmp"), UInt32.MaxValue - 100_000, UInt32.MaxValue);
			return true;
		}

		private bool MultiFilesConsistent()
		{
			const string fileMask = "consequent*.tmp";
			const string fileFormatString = "consequent{0}.tmp";
			int fileCount = 0;

			string NextFileName()
				=>
				Path.Combine(_tempDir, String.Format(fileFormatString, ++fileCount));
			
			ConsistencyTestFile<UInt16>(NextFileName(), 0, 10_000);
			ConsistencyTestFile<UInt16>(NextFileName(), 10_001, 20_000);
			ConsistencyTestFile<UInt16>(NextFileName(), 20_001, 30_000);
			ConsistencyTestFile<UInt16>(NextFileName(), 30_001, UInt16.MaxValue);
			ConsistencyTestMulti<UInt16>(_tempDir, fileMask, 0, UInt16.MaxValue);
			return true;
		}

		private void PrepareTempDirectory()
		{
			var di = new DirectoryInfo(_tempDir);
			if (!di.Exists)
			{
				di.Create();
			}
			else
			{
				foreach (var f in di.EnumerateFiles("*.tmp"))
				{
					f.Delete();
				}
			}
		}

		private void ConsistencyTestBytes<T>(uint from, uint to)
		{
			long bSize = (to - from + 1) * Marshal.SizeOf(typeof(T));

			if (to < from || bSize > int.MaxValue)
			{
				throw new TestArgumentsException();
			}

			byte[] buffer = new byte[(int)bSize];

			WriteBytesNumbers();
			ReadBytesNumbers();

			void WriteBytesNumbers()
			{
				if (typeof(T) == typeof(byte))
				{
					WriteBytes();
				}
				else if (typeof(T) == typeof(UInt16))
				{
					WriteUInt16s();
				}
				else if (typeof(T) == typeof(UInt32))
				{
					WriteUInt32s();
				}
				else
				{
					throw new TestArgumentsException();
				}

				void WriteBytes()
				{
					for (long i = from; i <= to; i++)
					{
						buffer[i] = (byte)i;
					}
				}

				void WriteUInt16s()
				{
					int bufferIdx = 0;

					for (long i = from; i <= to; i++)
					{
						var bytes = BitConverter.GetBytes((UInt16)i);
						Array.Copy(bytes, 0, buffer, bufferIdx, bytes.Length);
						bufferIdx += bytes.Length;
					}
				}

				void WriteUInt32s()
				{
					int bufferIdx = 0;

					for (long i = from; i <= to; i++)
					{
						var bytes = BitConverter.GetBytes((UInt32)i);
						Array.Copy(bytes, 0, buffer, bufferIdx, bytes.Length);
						bufferIdx += bytes.Length;
					}
				}
			}

			void ReadBytesNumbers()
			{
				var rg = RandomNumbers<T>.Create();
				rg.MaxArraySize = _arraysSize;
				rg.AddFromBytes(buffer);

				for (long i = from; i <= to; i++)
				{
					if (rg.GetInt() != i)
					{
						throw new TestFailedException();
					}
				}
			}
		}

		private void ConsistencyTestFile<T>(string newFilePath, uint from, uint to)
		{
			if (to < from)
			{
				throw new TestArgumentsException();
			}

			WriteFileNumbers();
			ReadFileNumbers();

			void WriteFileNumbers()
			{
				using var stream = File.Open(newFilePath, FileMode.Create);
				using var writer = new BinaryWriter(stream);

				if (typeof(T) == typeof(byte))
				{
					WriteBytes();
				}
				else if (typeof(T) == typeof(UInt16))
				{
					WriteUInt16s();
				}
				else if (typeof(T) == typeof(UInt32))
				{
					WriteUInt32s();
				}
				else
				{
					throw new TestArgumentsException();
				}

				void WriteBytes()
				{
					for (long i = from; i <= to; i++)
					{
						writer.Write((byte)i);
					}
				}

				void WriteUInt16s()
				{
					for (long i = from; i <= to; i++)
					{
						writer.Write((UInt16)i);
					}
				}

				void WriteUInt32s()
				{
					for (long i = from; i <= to; i++)
					{
						writer.Write((UInt32)i);
					}
				}
			}

			void ReadFileNumbers()
			{
				var rg = RandomNumbers<T>.Create();
				rg.MaxArraySize = _arraysSize;
				rg.AddFromFile(newFilePath);

				for (long i = from; i <= to; i++)
				{
					if (rg.GetInt() != i)
					{
						throw new TestFailedException();
					}
				}
			}
		}

		private void ConsistencyTestMulti<T>(string path, string mask, uint from, uint to)
		{
			if (to < from)
			{
				throw new TestArgumentsException();
			}

			var rg = RandomNumbers<T>.Create();
			rg.MaxArraySize = _arraysSize;
			rg.AddFromPath(path, mask);

			for (long i = from; i <= to; i++)
			{
				if (rg.GetInt() != i)
				{
					throw new TestFailedException();
				}
			}
		}

		internal class TestFailedException : ApplicationException
		{
			public TestFailedException() : base("Test failed") { }
		}

		internal class TestArgumentsException : ApplicationException
		{
			public TestArgumentsException() : base("Test bad arguments") { }
		}
	}
}
