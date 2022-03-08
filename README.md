## What is this?
This repo contains benchmarks for several popular managed PDB readers (for both Windows and Portable PDBs), with a specific interest in gauging which library is best for production diagnostic use cases, where the PDB reading will be performed in a production environment, and possibly from within the monitored application itself.

## Why even bother doing a benchmark on PDB readers?
In most use cases, production diagnostics tools (such as debugger and profilers) only need to make a few (dozens or hundreds) reads 
from PDB at any given time. Even in the case of code coverage tools, which need to read sequence points for each and every executed method,
the overall performance likely won't be hugely effected by PDB reading performance.

The motivation for creating these benchmarks is more around measuring the memory impact - as different libraries have wildly different characteristics both in terms of native and managed allocations. This is very significant, especially in memory constrained production environments (e.g. linux containers running in k8).

## How do these benchmarks work?
This repo utilizes Benchmark.NET and xUnit side by side, so that you can easily benchmark or run/debug each variation. 

## What libraries are included?
See  [PDB Library enum](https://github.com/OmerRaviv/PdbBenchmarks/blob/main/PdbReadingBenchmarks/PdbReaderLibrary.cs#L3).

### Why isn't library XYZ included in this benchmark?

- [`PPDB`](https://github.com/AaronRobinsonMSFT/PPDB) is a very nice native implementation of a Portable PDB reader, that seems to be very much based on `System.Reflection.Metadata`'s design. This library was not included because it seems it has [not been thoroughly tested in production use-cases](https://github.com/AaronRobinsonMSFT/PPDB/issues/9).
- `dbghelp` is a Win32 API that can be easily used via PInvoke to read Windows PDBs with excellent performance. It was not included in this benchmark because, being a legacy API, it only has the notion of line numbers, and not column numbers, which makes it inadequate for handling modern C# code which may contain several lambda methods in the same line of code.

If you feel there is another library that is worth including in these benchmarks, please don't hesitate and create an issue/PR for it.

## Results
``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.22000
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4470.0), X64 RyuJIT
  Job-OYCNNG : .NET Framework 4.8 (4.8.4470.0), X64 RyuJIT

IterationCount=100  LaunchCount=10  RunStrategy=Monitoring  
WarmupCount=0  

```
|                            Method |     pdbType |      readerLibrary |      Mean |     Error |    StdDev |     Median |     Gen 0 | Gen 1 | Gen 2 |  Allocated | Allocated native memory | Native memory leak |
|---------------------------------- |------------ |------------------- |----------:|----------:|----------:|-----------:|----------:|------:|------:|-----------:|------------------------:|-------------------:|
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** | **DiaNativeSymReader** |  **3.140 ms** | **0.0340 ms** | **0.3259 ms** |  **3.0492 ms** |         **-** |     **-** |     **-** |  **251.66 KB** |              **1575.83 KB** |            **1.87 KB** |
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** |      **Dnlib_Managed** | **11.741 ms** | **0.1079 ms** | **1.0338 ms** | **11.5099 ms** | **1000.0000** |     **-** |     **-** | **9221.22 KB** |                 **2.21 KB** |            **0.16 KB** |
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** | **Dnlib_DiaSymReader** |  **3.775 ms** | **0.0297 ms** | **0.2843 ms** |  **3.7096 ms** |         **-** |     **-** |     **-** | **2223.19 KB** |              **1607.46 KB** |            **2.09 KB** |
| **GetILOffsetAndLocals_FromFileLine** | **PortablePdb** | **DiaNativeSymReader** |  **2.309 ms** | **0.0331 ms** | **0.3170 ms** |  **2.2220 ms** |         **-** |     **-** |     **-** | **1264.05 KB** |               **243.84 KB** |            **0.66 KB** |
| **GetILOffsetAndLocals_FromFileLine** | **PortablePdb** |      **Dnlib_Managed** |  **1.024 ms** | **0.0091 ms** | **0.0870 ms** |  **0.9959 ms** |         **-** |     **-** |     **-** | **1331.39 KB** |                 **2.35 KB** |            **0.28 KB** |


|                                     Method |     pdbType |      readerLibrary |        Mean |    Error |    StdDev |      Median |     Gen 0 | Gen 1 | Gen 2 |  Allocated | Allocated native memory | Native memory leak |
|------------------------------------------- |------------ |------------------- |------------:|---------:|----------:|------------:|----------:|------:|------:|-----------:|------------------------:|-------------------:|
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** | **DiaNativeSymReader** |  **2,099.5 μs** | **19.23 μs** | **184.27 μs** |  **2,051.3 μs** |         **-** |     **-** |     **-** |  **336.69 KB** |              **1541.93 KB** |            **2.24 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** |          **MonoCecil** |  **8,181.4 μs** | **66.88 μs** | **640.79 μs** |  **8,008.8 μs** |         **-** |     **-** |     **-** | **4427.26 KB** |                 **2.52 KB** |            **0.84 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** |      **Dnlib_Managed** | **10,348.7 μs** | **49.84 μs** | **477.58 μs** | **10,277.1 μs** | **1000.0000** |     **-** |     **-** | **8963.57 KB** |                 **2.31 KB** |            **0.25 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** | **Dnlib_DiaSymReader** |  **2,849.1 μs** | **29.10 μs** | **278.81 μs** |  **2,777.6 μs** |         **-** |     **-** |     **-** | **2316.21 KB** |              **1574.86 KB** |            **2.74 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** | **PortablePdb** | **DiaNativeSymReader** |    **294.5 μs** |  **9.71 μs** |  **93.03 μs** |    **287.9 μs** |         **-** |     **-** |     **-** |  **136.05 KB** |               **243.59 KB** |            **0.41 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** | **PortablePdb** |      **Dnlib_Managed** |    **790.0 μs** | **12.13 μs** | **116.27 μs** |    **753.1 μs** |         **-** |     **-** |     **-** | **1211.18 KB** |                 **2.16 KB** |            **0.09 KB** |
