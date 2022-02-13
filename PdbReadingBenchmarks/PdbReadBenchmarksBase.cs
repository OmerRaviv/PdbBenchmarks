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
    [RankColumn,MemoryDiagnoser,NativeMemoryProfiler]
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
                new object[] { PdbType.WindowsPdb, PdbReaderLibrary.Dnlib },
                new object[] { PdbType.WindowsPdb, PdbReaderLibrary.DbgHelp },
                new object[] { PdbType.WindowsPdb, PdbReaderLibrary.MonoCecil },
                new object[] { PdbType.WindowsPdb, PdbReaderLibrary.DiaNativeSymReader },
                new object[] { PdbType.PortablePdb, PdbReaderLibrary.Dnlib},
                //new object[] { PdbType.PortablePdb, PdbReaderLibrary.DiaNativeSymReader },
            };

        protected static Task VerifyResults<T>(T results, PdbType pdbType)
        {
#if DEBUG // Do not verify results if we're running a benchmark
            var verifySettings = new VerifySettings();
            verifySettings.UseParameters(pdbType, "all");
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
                PdbReaderLibrary.Dnlib => new DnlibReader.DnlibPdbReader(sample.AssemblyFullPath, sample.PdbFilePath),
                _ => throw new ArgumentOutOfRangeException(nameof(readerLibrary), readerLibrary, null)
            };
        }
    }
}