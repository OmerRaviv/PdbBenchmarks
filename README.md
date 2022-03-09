## What is this?
This repo contains benchmarks for several popular managed PDB readers (for both Windows and Portable PDBs), with a specific interest in gauging which library is best for production diagnostic use cases, where the PDB reading will be performed in a production environment, and possibly from within the monitored application itself.

## Why even bother doing a benchmark on PDB readers?
In most use cases, production diagnostics tools (such as debugger and profilers) only need to make a few (dozens or hundreds) reads 
from PDB at any given time. Even in the case of code coverage tools, which need to read sequence points for each and every executed method,
the overall performance likely won't be hugely effected by PDB reading performance.

The motivation for creating these benchmarks is more around measuring the memory impact - as different libraries have wildly different characteristics both in terms of native and managed allocations. This is very significant, especially in memory constrained production environments (e.g. linux containers running in k8).

## How do these benchmarks work?
This repo utilizes Benchmark.NET and xUnit side by side, so that you can easily benchmark or run/debug each variation. 
In the unit tests, we employ snapshot testing with [Verify](https://github.com/VerifyTests/Verify) in order to ensure the code fragments being benchmarked for the different libraries all return the exact same result. 

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
|                                     Method |     pdbType |      readerLibrary |        Mean |     Error |      StdDev |      Median |     Gen 0 | Gen 1 | Gen 2 |  Allocated | Allocated native memory | Native memory leak |
|------------------------------------------- |------------ |------------------- |------------:|----------:|------------:|------------:|----------:|------:|------:|-----------:|------------------------:|-------------------:|
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** | **DiaNativeSymReader** |  **2,155.9 μs** |  **26.01 μs** |   **249.22 μs** |  **2,088.7 μs** |         **-** |     **-** |     **-** |  **336.69 KB** |              **1542.02 KB** |            **2.33 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** |          **MonoCecil** |  **8,258.0 μs** |  **52.65 μs** |   **504.48 μs** |  **8,119.9 μs** |         **-** |     **-** |     **-** | **4427.26 KB** |                 **2.52 KB** |            **0.84 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** |      **Dnlib_Managed** | **10,971.8 μs** | **113.83 μs** | **1,090.69 μs** | **10,713.0 μs** | **1000.0000** |     **-** |     **-** | **7000.02 KB** |                 **1.94 KB** |            **0.09 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** |  **WindowsPdb** | **Dnlib_DiaSymReader** |  **2,439.1 μs** |  **40.23 μs** |   **385.48 μs** |  **2,324.0 μs** |         **-** |     **-** |     **-** |  **352.69 KB** |              **1574.61 KB** |            **2.71 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** | **PortablePdb** | **DiaNativeSymReader** |    **275.0 μs** |   **6.68 μs** |    **64.00 μs** |    **282.2 μs** |         **-** |     **-** |     **-** |  **136.05 KB** |                **243.3 KB** |            **0.13 KB** |
| **GetLocalsAndSequencePoints_FromMethodToken** | **PortablePdb** |      **Dnlib_Managed** |    **846.0 μs** |  **16.94 μs** |   **162.31 μs** |    **801.9 μs** |         **-** |     **-** |     **-** |  **969.44 KB** |                 **2.79 KB** |            **0.94 KB** |

|                            Method |     pdbType |      readerLibrary |      Mean |     Error |    StdDev |    Median |     Gen 0 | Gen 1 | Gen 2 |  Allocated | Allocated native memory | Native memory leak |
|---------------------------------- |------------ |------------------- |----------:|----------:|----------:|----------:|----------:|------:|------:|-----------:|------------------------:|-------------------:|
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** | **DiaNativeSymReader** |  **3.974 ms** | **0.0805 ms** | **0.7716 ms** |  **3.688 ms** |         **-** |     **-** |     **-** |  **251.66 KB** |              **1575.74 KB** |            **1.79 KB** |
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** |      **Dnlib_Managed** | **12.050 ms** | **0.1385 ms** | **1.3274 ms** | **11.683 ms** | **1000.0000** |     **-** |     **-** | **7257.67 KB** |                 **2.09 KB** |            **0.25 KB** |
| **GetILOffsetAndLocals_FromFileLine** |  **WindowsPdb** | **Dnlib_DiaSymReader** |  **3.307 ms** | **0.0364 ms** | **0.3486 ms** |  **3.212 ms** |         **-** |     **-** |     **-** |  **259.66 KB** |              **1607.28 KB** |            **2.12 KB** |
| **GetILOffsetAndLocals_FromFileLine** | **PortablePdb** | **DiaNativeSymReader** |  **2.327 ms** | **0.0329 ms** | **0.3151 ms** |  **2.233 ms** |         **-** |     **-** |     **-** | **1264.05 KB** |               **243.62 KB** |            **0.44 KB** |
| **GetILOffsetAndLocals_FromFileLine** | **PortablePdb** |      **Dnlib_Managed** |  **1.076 ms** | **0.0206 ms** | **0.1970 ms** |  **1.002 ms** |         **-** |     **-** |     **-** | **1089.65 KB** |                 **2.13 KB** |            **0.28 KB** |
