using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using PdbReadingBenchmarks.Contracts;
using PdbReadingBenchmarks.DbgHelpPdbReader;
using PdbReadingBenchmarks.DiaNativeSymReader;
using VerifyTests;
using VerifyXunit;

namespace PdbReadingBenchmarks
{
    [MemoryDiagnoser,NativeMemoryProfiler]
    [SimpleJob(RunStrategy.Monitoring, 10, 0, 100)]
    [UsesVerify]
    public class PdbReadBenchmarksBase
    {
        [GlobalSetup]
        public void Setup()
        {
            Assembly.LoadFrom(SampleDataFactory.GetSampleData(PdbType.WindowsPdb).AssemblyFullPath);
        }

        public static IEnumerable<object[]> AllScenarios =>
            new[]
            {
                new object[] { PdbType.WindowsPdb, PdbReaderLibrary.Dnlib_Managed },
                new object[] { PdbType.WindowsPdb, PdbReaderLibrary.Dnlib_DiaSymReader },
                new object[] { PdbType.WindowsPdb, PdbReaderLibrary.MonoCecil },
                new object[] { PdbType.WindowsPdb, PdbReaderLibrary.DiaNativeSymReader },
                
                new object[] { PdbType.PortablePdb, PdbReaderLibrary.Dnlib_Managed},
                new object[] { PdbType.PortablePdb, PdbReaderLibrary.DiaNativeSymReader },
            };

        protected static Task VerifyResults<T>(T results, PdbType pdbType)
        {
#if DEBUG // Do not verify results if we're running a benchmark
            var verifySettings = new VerifySettings();
            verifySettings.UseParameters(pdbType, "all");
            verifySettings.ScrubLinesContaining("Index:");
            return Verifier.Verify(results, verifySettings);
#endif
            return Task.CompletedTask;
        }

        protected IDebugInfoProvider CreateDebugInfoProvider(PdbReaderLibrary readerLibrary, PdbType pdbType)
        {
            var sample = SampleDataFactory.GetSampleData(pdbType);
            return readerLibrary switch
            {
                PdbReaderLibrary.DbgHelp => new DebugHelpPdbReader(sample.AssemblyFullPath),
                PdbReaderLibrary.DiaNativeSymReader => new DiaSymReaderPdbReader(sample.PdbFilePath, sample.AssemblyFullPath),
                PdbReaderLibrary.MonoCecil => new MonoCecilPdbReader(sample.AssemblyFullPath, sample.PdbFilePath),
                PdbReaderLibrary.Dnlib_Managed => new DnlibReader.DnlibPdbReader(sample.AssemblyFullPath, sample.PdbFilePath, useDiaSymReader: false),
                PdbReaderLibrary.Dnlib_DiaSymReader => new DnlibReader.DnlibPdbReader(sample.AssemblyFullPath, sample.PdbFilePath, useDiaSymReader: true),
                _ => throw new ArgumentOutOfRangeException(nameof(readerLibrary), readerLibrary, null)
            };
        }
    }
}