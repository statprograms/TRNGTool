# True random numbers generator tool

Получение случайных чисел с хорошим статистическим качеством и скоростью.    

Код предназначен для работы с готовыми последовательностями истинно случайных чисел, полученных, например,
с помощью таких сервисов как [random.org](https://www.random.org) или [quantumnumbers.anu.edu.au](https://quantumnumbers.anu.edu.au).	 

Для генерации случайных чисел в нужном диапазоне используется алгоритм Мелиссы О`Нил:  
[pcg-random.org/posts/bounded-rands.html](https://www.pcg-random.org/posts/bounded-rands.html)  
[github.com/imneme/bounded-rands](https://github.com/imneme/bounded-rands)  
  
## Использование

Получение экземпляра класса RandomNumbers<T>:

```C#
using TRNGTool;

var randomDataPath = @"my_path";
var randomDataFile = @"my_path\my_file.bin";

// указание пути к бинарным файлам в качестве источника данных:
var rgInt8  = RandomNumbers<byte>.Create(randomDataPath, "*.bin", 10_000);  // 10_000 байт
var rgInt16 = RandomNumbers<UInt16>.Create(randomDataPath, "*.bin");        // использовать все имеющиеся данные
var rgInt32 = RandomNumbers<UInt32>.Create(randomDataPath, "*.bin");

// указание пути к конкретному файлу:
var anotherRgInt8  = RandomNumbers<byte>.Create(randomDataFile);             // использовать все имеющиеся данные
var anotherRgInt16 = RandomNumbers<UInt16>.Create(randomDataFile);
var anotherRgInt32 = RandomNumbers<UInt32>.Create(randomDataFile, 10_000);   // также 10_000 байт
```

По-другому:

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
В первом параметре Create() принимает путь к файлам (или файлу) со случайными данными.
Если используется путь к директории, обязательно указание файловой маски в качестве второго параметра.
В последнем параметре указывается точное количество байт для загрузки (если его опустить, загружаются все имеющиеся данные).
Указанный размер должен быть кратен количеству байт используемого для компоновки случайных данных целочисленного типа.  

В качестве источника данных можно использовать байтовый массив:

```C#
byte[] myByteArray = new byte[] { /* данные */ };

var rgInt8  = RandomNumbers<byte>.Create(myByteArray, 0, 10_000);  // 10_000 байт от начала
var rgInt16 = RandomNumbers<UInt16>.Create(myByteArray);           // использовать все имеющиеся данные
var rgInt32 = RandomNumbers<UInt32>.Create(myByteArray);

var anotherRgInt8 = RandomNumbers<byte>.Create();
var anotherRgInt16 = RandomNumbers<UInt16>.Create();
var anotherRgInt32 = RandomNumbers<UInt32>.Create();

anotherRgInt8.AddFromBytes(myByteArray, 0, 10_000);
anotherRgInt16.AddFromBytes(myByteArray);
anotherRgInt32.AddFromBytes(myByteArray);
```

Все данные загружаются в кучу в двусвязный список массивов (LinkedList<T[]>).
Методы GetInt() позволяют последовательно читать сохранённые данные.
Массивы, которые были полностью прочитаны, удаляются из списка, становясь доступными для сборки мусора.

#### Получение случайных чисел во всём диапазоне целочисленных типов byte, UInt16, или UInt32:

```C#

// [0, 255]
uint r1 = rgInt8.GetInt();

// [0, 65535]
uint r2 = rgInt16.GetInt();

// [0, UInt32.MaxValue]
uint r3 = rgInt32.GetInt();
```

#### Получение случайных чисел в заданном диапазоне:

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

## Обработка ошибок

Для того, чтобы отследить момент исчерпания данных, загруженных в объект класса RandomNumbers, используйте событие **OutOfData**:

```C#
rg.OutOfData += RgOutOfData;

for (int i = 0; i < 100_000; i++)
{
    var r = rg.GetInt();
}   
    
void RgOutOfData(object sender, EventArgs e)
{
    // загрузить больше данных
    rg.AddFromFile(myFile);
    rg.AddFromPath(myPath);
}

```

Событие срабатывает непосредственно после извлечения методом GetInt() последнего элемента последнего массива.
Последующие обращения к GetInt() приведут к генерации исключения **TRNGToolOutOfDataException**.


#### Обработка ошибок ввода-вывода и других ошибок:

```C#
try
{
    // использование RandomNumbers
}
catch (TRNGToolOutOfDataException e)
{
    // закончились данные
    Console.WriteLine(e);
}
catch (TRNGToolIOException e)
{
    // ошибка ввода-вывода
    Console.WriteLine(e);
    if (e.InnerException != null)
        Console.WriteLine(e.InnerException);
}
catch (TRNGToolException e)
{
    // неверные аргументы или данные неподходящего размера
    Console.WriteLine(e);
}
```

Все исключения SystemException, возникающие во время работы с классом RandomNumbers, помещаются в свойство **InnerException**.


## Примечания

Перед загрузкой данных, с помощью свойства **MaxArraySize** можно задать предельный размер используемых
объектом класса RandomNumbers массивов:

```C#
const int oneKb = 2 << (10 - 1);
rg.MaxArraySize = oneKb;
rg.Clear();
rg.AddFromPath(myPath);
```

Предельный размер массивов по умолчанию — 35 Мб.

На графиках приведены результаты работы теста производительности, генерировавшего 50 миллионов случайных чисел в диапазоне [0, 500_000] с различными размерами массивов
(общий объем предварительно загруженных случайных данных составлял 250 Мб, из которых в каждой итерации использовалось 190,8 Мб):
  
Без принудительной сборки мусора:
  
![alt text](https://i.ibb.co/vcrG84w/3.png)
![alt text](https://i.ibb.co/ygMBqk2/4.png)  
  
С принудительной сборкой мусора перед каждой итерацией:
  
![alt text](https://i.ibb.co/Q6d0VHL/1.png)
![alt text](https://i.ibb.co/VL0qc5h/2.png)

Размер массивов не оказывает существенного влияния на производительность, но внезапно возникающие в куче большие массивы побуждают
сборщик мусора выполнять дополнительную работу по организации данных. По этой причине лучше использовать небольшие массивы.
