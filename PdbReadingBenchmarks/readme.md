This repo contains benchmarks for several popular managed PDB readers.

Why bother benchmarking PDB reading in the first place?
Most tools (such as debugger and profilers) only need to make a few (dozens, hundreds) of calls 

```

|                                     Method |     pdbType |      readerLibrary |        Mean |     Error |     StdDev |      Median | Rank |     Gen 0 | Gen 1 | Gen 2 |  Allocated | Allocated native memory | Native memory leak |
|------------------------------------------- |------------ |------------------- |------------:|----------:|-----------:|------------:|-----:|----------:|------:|------:|-----------:|------------------------:|-------------------:|
| GetLocalsAndSequencePoints_FromMethodToken |  WindowsPdb |            DbgHelp |    592.2 us |  11.42 us |   109.4 us |    552.8 us |    1 |         - |     - |     - |  587.41KB |                 9.95 KB |            0.82 KB |
| GetLocalsAndSequencePoints_FromMethodToken |  WindowsPdb | DiaNativeSymReader |  2,368.8 us |  29.70 us |   284.5 us |  2,280.9 us |    4 |         - |     - |     - |  916.09KB |              1543.91 KB |         1238.15 KB |
| GetLocalsAndSequencePoints_FromMethodToken |  WindowsPdb |          MonoCecil |  9,055.1 us | 124.53 us | 1,193.2 us |  8,722.1 us |    5 |         - |     - |     - | 5006.66KB |                 2.11 KB |            0.22 KB |
| GetLocalsAndSequencePoints_FromMethodToken |  WindowsPdb |              Dnlib | 11,456.1 us |  94.68 us |   907.2 us | 11,282.2 us |    6 | 1000.0000 |     - |     - | 9552.99KB |                 2.37 KB |            0.09 KB |
| GetLocalsAndSequencePoints_FromMethodToken | PortablePdb | DiaNativeSymReader |    819.6 us |  15.34 us |   147.0 us |    763.2 us |    2 |         - |     - |     - |  812.44KB |               243.68 KB |             242 KB |
| GetLocalsAndSequencePoints_FromMethodToken | PortablePdb |              Dnlib |  1,340.1 us |  16.77 us |   160.7 us |  1,280.2 us |    3 |         - |     - |     - | 1911.78KB |                 2.69 KB |            0.41 KB |

```


```

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.22000
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4470.0), X64 RyuJIT
  Job-OYCNNG : .NET Framework 4.8 (4.8.4470.0), X64 RyuJIT

IterationCount=100  LaunchCount=10  RunStrategy=Monitoring  
WarmupCount=0  
```

````
|                            Method |     pdbType |      readerLibrary |      Mean |     Error |    StdDev |    Median | Rank |     Gen 0 | Gen 1 | Gen 2 |  Allocated | Allocated native memory | Native memory leak |
|---------------------------------- |------------ |------------------- |----------:|----------:|----------:|----------:|-----:|----------:|------:|------:|-----------:|------------------------:|-------------------:|
| GetILOffsetAndLocals_FromFileLine |  WindowsPdb | DiaNativeSymReader |  2.997 ms | 0.0347 ms | 0.3326 ms |  2.888 ms |    3 |         - |     - |     - |  251.66 KB |    1577.45 KB |         1158.39 KB |
| GetILOffsetAndLocals_FromFileLine |  WindowsPdb |              Dnlib | 11.586 ms | 0.1017 ms | 0.9747 ms | 11.399 ms |    4 | 1000.0000 |     - |     - |  9163.7 KB |       2.15 KB |            0.09 KB |
| GetILOffsetAndLocals_FromFileLine | PortablePdb | DiaNativeSymReader |  2.353 ms | 0.0391 ms | 0.3746 ms |  2.216 ms |    2 |         - |     - |     - | 1264.05 KB |     243.99 KB |          242.53 KB |
| GetILOffsetAndLocals_FromFileLine | PortablePdb |              Dnlib |  1.050 ms | 0.0134 ms | 0.1283 ms |  1.011 ms |    1 |         - |     - |     - | 1331.39 KB |       2.69 KB |            0.63 KB |
```