# True random numbers generator tool

��������� ��������� ����� � ������� �������������� ��������� � ���������.    

��� ������������ ��� ������ � �������� �������������������� ������� ��������� �����, ����������, ��������,
� ������� ����� �������� ��� [random.org](https://www.random.org) ��� [quantumnumbers.anu.edu.au](https://quantumnumbers.anu.edu.au).	 

��� ��������� ��������� ����� � ������ ��������� ������������ �������� ������� �`���:  
[pcg-random.org/posts/bounded-rands.html](https://www.pcg-random.org/posts/bounded-rands.html)  
[github.com/imneme/bounded-rands](https://github.com/imneme/bounded-rands)  
  
## �������������

��������� ���������� ������ RandomNumbers<T>:

```C#
using TRNGTool;

var randomDataPath = @"my_path";
var randomDataFile = @"my_path\my_file.bin";

// �������� ���� � �������� ������ � �������� ��������� ������:
var rgInt8  = RandomNumbers<byte>.Create(randomDataPath, "*.bin", 10_000);  // 10_000 ����
var rgInt16 = RandomNumbers<UInt16>.Create(randomDataPath, "*.bin");        // ������������ ��� ��������� ������
var rgInt32 = RandomNumbers<UInt32>.Create(randomDataPath, "*.bin");

// �������� ���� � ����������� �����:
var anotherRgInt8  = RandomNumbers<byte>.Create(randomDataFile);             // ������������ ��� ��������� ������
var anotherRgInt16 = RandomNumbers<UInt16>.Create(randomDataFile);
var anotherRgInt32 = RandomNumbers<UInt32>.Create(randomDataFile, 10_000);   // ����� 10_000 ����
```

��-�������:

```C#
var rgInt8 = RandomNumbers<byte>.Create();
var rgInt16 = RandomNumbers<UInt16>.Create();
var rgInt32 = RandomNumbers<UInt32>.Create();
var anotherRgInt8 = RandomNumbers<byte>.Create();
var anotherRgInt16 = RandomNumbers<UInt16>.Create();
var anotherRgInt32 = RandomNumbers<UInt32>.Create();

rgInt8.AddFromPath(randomDataPath, "*.bin", 10_000);
rgInt16.AddFromPath(randomDataPath, "*.bin");
rgInt32.AddFromPath(randomDataPath, "*.bin");

anotherRgInt8.AddFromFile(randomDataFile);
anotherRgInt16.AddFromFile(randomDataFile);
anotherRgInt32.AddFromFile(randomDataFile, 10_000);
```
� ������ ��������� Create() ��������� ���� � ������ (��� �����) �� ���������� �������.
���� ������������ ���� � ����������, ����������� �������� �������� ����� � �������� ������� ���������.
� ��������� ��������� ����������� ������ ���������� ���� ��� �������� (���� ��� ��������, ����������� ��� ��������� ������).
��������� ������ ������ ���� ������ ���������� ���� ������������� ��� ���������� ��������� ������ �������������� ����.  

� �������� ��������� ������ ����� ������������ �������� ������:

```C#
byte[] myByteArray = new byte[] { /* ������ */ };

var rgInt8  = RandomNumbers<byte>.Create(myByteArray, 0, 10_000);  // 10_000 ���� �� ������
var rgInt16 = RandomNumbers<UInt16>.Create(myByteArray);           // ������������ ��� ��������� ������
var rgInt32 = RandomNumbers<UInt32>.Create(myByteArray);

var anotherRgInt8 = RandomNumbers<byte>.Create();
var anotherRgInt16 = RandomNumbers<UInt16>.Create();
var anotherRgInt32 = RandomNumbers<UInt32>.Create();

anotherRgInt8.AddFromBytes(myByteArray, 0, 10_000);
anotherRgInt16.AddFromBytes(myByteArray);
anotherRgInt32.AddFromBytes(myByteArray);
```

��� ������ ����������� � ���� � ���������� ������ �������� (LinkedList<T[]>).
������ GetInt() ��������� ��������������� ������ ���������� ������.
�������, ������� ���� ��������� ���������, ��������� �� ������, ��������� ���������� ��� ������ ������.

#### ��������� ��������� ����� �� ��� ��������� ������������� ����� byte, UInt16, ��� UInt32:

```C#

// [0, 255]
uint r1 = rgInt8.GetInt();

// [0, 65535]
uint r2 = rgInt16.GetInt();

// [0, UInt32.MaxValue]
uint r3 = rgInt32.GetInt();
```

#### ��������� ��������� ����� � �������� ���������:

```C#

// [0, 254]
uint r1 = rgInt8.GetInt(0, 255);

// [300, 554]
uint r2 = rgInt8.GetInt(300, 555);

// [10_000, 10_254]
uint r3 = rgInt8.GetInt(10_000, 10_000 + byte.MaxValue);

// [0, 65534]
uint r4 = rgInt16.GetInt(0, UInt16.MaxValue);

// [0, UInt32.MaxValue - 1]
uint r5 = rgInt32.GetInt(0, UInt32.MaxValue);

```

## ��������� ������

��� ����, ����� ��������� ������ ���������� ������, ����������� � ������ ������ RandomNumbers, ����������� ������� **OutOfData**:

```C#
rg.OutOfData += RgOutOfData;

for (int i = 0; i < 100_000; i++)
{
    var r = rg.GetInt();
}   
    
void RgOutOfData(object sender, EventArgs e)
{
    // ��������� ������ ������
    rg.AddFromFile(myFile);
    rg.AddFromPath(myPath);
}

```

������� ����������� ��������������� ����� ���������� ������� GetInt() ���������� �������� ���������� �������.
����������� ��������� � GetInt() �������� � ��������� ���������� **TRNGToolOutOfDataException**.


#### ��������� ������ �����-������ � ������ ������:

```C#
try
{
    // ������������� RandomNumbers
}
catch (TRNGToolOutOfDataException e)
{
    // ����������� ������
    Console.WriteLine(e);
}
catch (TRNGToolIOException e)
{
    // ������ �����-������
    Console.WriteLine(e);
    if (e.InnerException != null)
        Console.WriteLine(e.InnerException);
}
catch (TRNGToolException e)
{
    // �������� ��������� ��� ������ ������������� �������
    Console.WriteLine(e);
}
```

��� ���������� SystemException, ����������� �� ����� ������ � ������� RandomNumbers, ���������� � �������� **InnerException**.


## ����������

����� ��������� ������, � ������� �������� **MaxArraySize** ����� ������ ���������� ������ ������������
�������� ������ RandomNumbers ��������:

```C#
const int oneKb = 2 << (10 - 1);
rg.MaxArraySize = oneKb;
rg.Clear();
rg.AddFromPath(myPath);
```

���������� ������ �������� �� ��������� � 35 ��.

�� �������� ��������� ���������� ������ ����� ������������������, ��������������� 50 ��������� ��������� ����� � ��������� [0, 500_000] � ���������� ��������� ��������
(����� ����� �������������� ����������� ��������� ������ ��������� 250 ��, �� ������� � ������ �������� �������������� 190,8 ��):
  
��� �������������� ������ ������:
  
![alt text](https://i.ibb.co/vcrG84w/3.png)
![alt text](https://i.ibb.co/ygMBqk2/4.png)  
  
� �������������� ������� ������ ����� ������ ���������:
  
![alt text](https://i.ibb.co/Q6d0VHL/1.png)
![alt text](https://i.ibb.co/VL0qc5h/2.png)

������ �������� �� ��������� ������������� ������� �� ������������������, �� �������� ����������� � ���� ������� ������� ���������
������� ������ ��������� �������������� ������ �� ����������� ������. �� ���� ������� ����� ������������ ��������� �������.
