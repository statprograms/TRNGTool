using TRNGTool;

try
{
	Example();
}
catch (TRNGToolException e)
{
	Console.WriteLine(e);

	if (e.InnerException != null)
	{
		Console.WriteLine(e.InnerException);
	}
}
catch (Exception e)
{
	Console.WriteLine(e);
}

static void Example()
{
	var randomDataPath = @"..\..\..\random_data";
	const int oneKb = 2 << (10 - 1);

	var rgInt8 = RandomNumbers<byte>.Create(randomDataPath, "*.bin");
	var rgInt16 = RandomNumbers<UInt16>.Create(randomDataPath, "*.bin");
	var rgInt32 = RandomNumbers<UInt32>.Create(randomDataPath, "*.bin", oneKb);

	rgInt32.OutOfData += rgInt32_OnOutOfData;

	byte r1 = (byte)rgInt8.GetInt();
	Console.WriteLine($"[0 to 255]\n{r1}\n");

	uint r3 = rgInt16.GetInt();
	Console.WriteLine($"[0 to 65535]\n{r3}\n");

	uint r4 = rgInt32.GetInt();
	Console.WriteLine($"[0 to UInt32.MaxValue]\n{r4}\n");

	uint r5 = rgInt8.GetInt(0, 255);
	Console.WriteLine($"[0 to 254]\n{r5}\n");

	uint r6 = rgInt8.GetInt(300, 555);
	Console.WriteLine($"[300 to 554]\n{r6}\n");

	uint r7 = rgInt8.GetInt(10_000, 10_000 + byte.MaxValue);
	Console.WriteLine($"[10_000 to 10_254]\n{r7}\n");

	uint r8 = rgInt16.GetInt(0, UInt16.MaxValue);
	Console.WriteLine($"[0 to 65534]\n{r8}\n");

	uint r9 = rgInt32.GetInt(100_000, 500_000 + 1);
	Console.WriteLine($"[100_000 to 500_000]\n{r9}\n");

	uint r10 = rgInt32.GetInt(0, UInt32.MaxValue);
	Console.WriteLine($"[0 to UInt32.MaxValue - 1]\n{r10}\n");

	for (int i = 0; i < 100_000; i++)
	{
		rgInt32.GetInt();

		if (i % 10_000 == 0)
		{
			Console.WriteLine($"Usage: {rgInt32.Usage:p}");
		}
	}

	void rgInt32_OnOutOfData(object? sender, EventArgs e)
	{
		Console.WriteLine("rgInt32 is out of data, loading more");
		var randomDataPath2 = @"..\..\..\random_data2";
		rgInt32.AddFromFile(Path.Combine(randomDataPath2, "2022-06-02.bin"));
	}
}
