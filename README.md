This repo contains benchmarks for several popular managed PDB readers (for both Windows and Portable PDBs), with a specific interest in gauging which library is best for production diagnostic use cases, where the PDB reading will be performed in a production environment, and possibly from within the monitored application itself.

## Why even bother doing a benchmark on PDB readers?
In most use cases, production diagnostics tools (such as debugger and profilers) only need to make a few (dozens or hundreds) reads 
from PDB at any given time. Even code coverage tools, which need to read sequence points for each and every executed method,
the overall performance will likely won't be hugely effected by PDB reading performance.

The motivation for creating these benchmarks is more around measuring the memory impact - as different libraries have wildly different characteristics both in terms of native and managed allocations. This is very significant, especially in memory constrained production environments (e.g. linux containers running in k8).

## How do these benchmarks work?
This repo utilizes Benchmark.NET and xUnit side by side, so that you can easily benchmark or run/debug each variation. 

## What libraries are included?
See  [PDB Library enum](https://github.com/OmerRaviv/PdbBenchmarks/blob/main/PdbReadingBenchmarks/PdbReaderLibrary.cs#L3).

### Why isn't library XYZ included in this benchmark?

- [`PPDB`](https://github.com/AaronRobinsonMSFT/PPDB) is a very nice native implementation of a Portable PDB reader, that seems to be very much based on `System.Reflection.Metadata`'s design. This library was not included because it seems it has [not been thoroughly tested in production use-cases](https://github.com/AaronRobinsonMSFT/PPDB/issues/9).
- `dbghelp` is a Win32 API that can be easily used via PInvoke to read Windows PDBs with excellent performance. It was not included in this benchmark because, being a legacy API, it only has the notion of line numbers, and not column numbers, which makes it inadequate for handling modern C# code which may contain several lambda methods in the same line of code.


##Results
``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.22000
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4470.0), X64 RyuJIT
  Job-OYCNNG : .NET Framework 4.8 (4.8.4470.0), X64 RyuJIT

IterationCount=100  LaunchCount=10  RunStrategy=Monitoring  
WarmupCount=0  

```
|                                     Method |     pdbType |      readerLibrary |        Mean |    Error |    StdDev |      Median | Rank |     Gen 0 | Gen 1 | Gen 2 |  Allocated | Allocated native memory | Native memory leak |
|------------------------------------------- |------------ |------------------- |------------:|---------:|----------:|------------:|-----:|----------:|------:|------:|-----------:|------------------------:|-------------------:|
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** | **DiaNativeSymReader** |  **2,280.4 μs** | **18.51 μs** | **177.35 μs** |  **2,238.0 μs** |    **3** |         **-** |     **-** |     **-** |  **916.09 KB** |                 **1544 KB** |         **1238.25 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** |          **MonoCecil** |  **8,608.3 μs** | **45.34 μs** | **434.42 μs** |  **8,526.6 μs** |    **5** |         **-** |     **-** |     **-** | **5006.66 KB** |                 **2.05 KB** |            **0.16 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** |      **Dnlib_Managed** | **11,287.5 μs** | **72.66 μs** | **696.23 μs** | **11,172.1 μs** |    **6** | **1000.0000** |     **-** |     **-** | **9552.98 KB** |                 **2.59 KB** |            **0.31 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** | **Dnlib_DiaSymReader** |  **3,293.8 μs** | **34.65 μs** | **332.05 μs** |  **3,226.5 μs** |    **4** |         **-** |     **-** |     **-** | **3427.67 KB** |              **1576.72 KB** |         **1268.05 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** | **PortablePdb** | **DiaNativeSymReader** |    **752.0 μs** |  **9.53 μs** |  **91.35 μs** |    **739.9 μs** |    **1** |         **-** |     **-** |     **-** |  **812.44 KB** |               **243.68 KB** |             **242 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** | **PortablePdb** |      **Dnlib_Managed** |  **1,358.0 μs** | **17.22 μs** | **165.00 μs** |  **1,306.0 μs** |    **2** |         **-** |     **-** |     **-** | **1911.78 KB** |                 **2.88 KB** |            **0.59 KB** |




|                            Method |     pdbType |      readerLibrary |      Mean |     Error |    StdDev |     Median | Rank |     Gen 0 | Gen 1 | Gen 2 |  Allocated | Allocated native memory | Native memory leak |
|---------------------------------- |------------ |------------------- |----------:|----------:|----------:|-----------:|-----:|----------:|------:|------:|-----------:|------------------------:|-------------------:|
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** | **DiaNativeSymReader** |  **2.848 ms** | **0.0253 ms** | **0.2420 ms** |  **2.7898 ms** |    **3** |         **-** |     **-** |     **-** |  **251.66 KB** |              **1577.38 KB** |         **1158.32 KB** |
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** |      **Dnlib_Managed** | **11.393 ms** | **0.1080 ms** | **1.0352 ms** | **11.1354 ms** |    **5** | **1000.0000** |     **-** |     **-** | **9221.22 KB** |                 **2.31 KB** |            **0.25 KB** |
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** | **Dnlib_DiaSymReader** |  **3.616 ms** | **0.0258 ms** | **0.2472 ms** |  **3.5603 ms** |    **4** |         **-** |     **-** |     **-** |  **2748.8 KB** |              **1609.08 KB** |         **1188.13 KB** |
| **GetILOffsetAndLocals_FromFileLine** | **PortablePdb** | **DiaNativeSymReader** |  **2.120 ms** | **0.0144 ms** | **0.1376 ms** |  **2.0872 ms** |    **2** |         **-** |     **-** |     **-** | **1264.05 KB** |               **243.68 KB** |          **242.21 KB** |
| **GetILOffsetAndLocals_FromFileLine** | **PortablePdb** |      **Dnlib_Managed** |  **1.021 ms** | **0.0121 ms** | **0.1160 ms** |  **0.9870 ms** |    **1** |         **-** |     **-** |     **-** | **1331.39 KB** |                  **2.5 KB** |            **0.44 KB** |
