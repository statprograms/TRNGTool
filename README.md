# True random numbers generator tool

Code for selecting a random number with good statistical quality and speed.  

You need pregenerated binary random data to use this, for example obtained from TRNG online services like
[random.org](https://www.random.org) or [quantumnumbers.anu.edu.au](https://quantumnumbers.anu.edu.au).	 

Melissa O'Neill's algorithm is used to generate random numers in the desired range:  
[pcg-random.org/posts/bounded-rands.html](https://www.pcg-random.org/posts/bounded-rands.html)  
[github.com/imneme/bounded-rands](https://github.com/imneme/bounded-rands)  
  

## Usage

Getting instance of the RandomNumbers<T> class:

```C#
using TRNGTool;

var randomDataPath = @"my_path";
var randomDataFile = @"my_path\my_file.bin";

// passing the path to binary files as a source of random data:
var rgInt8  = RandomNumbers<byte>.Create(randomDataPath, "*.bin", 10_000);  // 10_000 bytes
var rgInt16 = RandomNumbers<UInt16>.Create(randomDataPath, "*.bin");        // use all available data
var rgInt32 = RandomNumbers<UInt32>.Create(randomDataPath, "*.bin");

// passing path to a particular file:
var anotherRgInt8  = RandomNumbers<byte>.Create(randomDataFile);             // use all available data
var anotherRgInt16 = RandomNumbers<UInt16>.Create(randomDataFile);
var anotherRgInt32 = RandomNumbers<UInt32>.Create(randomDataFile, 10_000);   // also ten thousand bytes
```

Or the same:

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

Pass to Create() the path to the data files or a file as the first parameter.
If you are using a path, you must also provide the file mask as the second parameter.
With the last parameter you can specify the exact number of bytes to load (omitting it means all available data will be used).
The size specified must be a multiple to the sizeof() of the integer type used to assemble the random data.  

You can also use byte array as a source of data:

```C#
byte[] myByteArray = new byte[] { /* data from somewhere */ };

var rgInt8  = RandomNumbers<byte>.Create(myByteArray, 0, 10_000);  // 10_000 bytes from the beginning
var rgInt16 = RandomNumbers<UInt16>.Create(myByteArray);           // use all available data
var rgInt32 = RandomNumbers<UInt32>.Create(myByteArray);

var anotherRgInt8 = RandomNumbers<byte>.Create();
var anotherRgInt16 = RandomNumbers<UInt16>.Create();
var anotherRgInt32 = RandomNumbers<UInt32>.Create();

anotherRgInt8.AddFromBytes(myByteArray, 0, 10_000);
anotherRgInt16.AddFromBytes(myByteArray);
anotherRgInt32.AddFromBytes(myByteArray);
```

All data are loaded into the heap and stored internally as a LinkedList of arrays.
GetInt() methods allow you to sequentially read the stored data.
Arrays that had been completely read are removed from the list, becoming available for GC.

#### Obtaining random numbers within the entire range of the byte, UInt16 or UInt32 numeric types:

```C#

// [0 to 255]
uint r1 = rgInt8.GetInt();

// [0 to 65535]
uint r2 = rgInt16.GetInt();

// [0 to UInt32.MaxValue]
uint r3 = rgInt32.GetInt();
```

#### Getting numbers in a custom range:

```C#

// [0 to 254]
uint r1 = rgInt8.GetInt(0, 255);

// [300 to 554]
uint r2 = rgInt8.GetInt(300, 555);

// [10_000 to 10_254]
uint r3 = rgInt8.GetInt(10_000, 10_000 + byte.MaxValue);

// [0 to 65534]
uint r4 = rgInt16.GetInt(0, UInt16.MaxValue);

// [0 to UInt32.MaxValue - 1]
uint r5 = rgInt32.GetInt(0, UInt32.MaxValue);

```

## Error handling

Use the **OutOfData** event to detect the moment of exhaustion of the RandomNumbers object's buffers:

```C#
rg.OutOfData += RgOutOfData;

for (int i = 0; i < 100_000; i++)
{
    var r = rg.GetInt();
}   
    
void RgOutOfData(object sender, EventArgs e)
{
    // load more data
    rg.AddFromFile(myFile);
    rg.AddFromPath(myPath);
}

```

Event is raised immediately after the GetInt() method used the last available element in the last array.  
The next call to GetInt() will cause **TRNGToolOutOfDataException**.


#### Handling I/O and other errors:

```C#
try
{
    // use RandomNumbers
}
catch (TRNGToolOutOfDataException e)
{
    // out of data
    Console.WriteLine(e);
}
catch (TRNGToolIOException e)
{
    // IO error
    Console.WriteLine(e);
    if (e.InnerException != null)
        Console.WriteLine(e.InnerException);
}
catch (TRNGToolException e)
{
    // bad arguments or data of incorrect size
    Console.WriteLine(e);
}
```

If a SystemException exception occurs the RandomNumbers object envelopes it in the **InnerException** property.


## Notes

You can set the size of arrays that the RandomNumbers object uses by changing the **MaxArraySize** property before loading the data:

```C#
const int oneKb = 2 << (10 - 1);
rg.MaxArraySize = oneKb;
rg.Clear();
rg.AddFromPath(myPath);
```

The default value is set to 35 MB.

Here is the result of the perfomance test which was generating 50 million UInt32 random numbers in the range  
[0, 500_000] with different arrays sizes. The overall size of the preloaded random data was 250 MB, of which 190.8 MB was used in each iteration.
  
Without forced GC:
  
![alt text](https://i.ibb.co/vcrG84w/3.png)
![alt text](https://i.ibb.co/ygMBqk2/4.png)  
  
With forced GC before each iteration:
  
![alt text](https://i.ibb.co/Q6d0VHL/1.png)
![alt text](https://i.ibb.co/VL0qc5h/2.png)

The size of arrays does not have significant impact on perfomance, but large arrays suddenly emerging on the heap encourage GC to do extra work for arranging the data.
For this reason don't choose large array sizes.
